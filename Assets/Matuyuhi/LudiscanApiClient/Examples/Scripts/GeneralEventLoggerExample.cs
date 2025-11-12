using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Examples
{
    /// <summary>
    /// GeneralEventLoggerの使い方を示すサンプルスクリプト
    /// ゲーム内の一般的なイベントを記録する例
    /// </summary>
    public class GeneralEventLoggerExample : MonoBehaviour
    {
        [Header("Logger Settings")]
        [SerializeField] private int initialCapacity = 2000;
        [SerializeField] private int defaultPlayerId = 0;

        private Session currentSession;
        private bool isSessionActive = false;
        private long sessionStartTime;

        private void Start()
        {
            // GeneralEventLoggerの初期化（シングルトン）
            GeneralEventLogger.Initialize(initialCapacity);
            sessionStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Debug.Log("GeneralEventLogger initialized");

            // セッション作成（簡略化のため、既にセッションが作成されていると仮定）
            isSessionActive = true;
        }

        /// <summary>
        /// 現在のオフセットタイムスタンプを取得
        /// </summary>
        private ulong GetOffsetTimestamp()
        {
            long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (ulong)(currentTime - sessionStartTime);
        }

        #region プレイヤーイベント

        /// <summary>
        /// プレイヤーがスポーンしたときの記録例
        /// </summary>
        public void OnPlayerSpawned(int playerId, Vector3 position)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                spawn_point = "checkpoint_1",
                is_respawn = false
            };

            GeneralEventLogger.Instance.AddLog(
                "player_spawn",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Player spawned: {playerId} at {position}");
        }

        /// <summary>
        /// プレイヤーがリスポーンしたときの記録例
        /// </summary>
        public void OnPlayerRespawned(int playerId, Vector3 position, string deathReason)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                spawn_point = "last_checkpoint",
                is_respawn = true,
                death_reason = deathReason
            };

            GeneralEventLogger.Instance.AddLog(
                "player_spawn",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Player respawned: {playerId} at {position}");
        }

        /// <summary>
        /// プレイヤーが死亡したときの記録例
        /// </summary>
        public void OnPlayerDeath(int playerId, Vector3 position, string deathCause)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                death_cause = deathCause,
                lives_remaining = 2
            };

            GeneralEventLogger.Instance.AddLog(
                "death",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Player died: {playerId}, cause: {deathCause}");
        }

        #endregion

        #region ゲームプレイイベント

        /// <summary>
        /// ゲームフェーズが変更されたときの記録例
        /// </summary>
        public void OnGamePhaseChanged(string fromPhase, string toPhase, Vector3 position)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                from_phase = fromPhase,
                to_phase = toPhase,
                elapsed_time = Time.time
            };

            GeneralEventLogger.Instance.AddLog(
                "game_phase_changed",
                metadata,
                GetOffsetTimestamp(),
                position,
                defaultPlayerId
            );

            Debug.Log($"Game phase changed: {fromPhase} -> {toPhase}");
        }

        /// <summary>
        /// スコアマイルストーンに到達したときの記録例
        /// </summary>
        public void OnScoreMilestone(int playerId, Vector3 position, int currentScore, int milestone)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                current_score = currentScore,
                milestone = milestone,
                milestone_type = "score_reached"
            };

            GeneralEventLogger.Instance.AddLog(
                "score_milestone",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Score milestone reached: {milestone} by player {playerId}");
        }

        /// <summary>
        /// プレイヤーがゴールに到達したときの記録例
        /// </summary>
        public void OnPlayerSuccess(int playerId, Vector3 position, float completionTime)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                completion_time = completionTime,
                ranking = 1
            };

            GeneralEventLogger.Instance.AddLog(
                "success",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Player succeeded: {playerId}, time: {completionTime}s");
        }

        #endregion

        #region アイテム/アビリティイベント

        /// <summary>
        /// プレイヤーがアイテムを取得したときの記録例
        /// </summary>
        public void OnItemCollected(int playerId, Vector3 position, string itemType)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                item_type = itemType,
                collect_method = "pickup"
            };

            GeneralEventLogger.Instance.AddLog(
                "get_hand_change_item",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Item collected: {itemType} by player {playerId}");
        }

        /// <summary>
        /// プレイヤーがダッシュアイテムを使用したときの記録例
        /// </summary>
        public void OnDashItemUsed(int playerId, Vector3 position, string direction)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                dash_direction = direction,
                dash_distance = 5.0f
            };

            GeneralEventLogger.Instance.AddLog(
                "use_dash_item",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Dash item used by player {playerId}");
        }

        /// <summary>
        /// プレイヤーの手が変更されたときの記録例
        /// </summary>
        public void OnHandChanged(int playerId, Vector3 position, string fromHand, string toHand)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                from_hand = fromHand,
                to_hand = toHand,
                change_reason = "item_pickup"
            };

            GeneralEventLogger.Instance.AddLog(
                "hand_changed",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Hand changed: {fromHand} -> {toHand}");
        }

        #endregion

        #region 衝突/戦闘イベント

        /// <summary>
        /// 敵との衝突を試みたときの記録例
        /// </summary>
        public void OnCollisionAttempt(int playerId, Vector3 position, string targetType, bool success)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                target_type = targetType,
                collision_success = success,
                player_velocity = 10.5f
            };

            GeneralEventLogger.Instance.AddLog(
                "collision_attempt",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Collision attempt: player {playerId} vs {targetType}, success: {success}");
        }

        /// <summary>
        /// プレイヤーが敵を捕まえたときの記録例
        /// </summary>
        public void OnPlayerCatch(int playerId, Vector3 position, string enemyType)
        {
            if (!isSessionActive || !GeneralEventLogger.IsInitialized) return;

            var metadata = new
            {
                enemy_type = enemyType,
                catch_method = "collision"
            };

            GeneralEventLogger.Instance.AddLog(
                "player_catch",
                metadata,
                GetOffsetTimestamp(),
                position,
                playerId
            );

            Debug.Log($"Player caught enemy: {enemyType}");
        }

        #endregion

        /// <summary>
        /// 蓄積されたログをアップロードしてクリア
        /// </summary>
        public async Task UploadAndClearLogs()
        {
            if (!isSessionActive || !LudiscanClient.IsInitialized || !GeneralEventLogger.IsInitialized)
            {
                Debug.LogWarning("Session not active or client not initialized");
                return;
            }

            try
            {
                var logs = GeneralEventLogger.Instance.GetLogsAndClear();
                if (logs.Length == 0)
                {
                    Debug.Log("No general event logs to upload");
                    return;
                }

                Debug.Log($"Uploading {logs.Length} general event logs...");
                await LudiscanClient.Instance.UploadGeneralEventLogs(currentSession, logs);
                Debug.Log("General event logs uploaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upload general event logs: {e.Message}");
            }
        }

        // デバッグ用: 手動でアップロードをトリガー
        [ContextMenu("Upload General Event Logs")]
        private void ManualUpload()
        {
            _ = UploadAndClearLogs();
        }

        // デバッグ用: テストイベントを記録
        [ContextMenu("Test: Log Player Spawn")]
        private void TestLogPlayerSpawn()
        {
            OnPlayerSpawned(defaultPlayerId, transform.position);
        }

        [ContextMenu("Test: Log Player Death")]
        private void TestLogPlayerDeath()
        {
            OnPlayerDeath(defaultPlayerId, transform.position, "fell_off_cliff");
        }

        [ContextMenu("Test: Log Score Milestone")]
        private void TestLogScoreMilestone()
        {
            OnScoreMilestone(defaultPlayerId, transform.position, 1000, 1000);
        }

        private void OnDestroy()
        {
            // セッション終了時に残っているログをアップロード
            if (isSessionActive && GeneralEventLogger.IsInitialized && GeneralEventLogger.Instance.LogCount > 0)
            {
                _ = UploadAndClearLogs();
            }
        }
    }
}
