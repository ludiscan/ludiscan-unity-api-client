using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using Matuyuhi.LudiscanApi.Client.Model;
using UnityEngine;

namespace LudiscanApiClient.Examples
{
    /// <summary>
    /// FieldObjectLoggerの使い方を示すサンプルスクリプト
    /// アイテム、敵などのフィールドオブジェクトのイベントを記録する例
    /// </summary>
    public class FieldObjectLoggerExample : MonoBehaviour
    {
        [Header("Logger Settings")]
        [SerializeField] private int initialCapacity = 1000;

        private FieldObjectLogger fieldObjectLogger;
        private Session currentSession;
        private bool isSessionActive = false;
        private long sessionStartTime;

        private void Start()
        {
            // FieldObjectLoggerの初期化
            fieldObjectLogger = new FieldObjectLogger(initialCapacity);
            sessionStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Debug.Log("FieldObjectLogger initialized");

            // セッション作成（簡略化のため、既にセッションが作成されていると仮定）
            isSessionActive = true;
        }

        /// <summary>
        /// 現在のオフセットタイムスタンプを取得
        /// </summary>
        private uint GetOffsetTimestamp()
        {
            long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (uint)(currentTime - sessionStartTime);
        }

        #region アイテム関連のイベント例

        /// <summary>
        /// アイテムが出現したときの記録例
        /// </summary>
        public void OnItemSpawned(string itemId, string itemType, Vector3 position)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            var metadata = new { spawn_reason = "game_start" };

            fieldObjectLogger.LogItemSpawn(itemId, itemType, position, offsetTimestamp, metadata);
            Debug.Log($"Item spawned: {itemType} at {position}");
        }

        /// <summary>
        /// アイテムがプレイヤーに取得されたときの記録例
        /// </summary>
        public void OnItemPickedUp(string itemId, string itemType, Vector3 position, int playerId)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            fieldObjectLogger.LogItemDespawn(itemId, itemType, position, offsetTimestamp, playerId);
            Debug.Log($"Item picked up: {itemType} by player {playerId}");
        }

        /// <summary>
        /// アイテムが自然消滅したときの記録例
        /// </summary>
        public void OnItemExpired(string itemId, string itemType, Vector3 position)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            fieldObjectLogger.LogItemDespawn(itemId, itemType, position, offsetTimestamp);
            Debug.Log($"Item expired: {itemType}");
        }

        #endregion

        #region 敵関連のイベント例

        /// <summary>
        /// 敵が出現したときの記録例
        /// </summary>
        public void OnEnemySpawned(string enemyId, string enemyType, Vector3 position)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            var metadata = new { difficulty = "normal", spawn_type = "wave" };

            fieldObjectLogger.LogEnemySpawn(enemyId, enemyType, position, offsetTimestamp, metadata);
            Debug.Log($"Enemy spawned: {enemyType} at {position}");
        }

        /// <summary>
        /// 敵が移動したときの記録例
        /// </summary>
        public void OnEnemyMoved(string enemyId, string enemyType, Vector3 position)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            fieldObjectLogger.LogEnemyMove(enemyId, enemyType, position, offsetTimestamp);
        }

        /// <summary>
        /// 敵が倒されたときの記録例
        /// </summary>
        public void OnEnemyDefeated(string enemyId, string enemyType, Vector3 position, int killedByPlayerId)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            fieldObjectLogger.LogEnemyDeath(enemyId, enemyType, position, offsetTimestamp, killedByPlayerId);
            Debug.Log($"Enemy defeated: {enemyType} by player {killedByPlayerId}");
        }

        #endregion

        #region カスタムオブジェクトのイベント例

        /// <summary>
        /// カスタムオブジェクトの状態更新の記録例
        /// </summary>
        public void OnObjectStateChanged(string objectId, string objectType, Vector3 position, object status)
        {
            if (!isSessionActive) return;

            uint offsetTimestamp = GetOffsetTimestamp();
            fieldObjectLogger.LogObjectUpdate(objectId, objectType, position, offsetTimestamp, status);
            Debug.Log($"Object updated: {objectType}");
        }

        #endregion

        /// <summary>
        /// 蓄積されたログをアップロードしてクリア
        /// </summary>
        public async Task UploadAndClearLogs()
        {
            if (!isSessionActive || !LudiscanClient.IsInitialized)
            {
                Debug.LogWarning("Session not active or client not initialized");
                return;
            }

            try
            {
                var logs = fieldObjectLogger.GetLogsAndClear();
                if (logs.Length == 0)
                {
                    Debug.Log("No field object logs to upload");
                    return;
                }

                Debug.Log($"Uploading {logs.Length} field object logs...");
                await LudiscanClient.Instance.UploadFieldObjectLogs(currentSession, logs);
                Debug.Log("Field object logs uploaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upload field object logs: {e.Message}");
            }
        }

        // デバッグ用: 手動でアップロードをトリガー
        [ContextMenu("Upload Field Object Logs")]
        private void ManualUpload()
        {
            _ = UploadAndClearLogs();
        }

        // デバッグ用: テストアイテムを生成
        [ContextMenu("Test: Spawn Item")]
        private void TestSpawnItem()
        {
            string itemId = System.Guid.NewGuid().ToString();
            OnItemSpawned(itemId, "health_potion", transform.position);
        }

        // デバッグ用: テスト敵を生成
        [ContextMenu("Test: Spawn Enemy")]
        private void TestSpawnEnemy()
        {
            string enemyId = System.Guid.NewGuid().ToString();
            OnEnemySpawned(enemyId, "goblin", transform.position);
        }

        private void OnDestroy()
        {
            // セッション終了時に残っているログをアップロード
            if (isSessionActive && fieldObjectLogger.LogCount > 0)
            {
                _ = UploadAndClearLogs();
            }
        }
    }
}
