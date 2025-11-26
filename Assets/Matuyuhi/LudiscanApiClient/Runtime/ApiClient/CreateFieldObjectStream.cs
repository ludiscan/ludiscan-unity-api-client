using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LudiscanApiClient.Runtime.ApiClient.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    public partial class LudiscanClient
    {
        /// <summary>
        /// フィールドオブジェクトログのバイナリストリームを作成
        /// LSFO V1フォーマット
        /// </summary>
        private Stream CreateFieldObjectStream(FieldObjectEntity[] buffer)
        {
            var data = ConstructFieldObjectStreamV1(buffer);
            return new MemoryStream(data);
        }

        /// <summary>
        /// LSFO V1 パケット構築
        /// フォーマット:
        ///   Header: Magic("LSFO") + Version(1) + RecordCount + StringTableCount
        ///   StringTable: 文字列の重複排除テーブル
        ///   Records: オブジェクトログレコード
        /// </summary>
        private byte[] ConstructFieldObjectStreamV1(FieldObjectEntity[] buffer)
        {
            // 文字列テーブルを構築
            var stringTable = new List<string>();
            var stringToIndex = new Dictionary<string, uint>();

            foreach (var entry in buffer)
            {
                if (!stringToIndex.ContainsKey(entry.ObjectId))
                {
                    stringToIndex[entry.ObjectId] = (uint)stringTable.Count;
                    stringTable.Add(entry.ObjectId);
                }

                if (!stringToIndex.ContainsKey(entry.ObjectType))
                {
                    stringToIndex[entry.ObjectType] = (uint)stringTable.Count;
                    stringTable.Add(entry.ObjectType);
                }
            }

            // status のJSONを事前に生成
            var statusBytes = new byte[buffer.Length][];
            int totalStatusLen = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                var sObj = buffer[i].Status;
                if (sObj == null)
                {
                    statusBytes[i] = Array.Empty<byte>();
                    continue;
                }

                var json = JsonConvert.SerializeObject(sObj);
                var bytes = Encoding.UTF8.GetBytes(json);
                statusBytes[i] = bytes;
                totalStatusLen += bytes.Length;
            }

            // 文字列テーブルのバイト数を計算
            int stringTableSize = 0;
            foreach (var str in stringTable)
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                stringTableSize += 4 + bytes.Length; // length(4) + data
            }

            // 容量見積もり:
            //   Header: 13 bytes
            //   StringTable: 計算済み
            //   Records: (4+4+1+4+4+4+8+4) * N + status総和 = 33 * N + totalStatusLen
            int capacity = 13 + stringTableSize + (33 * buffer.Length) + totalStatusLen;

            using var ms = new MemoryStream(capacity);
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // ========== Header ==========
            bw.Write(Encoding.ASCII.GetBytes("LSFO"));  // Magic: 4 bytes
            bw.Write((byte)1);                           // Version: 1 byte
            bw.Write((uint)buffer.Length);               // RecordCount: 4 bytes
            bw.Write((uint)stringTable.Count);           // StringTableCount: 4 bytes

            // ========== String Table ==========
            foreach (var str in stringTable)
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                bw.Write((uint)bytes.Length);            // string_len: 4 bytes
                bw.Write(bytes);                         // string_data: variable
            }

            // ========== Records ==========
            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];

                // インデックス取得
                uint objectIdIndex = stringToIndex[entry.ObjectId];
                uint objectTypeIndex = stringToIndex[entry.ObjectType];

                bw.Write(objectIdIndex);                 // object_id_index: 4 bytes
                bw.Write(objectTypeIndex);               // object_type_index: 4 bytes

                // EventType の値を バイナリフォーマット（0x01-0x04）に変換
                byte eventTypeByte = entry.EventType switch
                {
                    FieldObjectEventType.Spawn => 0x01,
                    FieldObjectEventType.Move => 0x02,
                    FieldObjectEventType.Despawn => 0x03,
                    FieldObjectEventType.Update => 0x04,
                    _ => 0x01  // Default to Spawn
                };
                bw.Write(eventTypeByte);                 // event_type: 1 byte

                // 既存の座標変換ロジックを踏襲（Unity X,Y,Z → 送信 X=Z, Y=X, Z=Y, 1cmスケール）
                bw.Write(entry.Z * 100f);               // x: 4 bytes (float)
                bw.Write(entry.X * 100f);               // y: 4 bytes (float)
                bw.Write(entry.Y * 100f);               // z: 4 bytes (float)
                bw.Write(entry.OffsetTimeStamp);        // offset_timestamp: 8 bytes

                // status
                var sBytes = statusBytes[i] ?? Array.Empty<byte>();

                // 安全のため上限チェック (5MB)
                const int MaxStatusBytes = 5 * 1024 * 1024;
                if (sBytes.Length > MaxStatusBytes)
                {
                    Debug.LogWarning($"FieldObject status too large ({sBytes.Length}), trimming to {MaxStatusBytes}.");
                    Array.Resize(ref sBytes, MaxStatusBytes);
                }

                bw.Write((uint)sBytes.Length);           // status_len: 4 bytes
                if (sBytes.Length > 0)
                {
                    bw.Write(sBytes);                    // status: variable
                }
            }

            bw.Flush();
            return ms.ToArray();
        }
    }
}
