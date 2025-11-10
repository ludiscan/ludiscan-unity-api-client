using System;
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
        /// 既存の呼び出し口。v2 でストリームを作る。
        /// status は getStatus から受け取る（null なら付与なし）
        /// </summary>
        private Stream CreatePositionStream(
            PositionEntry[] buffer
        )
        {
            var data = ConstructPositionStreamV2(buffer);
            return new MemoryStream(data);
        }

        /// <summary>
        /// v2 パケット（"LSLP", ver=2, record_count, records...）
        /// </summary>
        private byte[] ConstructPositionStreamV2(
            PositionEntry[] buffer
        )
        {
            // 先に status のJSONを作って合計サイズを見積もると無駄な再割当を減らせる
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

            // ざっくり容量計算：ヘッダ9B + レコード固定部28B * N + status総和
            //  レコード固定部: 4+4+4+4+8+4 = 28
            int recordFixed = 28;
            int capacity = 9 + buffer.Length * recordFixed + totalStatusLen;

            using var ms = new MemoryStream(capacity);
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // header
            bw.Write(Encoding.ASCII.GetBytes("LSLP"));
            bw.Write((byte)2);                  // version
            bw.Write((uint)buffer.Length);      // record_count

            for (int i = 0; i < buffer.Length; i++)
            {
                var pos = buffer[i];

                // 既存の座標変換ロジックを踏襲（Unity X,Y,Z → 送信 X=Z, Y=X, Z=Y, 1cmスケール）
                bw.Write(pos.PlayerId);                   // i32
                bw.Write(pos.Z * 100f);                   // f32 x
                bw.Write(pos.X * 100f);                   // f32 y
                bw.Write(pos.Y * 100f);                   // f32 z
                bw.Write((ulong)pos.OffsetTimeStamp);     // u64

                // status
                var sBytes = statusBytes[i] ?? Array.Empty<byte>();

                // （任意）安全のため上限チェック：例 5MB
                const int MaxStatusBytes = 5 * 1024 * 1024;
                if (sBytes.Length > MaxStatusBytes)
                {
                    Debug.LogWarning($"Status too large({sBytes.Length}), trimming to {MaxStatusBytes}.");
                    Array.Resize(ref sBytes, MaxStatusBytes);
                }

                bw.Write((uint)sBytes.Length);            // u32 len
                if (sBytes.Length > 0) bw.Write(sBytes);  // payload
            }

            bw.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// v1 パケット
        /// </summary>
        private byte[] ConstructPositionStreamV1(PositionEntry[] buffer)
        {
            int stampCount = buffer.Length;
            int entrySize = sizeof(int) + (sizeof(float) * 3) + sizeof(ulong); // 4 + 12 + 8 = 24
            int capacity = 4 + stampCount * entrySize; // 先頭に count のみ

            using var ms = new MemoryStream(capacity);
            using var bw = new BinaryWriter(ms);

            bw.Write(stampCount);

            for (int i = 0; i < stampCount; i++)
            {
                var pos = buffer[i];
                bw.Write(pos.PlayerId);
                bw.Write(pos.Z * 100f);
                bw.Write(pos.X * 100f);
                bw.Write(pos.Y * 100f);
                bw.Write((ulong)pos.OffsetTimeStamp);
            }

            return ms.ToArray();
        }
    }
}
