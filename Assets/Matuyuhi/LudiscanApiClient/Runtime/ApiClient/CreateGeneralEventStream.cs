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
        /// 一般イベントログのバイナリストリームを作成
        /// スクリーンショットが含まれる場合はLSEV V2、なければLSEV V1フォーマット
        /// </summary>
        private Stream CreateGeneralEventStream(GeneralEventEntity[] buffer)
        {
            // スクリーンショットを含むイベントがあるかチェック
            bool hasScreenshots = false;
            foreach (var entry in buffer)
            {
                if (entry.Screenshots != null && entry.Screenshots.Length > 0)
                {
                    hasScreenshots = true;
                    break;
                }
            }

            byte[] data;
            if (hasScreenshots)
            {
                data = ConstructGeneralEventStreamV2(buffer);
            }
            else
            {
                data = ConstructGeneralEventStreamV1(buffer);
            }
            return new MemoryStream(data);
        }

        /// <summary>
        /// LSEV V1 パケット構築
        /// フォーマット:
        ///   Header: Magic("LSEV") + Version(1) + RecordCount
        ///   Records: イベントレコード（各レコードにメタデータを含む）
        /// </summary>
        private byte[] ConstructGeneralEventStreamV1(GeneralEventEntity[] buffer)
        {
            // メタデータのJSONを事前に生成
            // metadataとpositionを結合して1つのJSONにする
            var metadataBytes = new byte[buffer.Length][];
            int totalMetadataLen = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                var metadata = entry.Metadata;
                var position = entry.Position;

                // 座標は GeneralEventLogger.AddLog で既に変換済み（API座標系・cm単位）
                // そのまま使用する
                Dictionary<string, object> positionDict = null;
                if (position != null && position is Dictionary<string, object> posDict)
                {
                    positionDict = posDict;
                }
                else if (position != null && position is Dictionary<string, float> posFloatDict)
                {
                    positionDict = new Dictionary<string, object>();
                    foreach (var kvp in posFloatDict)
                    {
                        positionDict[kvp.Key] = kvp.Value;
                    }
                }
                else if (position != null)
                {
                    // positionがオブジェクトの場合、JSONシリアライズして取得
                    positionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(position)) ?? new Dictionary<string, object>();
                }

                // metadataとpositionを結合
                object combinedData = null;
                if (metadata != null && positionDict != null)
                {
                    // Both metadata and position exist - merge them
                    var metaDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(metadata)) ?? new Dictionary<string, object>();

                    foreach (var kvp in positionDict)
                    {
                        metaDict[kvp.Key] = kvp.Value;
                    }
                    combinedData = metaDict;
                }
                else if (positionDict != null)
                {
                    combinedData = positionDict;
                }
                else if (metadata != null)
                {
                    combinedData = metadata;
                }

                if (combinedData == null)
                {
                    metadataBytes[i] = Array.Empty<byte>();
                    continue;
                }

                var json = JsonConvert.SerializeObject(combinedData);
                var bytes = Encoding.UTF8.GetBytes(json);
                metadataBytes[i] = bytes;
                totalMetadataLen += bytes.Length;
            }

            // 容量見積もり:
            //   Header: 9 bytes (Magic 4 + Version 1 + RecordCount 4)
            //   Records: (4+4+8+4+4) * N + metadata総和 = 24 * N + totalMetadataLen
            int capacity = 9 + (24 * buffer.Length) + totalMetadataLen;

            using var ms = new MemoryStream(capacity);
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // ========== Header ==========
            bw.Write(Encoding.ASCII.GetBytes("LSEV"));  // Magic: 4 bytes
            bw.Write((byte)1);                           // Version: 1 byte
            bw.Write((uint)buffer.Length);               // RecordCount: 4 bytes

            // ========== Records ==========
            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];

                // イベントタイプを文字列として記録
                var eventTypeBytes = Encoding.UTF8.GetBytes(entry.EventType);
                bw.Write((uint)eventTypeBytes.Length);   // event_type_len: 4 bytes
                bw.Write(eventTypeBytes);                 // event_type: variable

                bw.Write(entry.OffsetTimeStamp);         // offset_timestamp: 8 bytes
                bw.Write((uint)entry.Player);            // player: 4 bytes

                // メタデータ
                var mBytes = metadataBytes[i] ?? Array.Empty<byte>();

                // 安全のため上限チェック (1MB)
                const int MaxMetadataBytes = 1 * 1024 * 1024;
                if (mBytes.Length > MaxMetadataBytes)
                {
                    Debug.LogWarning($"GeneralEvent metadata too large ({mBytes.Length}), trimming to {MaxMetadataBytes}.");
                    Array.Resize(ref mBytes, MaxMetadataBytes);
                }

                bw.Write((uint)mBytes.Length);           // metadata_len: 4 bytes
                if (mBytes.Length > 0)
                {
                    bw.Write(mBytes);                    // metadata: variable
                }
            }

            bw.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// LSEV V2 パケット構築（スクリーンショット対応）
        /// フォーマット:
        ///   Header: Magic("LSEV") + Version(2) + RecordCount
        ///   Records: イベントレコード（各レコードにメタデータとスクリーンショットを含む）
        /// </summary>
        private byte[] ConstructGeneralEventStreamV2(GeneralEventEntity[] buffer)
        {
            // メタデータのJSONを事前に生成
            var metadataBytes = new byte[buffer.Length][];
            int totalMetadataLen = 0;
            int totalScreenshotLen = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                var metadata = entry.Metadata;
                var position = entry.Position;

                // 座標は GeneralEventLogger.AddLog で既に変換済み（API座標系・cm単位）
                // そのまま使用する
                Dictionary<string, object> positionDict = null;
                if (position != null && position is Dictionary<string, object> posDict)
                {
                    positionDict = posDict;
                }
                else if (position != null && position is Dictionary<string, float> posFloatDict)
                {
                    positionDict = new Dictionary<string, object>();
                    foreach (var kvp in posFloatDict)
                    {
                        positionDict[kvp.Key] = kvp.Value;
                    }
                }
                else if (position != null)
                {
                    // positionがオブジェクトの場合、JSONシリアライズして取得
                    positionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(position)) ?? new Dictionary<string, object>();
                }

                // metadataとpositionを結合
                object combinedData = null;
                if (metadata != null && positionDict != null)
                {
                    var metaDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(metadata)) ?? new Dictionary<string, object>();
                    foreach (var kvp in positionDict)
                    {
                        metaDict[kvp.Key] = kvp.Value;
                    }
                    combinedData = metaDict;
                }
                else if (positionDict != null)
                {
                    combinedData = positionDict;
                }
                else if (metadata != null)
                {
                    combinedData = metadata;
                }

                if (combinedData == null)
                {
                    metadataBytes[i] = Array.Empty<byte>();
                    continue;
                }

                var json = JsonConvert.SerializeObject(combinedData);
                var bytes = Encoding.UTF8.GetBytes(json);
                metadataBytes[i] = bytes;
                totalMetadataLen += bytes.Length;

                // スクリーンショットサイズを計算
                if (entry.Screenshots != null)
                {
                    foreach (var screenshot in entry.Screenshots)
                    {
                        if (screenshot != null)
                        {
                            totalScreenshotLen += screenshot.Length + 4; // 4 bytes for length prefix
                        }
                    }
                }
            }

            // 容量見積もり:
            //   Header: 9 bytes (Magic 4 + Version 1 + RecordCount 4)
            //   Records: (4+4+8+4+4+1) * N + metadata総和 + screenshots総和
            //          = 25 * N + totalMetadataLen + totalScreenshotLen
            int capacity = 9 + (25 * buffer.Length) + totalMetadataLen + totalScreenshotLen;

            using var ms = new MemoryStream(capacity);
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // ========== Header ==========
            bw.Write(Encoding.ASCII.GetBytes("LSEV"));  // Magic: 4 bytes
            bw.Write((byte)2);                           // Version: 1 byte (V2)
            bw.Write((uint)buffer.Length);               // RecordCount: 4 bytes

            // ========== Records ==========
            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];

                // イベントタイプを文字列として記録
                var eventTypeBytes = Encoding.UTF8.GetBytes(entry.EventType);
                bw.Write((uint)eventTypeBytes.Length);   // event_type_len: 4 bytes
                bw.Write(eventTypeBytes);                 // event_type: variable

                bw.Write(entry.OffsetTimeStamp);         // offset_timestamp: 8 bytes
                bw.Write((uint)entry.Player);            // player: 4 bytes

                // メタデータ
                var mBytes = metadataBytes[i] ?? Array.Empty<byte>();
                const int MaxMetadataBytes = 1 * 1024 * 1024;
                if (mBytes.Length > MaxMetadataBytes)
                {
                    Debug.LogWarning($"GeneralEvent metadata too large ({mBytes.Length}), trimming to {MaxMetadataBytes}.");
                    Array.Resize(ref mBytes, MaxMetadataBytes);
                }

                bw.Write((uint)mBytes.Length);           // metadata_len: 4 bytes
                if (mBytes.Length > 0)
                {
                    bw.Write(mBytes);                    // metadata: variable
                }

                // スクリーンショット
                var screenshots = entry.Screenshots;
                byte screenshotCount = 0;
                if (screenshots != null)
                {
                    screenshotCount = (byte)Math.Min(screenshots.Length, 255);
                }
                bw.Write(screenshotCount);               // screenshot_count: 1 byte

                if (screenshotCount > 0)
                {
                    for (int j = 0; j < screenshotCount; j++)
                    {
                        var screenshot = screenshots[j];
                        if (screenshot != null && screenshot.Length > 0)
                        {
                            // 上限チェック (500KB per screenshot)
                            const int MaxScreenshotBytes = 500 * 1024;
                            if (screenshot.Length > MaxScreenshotBytes)
                            {
                                Debug.LogWarning($"Screenshot {j} too large ({screenshot.Length}), trimming to {MaxScreenshotBytes}.");
                                Array.Resize(ref screenshot, MaxScreenshotBytes);
                            }

                            bw.Write((uint)screenshot.Length);   // image_len: 4 bytes
                            bw.Write(screenshot);                 // image_data: variable
                        }
                        else
                        {
                            bw.Write((uint)0);                   // empty screenshot
                        }
                    }
                }
            }

            bw.Flush();
            return ms.ToArray();
        }
    }
}
