using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Examples
{
    /// <summary>
    /// PositionLoggerの使い方を示すサンプルスクリプト
    /// プレイヤーの位置を定期的に記録してアップロードする例
    /// </summary>
    public class PositionLoggerExample : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private int playerId = 0;

        [Header("Logging Settings")]
        [SerializeField] private int bufferSize = 1000;
        [SerializeField] private int recordIntervalMilliseconds = 250;
        [SerializeField] private int uploadIntervalSeconds = 10;

        private PositionLogger positionLogger;
        private Session currentSession;
        private float uploadTimer;
        private bool isSessionActive = false;

        private void Start()
        {
            // PositionLoggerの初期化
            positionLogger = new PositionLogger(bufferSize);

            // 位置取得コールバックの設定
            positionLogger.OnLogPosition = GetPlayerPositions;

            // ロギング開始
            positionLogger.StartLogging(recordIntervalMilliseconds);
            Debug.Log($"PositionLogger started (interval: {recordIntervalMilliseconds}ms)");

            // セッション作成（簡略化のため、既にセッションが作成されていると仮定）
            // 実際には LudiscanBasicExample のようにセッションを作成する
            isSessionActive = true;
        }

        private void Update()
        {
            if (!isSessionActive) return;

            // 定期的にバッファの内容をアップロード
            uploadTimer += Time.deltaTime;
            if (uploadTimer >= uploadIntervalSeconds)
            {
                uploadTimer = 0f;
                _ = UploadPositionData();
            }
        }

        /// <summary>
        /// プレイヤーの位置情報を取得
        /// PositionLoggerから呼び出されるコールバック
        /// </summary>
        private List<PositionEntry> GetPlayerPositions()
        {
            if (playerTransform == null)
            {
                return new List<PositionEntry>();
            }

            var positions = new List<PositionEntry>
            {
                new PositionEntry
                {
                    PlayerId = playerId,
                    Position = playerTransform.position
                }
            };

            return positions;
        }

        /// <summary>
        /// バッファに蓄積された位置データをアップロード
        /// </summary>
        private async Task UploadPositionData()
        {
            if (!isSessionActive || !LudiscanClient.IsInitialized)
            {
                return;
            }

            try
            {
                var buffer = positionLogger.Buffer;
                if (buffer == null || buffer.Length == 0)
                {
                    Debug.Log("No position data to upload");
                    return;
                }

                // 空でないエントリをカウント
                int validEntries = 0;
                foreach (var entry in buffer)
                {
                    if (entry.PlayerId >= 0) // 有効なエントリかチェック
                    {
                        validEntries++;
                    }
                }

                Debug.Log($"Uploading {validEntries} position entries...");
                await LudiscanClient.Instance.UploadPosition(currentSession, buffer);
                Debug.Log("Position data uploaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upload position data: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            // ロギング停止
            if (positionLogger != null)
            {
                positionLogger.StopLogging();
                Debug.Log("PositionLogger stopped");
            }
        }

        // デバッグ用: 手動でアップロードをトリガー
        [ContextMenu("Upload Position Data Now")]
        private void ManualUpload()
        {
            _ = UploadPositionData();
        }

        // デバッグ用: ロギングの開始/停止を切り替え
        [ContextMenu("Toggle Logging")]
        private void ToggleLogging()
        {
            if (positionLogger.IsLoggingStarted)
            {
                positionLogger.StopLogging();
                Debug.Log("Logging stopped");
            }
            else
            {
                positionLogger.StartLogging(recordIntervalMilliseconds);
                Debug.Log("Logging started");
            }
        }
    }
}
