using System;
using System.Collections.Generic;
using LudiscanApiClient.Runtime.ApiClient.Model;
using Matuyuhi.LudiscanApi.Client.Model;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// フィールドオブジェクトのログを管理するクラス
    /// イベント発生時に AddLog() を呼び出してバッファに蓄積し、
    /// セッション終了時に GetLogsAndClear() でアップロード
    /// </summary>
    public class FieldObjectLogger
    {
        private List<FieldObjectEntity> buffer;
        private readonly object lockObject = new object();

        /// <summary>
        /// セッション開始時刻（Unix time milliseconds）
        /// </summary>
        public long SessionStartTime { get; private set; }

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

        public FieldObjectLogger(int initialCapacity = 1000)
        {
            buffer = new List<FieldObjectEntity>(initialCapacity);
            SessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// セッション開始時刻をリセット
        /// </summary>
        public void ResetSessionStartTime()
        {
            SessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// フィールドオブジェクトのログを追加
        /// </summary>
        /// <param name="objectId">オブジェクトID</param>
        /// <param name="objectType">オブジェクトタイプ</param>
        /// <param name="eventType">イベントタイプ</param>
        /// <param name="position">Unity座標</param>
        /// <param name="status">追加データ（nullも可）</param>
        public void AddLog(
            string objectId,
            string objectType,
            FieldObjectLogDto.EventTypeEnum eventType,
            Vector3 position,
            uint offsetTimestamp,
            object status = null
        )
        {
            if (string.IsNullOrEmpty(objectId))
            {
                Debug.LogWarning("FieldObjectLogger: objectId is null or empty");
                return;
            }

            if (string.IsNullOrEmpty(objectType))
            {
                Debug.LogWarning("FieldObjectLogger: objectType is null or empty");
                return;
            }

            // Unity座標をAPI座標に変換 (cm単位)
            // Unity: X, Y, Z
            // API: X=Z*100, Y=X*100, Z=Y*100
            var apiX = position.z * 100;
            var apiY = position.x * 100;
            var apiZ = position.y * 100;

            var entry = new FieldObjectEntity {
                ObjectId = objectId,
                ObjectType = objectType,
                X = apiX,
                Y = apiY,
                Z = apiZ,
                OffsetTimeStamp = offsetTimestamp,
                EventType = eventType,
                Status = status
            };

            lock (lockObject)
            {
                buffer.Add(entry);
            }
        }

        /// <summary>
        /// アイテムが出現した時
        /// </summary>
        public void LogItemSpawn(string itemId, string itemType, Vector3 position, uint offsetTimestamp, object metadata = null)
        {
            AddLog(itemId, itemType, FieldObjectLogDto.EventTypeEnum.Spawn, position, offsetTimestamp, metadata);
        }

        /// <summary>
        /// アイテムが消滅した時
        /// </summary>
        public void LogItemDespawn(string itemId, string itemType, Vector3 position, uint offsetTimestamp, int pickedByPlayer = -1)
        {
            var status = pickedByPlayer >= 0 ? new { picked_by = pickedByPlayer } : null;
            AddLog(itemId, itemType, FieldObjectLogDto.EventTypeEnum.Despawn, position, offsetTimestamp, status);
        }

        /// <summary>
        /// 敵が出現した時
        /// </summary>
        public void LogEnemySpawn(string enemyId, string enemyType, Vector3 position, uint offsetTimestamp, object metadata = null)
        {
            AddLog(enemyId, enemyType, FieldObjectLogDto.EventTypeEnum.Spawn, position, offsetTimestamp, metadata);
        }

        /// <summary>
        /// 敵が移動した時
        /// </summary>
        public void LogEnemyMove(string enemyId, string enemyType, Vector3 position, uint offsetTimestamp)
        {
            AddLog(enemyId, enemyType, FieldObjectLogDto.EventTypeEnum.Move, position, offsetTimestamp);
        }

        /// <summary>
        /// 敵が死亡した時
        /// </summary>
        public void LogEnemyDeath(string enemyId, string enemyType, Vector3 position, uint offsetTimestamp, int killedByPlayer = -1)
        {
            var status = killedByPlayer >= 0 ? new { killed_by = killedByPlayer } : null;
            AddLog(enemyId, enemyType, FieldObjectLogDto.EventTypeEnum.Despawn, position, offsetTimestamp, status);
        }

        /// <summary>
        /// オブジェクトの状態を更新した時
        /// </summary>
        public void LogObjectUpdate(string objectId, string objectType, Vector3 position, uint offsetTimestamp, object status)
        {
            AddLog(objectId, objectType, FieldObjectLogDto.EventTypeEnum.Update, position, offsetTimestamp, status);
        }

        /// <summary>
        /// ログを取得してバッファをクリア
        /// </summary>
        public FieldObjectEntity[] GetLogsAndClear()
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
