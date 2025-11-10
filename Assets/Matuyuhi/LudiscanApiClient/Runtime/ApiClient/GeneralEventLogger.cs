using System.Collections.Generic;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// 一般イベントログを管理するクラス
    /// RouteCoach 拡張イベント（player_spawn, collision_attempt, score_milestone など）を
    /// バッファに蓄積し、セッション終了時にまとめてアップロード
    /// </summary>
    public class GeneralEventLogger
    {
        private List<GeneralEventEntity> buffer;
        private readonly object lockObject = new object();

        /// <summary>
        /// ログ数
        /// </summary>
        public int LogCount
        {
            get
            {
                lock (lockObject)
                {
                    return buffer.Count;
                }
            }
        }

        public GeneralEventLogger(int initialCapacity = 2000)
        {
            buffer = new List<GeneralEventEntity>(initialCapacity);
        }

        /// <summary>
        /// 一般イベントのログを追加
        /// </summary>
        /// <param name="eventType">イベントタイプ（snake_case）</param>
        /// <param name="metadata">イベントのメタデータ</param>
        /// <param name="offsetTimestamp">ゲーム開始からのオフセット時間（ミリ秒）</param>
        /// <param name="position">イベントの位置情報（Vector3またはオブジェクト）</param>
        /// <param name="player">プレイヤーID</param>
        public void AddLog(
            string eventType,
            object metadata,
            ulong offsetTimestamp,
            Vector3 position,
            int player = 0
        )
        {
            if (string.IsNullOrEmpty(eventType))
            {
                Debug.LogWarning("GeneralEventLogger: eventType is null or empty");
                return;
            }

            // Unity座標をAPI座標に変換 (cm単位)
            // Unity: X, Y, Z
            // API: X=Z*100, Y=X*100, Z=Y*100
            object transformedPosition = null;
            transformedPosition = new Dictionary<string, float>
            {
                { "x", position.z * 100 },
                { "y", position.x * 100 },
                { "z", position.y * 100 }
            };

            var entry = new GeneralEventEntity
            {
                EventType = eventType,
                Metadata = metadata,
                OffsetTimeStamp = offsetTimestamp,
                Player = player,
                Position = transformedPosition
            };

            lock (lockObject)
            {
                buffer.Add(entry);
            }
        }

        /// <summary>
        /// ログを取得してバッファをクリア
        /// </summary>
        public GeneralEventEntity[] GetLogsAndClear()
        {
            lock (lockObject)
            {
                var result = buffer.ToArray();
                buffer.Clear();
                return result;
            }
        }

        /// <summary>
        /// バッファをクリア
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                buffer.Clear();
            }
        }
    }
}
