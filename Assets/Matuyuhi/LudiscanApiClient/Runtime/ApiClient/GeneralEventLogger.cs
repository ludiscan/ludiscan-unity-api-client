using System;
using System.Collections.Generic;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// 一般イベントログを管理するクラス
    /// シングルトンパターンで実装されており、Initialize()で初期化後、Instanceプロパティでアクセスします
    /// RouteCoach 拡張イベント（player_spawn, collision_attempt, score_milestone など）を
    /// バッファに蓄積し、セッション終了時にまとめてアップロード
    /// </summary>
    public class GeneralEventLogger
    {
        private static GeneralEventLogger _instance;

        private List<GeneralEventEntity> buffer;
        private readonly object lockObject = new object();

        /// <summary>
        /// スクリーンショット機能を有効にするかどうか
        /// </summary>
        public bool EnableScreenshots { get; set; } = true;

        /// <summary>
        /// スクリーンショットをキャプチャする対象のイベントタイプ
        /// デフォルトは "death" と "success"
        /// </summary>
        public HashSet<string> ScreenshotEventTypes { get; set; } = new HashSet<string> { "death", "success" };

        /// <summary>
        /// イベント発火時にキャプチャするスクリーンショットの枚数
        /// </summary>
        public int ScreenshotCount { get; set; } = 4;

        /// <summary>
        /// GeneralEventLoggerのシングルトンインスタンスを取得します
        /// Initialize()で初期化されていない場合は例外をスローします
        /// </summary>
        /// <exception cref="InvalidOperationException">ロガーが初期化されていない場合</exception>
        public static GeneralEventLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "GeneralEventLogger is not initialized. Call Initialize(int initialCapacity) first."
                    );
                }
                return _instance;
            }
        }

        /// <summary>
        /// GeneralEventLoggerが初期化済みかどうかを確認します
        /// </summary>
        /// <returns>初期化済みの場合はtrue、未初期化の場合はfalse</returns>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        /// GeneralEventLoggerを初期化します
        /// 既に初期化されている場合は警告を出力して再初期化します
        /// </summary>
        /// <param name="initialCapacity">バッファの初期容量（デフォルト: 2000）</param>
        public static void Initialize(int initialCapacity = 2000)
        {
            if (_instance != null)
            {
                Debug.LogWarning("GeneralEventLogger is already initialized. Reinitializing...");
            }
            _instance = new GeneralEventLogger(initialCapacity);
        }

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

        /// <summary>
        /// プライベートコンストラクタ
        /// </summary>
        /// <param name="initialCapacity">バッファの初期容量</param>
        private GeneralEventLogger(int initialCapacity = 2000)
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
        /// <param name="captureScreenshot">スクリーンショットを強制的にキャプチャする（null=自動判定）</param>
        public void AddLog(
            string eventType,
            object metadata,
            ulong offsetTimestamp,
            Vector3 position,
            int player = 0,
            bool? captureScreenshot = null
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

            // スクリーンショットキャプチャの判定
            byte[][] screenshots = null;
            bool shouldCaptureScreenshot = captureScreenshot ??
                (EnableScreenshots && ScreenshotEventTypes.Contains(eventType));

            if (shouldCaptureScreenshot && EventScreenshotCapture.IsInitialized)
            {
                try
                {
                    screenshots = EventScreenshotCapture.Instance.GetRecentScreenshots(ScreenshotCount);
                    if (screenshots != null && screenshots.Length > 0)
                    {
                        Debug.Log($"GeneralEventLogger: Captured {screenshots.Length} screenshots for event '{eventType}'");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GeneralEventLogger: Failed to capture screenshots for event '{eventType}': {ex.Message}");
                }
            }

            var entry = new GeneralEventEntity
            {
                EventType = eventType,
                Metadata = metadata,
                OffsetTimeStamp = offsetTimestamp,
                Player = player,
                Position = transformedPosition,
                Screenshots = screenshots
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
