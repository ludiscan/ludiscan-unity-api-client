using System.Collections;
using LudiscanApiClient.Runtime.ApiClient;
using UnityEngine;

namespace LudiscanApiClient.Examples
{
    /// <summary>
    /// スクリーンショット機能のテストスクリプト
    /// death/successイベント発火時にスクリーンショットが正しくキャプチャされるかを確認
    /// </summary>
    public class ScreenshotFeatureTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool autoRunTest = false;
        [SerializeField] private float testDelay = 3f;

        private long sessionStartTime;

        private void Start()
        {
            // GeneralEventLoggerの初期化
            if (!GeneralEventLogger.IsInitialized)
            {
                GeneralEventLogger.Initialize();
                Debug.Log("[Test] GeneralEventLogger initialized");
            }

            // EventScreenshotCaptureの初期化
            if (!EventScreenshotCapture.IsInitialized)
            {
                EventScreenshotCapture.Initialize(autoStartCapture: true);
                EventScreenshotCapture.Instance.ConfigureCapture(
                    interval: 0.5f,
                    bufferSize: 5,
                    scale: 0.5f,
                    quality: 75
                );
                Debug.Log("[Test] EventScreenshotCapture initialized");
            }

            sessionStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 自動テスト実行
            if (autoRunTest)
            {
                StartCoroutine(RunAutoTest());
            }
        }

        private IEnumerator RunAutoTest()
        {
            Debug.Log("[Test] Waiting for screenshot buffer to fill...");
            yield return new WaitForSeconds(testDelay);

            Debug.Log("[Test] Running screenshot feature test...");

            // Test 1: death イベントでスクリーンショットがキャプチャされるか
            TestDeathEventWithScreenshots();
            yield return new WaitForSeconds(1f);

            // Test 2: success イベントでスクリーンショットがキャプチャされるか
            TestSuccessEventWithScreenshots();
            yield return new WaitForSeconds(1f);

            // Test 3: 他のイベントではスクリーンショットがキャプチャされないか
            TestOtherEventWithoutScreenshots();
            yield return new WaitForSeconds(1f);

            // Test 4: スクリーンショット無効時
            TestScreenshotDisabled();

            Debug.Log("[Test] All tests completed!");
        }

        [ContextMenu("Test: Death Event with Screenshots")]
        private void TestDeathEventWithScreenshots()
        {
            Debug.Log("[Test 1] Testing death event with screenshots...");

            var metadata = new { death_cause = "test_fall", lives_remaining = 2 };
            GeneralEventLogger.Instance.AddLog(
                "death",
                metadata,
                GetOffsetTimestamp(),
                transform.position,
                player: 0
            );

            var logs = GeneralEventLogger.Instance.GetLogsAndClear();
            if (logs.Length > 0 && logs[0].Screenshots != null && logs[0].Screenshots.Length > 0)
            {
                Debug.Log($"[Test 1] ✓ PASS: Death event captured {logs[0].Screenshots.Length} screenshots");
                for (int i = 0; i < logs[0].Screenshots.Length; i++)
                {
                    Debug.Log($"  Screenshot {i + 1}: {logs[0].Screenshots[i].Length / 1024f:F2} KB");
                }
            }
            else
            {
                Debug.LogWarning("[Test 1] ✗ FAIL: No screenshots captured for death event");
            }
        }

        [ContextMenu("Test: Success Event with Screenshots")]
        private void TestSuccessEventWithScreenshots()
        {
            Debug.Log("[Test 2] Testing success event with screenshots...");

            var metadata = new { completion_time = 120.5f, ranking = 1 };
            GeneralEventLogger.Instance.AddLog(
                "success",
                metadata,
                GetOffsetTimestamp(),
                transform.position,
                player: 0
            );

            var logs = GeneralEventLogger.Instance.GetLogsAndClear();
            if (logs.Length > 0 && logs[0].Screenshots != null && logs[0].Screenshots.Length > 0)
            {
                Debug.Log($"[Test 2] ✓ PASS: Success event captured {logs[0].Screenshots.Length} screenshots");
            }
            else
            {
                Debug.LogWarning("[Test 2] ✗ FAIL: No screenshots captured for success event");
            }
        }

        [ContextMenu("Test: Other Event without Screenshots")]
        private void TestOtherEventWithoutScreenshots()
        {
            Debug.Log("[Test 3] Testing other event (should NOT capture screenshots)...");

            var metadata = new { score = 1000 };
            GeneralEventLogger.Instance.AddLog(
                "score_milestone",
                metadata,
                GetOffsetTimestamp(),
                transform.position,
                player: 0
            );

            var logs = GeneralEventLogger.Instance.GetLogsAndClear();
            if (logs.Length > 0 && (logs[0].Screenshots == null || logs[0].Screenshots.Length == 0))
            {
                Debug.Log("[Test 3] ✓ PASS: Other event correctly did NOT capture screenshots");
            }
            else
            {
                Debug.LogWarning("[Test 3] ✗ FAIL: Other event incorrectly captured screenshots");
            }
        }

        [ContextMenu("Test: Screenshot Disabled")]
        private void TestScreenshotDisabled()
        {
            Debug.Log("[Test 4] Testing with screenshots disabled...");

            GeneralEventLogger.Instance.EnableScreenshots = false;

            var metadata = new { death_cause = "test" };
            GeneralEventLogger.Instance.AddLog(
                "death",
                metadata,
                GetOffsetTimestamp(),
                transform.position,
                player: 0
            );

            var logs = GeneralEventLogger.Instance.GetLogsAndClear();
            if (logs.Length > 0 && (logs[0].Screenshots == null || logs[0].Screenshots.Length == 0))
            {
                Debug.Log("[Test 4] ✓ PASS: Screenshots correctly disabled");
            }
            else
            {
                Debug.LogWarning("[Test 4] ✗ FAIL: Screenshots were captured despite being disabled");
            }

            // Re-enable for future tests
            GeneralEventLogger.Instance.EnableScreenshots = true;
        }

        [ContextMenu("Test: Custom Event Type")]
        private void TestCustomEventType()
        {
            Debug.Log("[Test 5] Testing custom event type with screenshots...");

            // "boss_defeated"イベントをスクリーンショット対象に追加
            GeneralEventLogger.Instance.ScreenshotEventTypes.Add("boss_defeated");

            var metadata = new { boss_name = "TestBoss", difficulty = "hard" };
            GeneralEventLogger.Instance.AddLog(
                "boss_defeated",
                metadata,
                GetOffsetTimestamp(),
                transform.position,
                player: 0
            );

            var logs = GeneralEventLogger.Instance.GetLogsAndClear();
            if (logs.Length > 0 && logs[0].Screenshots != null && logs[0].Screenshots.Length > 0)
            {
                Debug.Log($"[Test 5] ✓ PASS: Custom event type captured {logs[0].Screenshots.Length} screenshots");
            }
            else
            {
                Debug.LogWarning("[Test 5] ✗ FAIL: No screenshots captured for custom event type");
            }
        }

        [ContextMenu("Show Current Buffer Status")]
        private void ShowBufferStatus()
        {
            if (EventScreenshotCapture.IsInitialized)
            {
                int count = EventScreenshotCapture.Instance.BufferedCount;
                Debug.Log($"[Info] Current screenshot buffer: {count} screenshots");
            }
            else
            {
                Debug.LogWarning("[Info] EventScreenshotCapture is not initialized");
            }
        }

        private ulong GetOffsetTimestamp()
        {
            long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (ulong)(currentTime - sessionStartTime);
        }
    }
}
