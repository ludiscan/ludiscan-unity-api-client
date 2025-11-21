using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// イベント発火時のスクリーンショットキャプチャを管理するクラス
    /// 一定間隔でスクリーンショットを撮影し、循環バッファに保持します
    /// death/successなどの重要イベント発火時に、直前の数秒間のスクリーンショットを提供します
    /// </summary>
    public class EventScreenshotCapture : MonoBehaviour
    {
        private static EventScreenshotCapture _instance;

        /// <summary>
        /// EventScreenshotCaptureのシングルトンインスタンスを取得します
        /// 未初期化の場合は自動的に初期化されます
        /// </summary>
        public static EventScreenshotCapture Instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// EventScreenshotCaptureが初期化済みかどうかを確認します
        /// </summary>
        public static bool IsInitialized => _instance != null;

        [Header("Capture Settings")]
        [Tooltip("スクリーンショット撮影間隔（秒）")]
        [SerializeField]
        private float captureInterval = 0.5f;

        [Tooltip("保持するスクリーンショットの最大枚数")]
        [SerializeField]
        private int bufferSize = 4;

        [Tooltip("スクリーンショットの縮小スケール（1.0 = フル解像度、0.25 = 1/4解像度）")]
        [SerializeField]
        [Range(0.1f, 1.0f)]
        private float screenshotScale = 0.25f;

        [Tooltip("JPEG品質（1-100、低いほどファイルサイズが小さい）。0の場合はPNG形式を使用")]
        [SerializeField]
        [Range(0, 100)]
        private int jpegQuality = 50;

        [Header("Runtime Status")]
        [Tooltip("現在キャプチャ中かどうか")]
        [SerializeField]
        private bool isCapturing = false;

        private Queue<byte[]> screenshotBuffer;
        private Coroutine captureCoroutine;
        private readonly object lockObject = new object();

        /// <summary>
        /// EventScreenshotCaptureを初期化します
        /// </summary>
        /// <param name="autoStartCapture">初期化後に自動的にキャプチャを開始するか</param>
        /// <returns>初期化されたインスタンス</returns>
        public static EventScreenshotCapture Initialize(bool autoStartCapture = true)
        {
            if (_instance != null)
            {
                Debug.LogWarning("EventScreenshotCapture is already initialized.");
                return _instance;
            }

            GameObject go = new GameObject("EventScreenshotCapture");
            _instance = go.AddComponent<EventScreenshotCapture>();
            DontDestroyOnLoad(go);

            if (autoStartCapture)
            {
                _instance.StartCapture();
            }

            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            screenshotBuffer = new Queue<byte[]>(bufferSize);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                StopCapture();
                _instance = null;
            }
        }

        /// <summary>
        /// スクリーンショットのキャプチャを開始します
        /// </summary>
        /// <param name="interval">キャプチャ間隔（秒）。負の値の場合は現在の設定値を使用</param>
        public void StartCapture(float interval = -1f)
        {
            if (isCapturing)
            {
                Debug.LogWarning("EventScreenshotCapture: Already capturing screenshots.");
                return;
            }

            if (interval > 0)
            {
                captureInterval = interval;
            }

            isCapturing = true;
            captureCoroutine = StartCoroutine(CaptureScreenshotsCoroutine());
            Debug.Log($"EventScreenshotCapture: Started capturing screenshots every {captureInterval} seconds.");
        }

        /// <summary>
        /// スクリーンショットのキャプチャを停止します
        /// </summary>
        public void StopCapture()
        {
            if (!isCapturing)
            {
                return;
            }

            isCapturing = false;
            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }
            Debug.Log("EventScreenshotCapture: Stopped capturing screenshots.");
        }

        /// <summary>
        /// バッファをクリアします
        /// </summary>
        public void ClearBuffer()
        {
            lock (lockObject)
            {
                screenshotBuffer.Clear();
            }
        }

        /// <summary>
        /// 最新のスクリーンショットをN枚取得します
        /// イベント発火時に、直前の数秒間のスクリーンショットを取得する際に使用します
        /// </summary>
        /// <param name="count">取得する枚数（デフォルト: 4枚）</param>
        /// <returns>スクリーンショットのバイト配列の配列（古い順）</returns>
        public byte[][] GetRecentScreenshots(int count = 4)
        {
            lock (lockObject)
            {
                int actualCount = Mathf.Min(count, screenshotBuffer.Count);
                if (actualCount == 0)
                {
                    Debug.LogWarning("EventScreenshotCapture: No screenshots available in buffer.");
                    return new byte[0][];
                }

                byte[][] result = new byte[actualCount][];
                var tempList = new List<byte[]>(screenshotBuffer);

                // 古い順に取得（バッファの先頭から）
                int startIndex = Mathf.Max(0, tempList.Count - actualCount);
                for (int i = 0; i < actualCount; i++)
                {
                    result[i] = tempList[startIndex + i];
                }

                return result;
            }
        }

        /// <summary>
        /// 現在のバッファに保持されているスクリーンショットの枚数を取得します
        /// </summary>
        public int BufferedCount
        {
            get
            {
                lock (lockObject)
                {
                    return screenshotBuffer.Count;
                }
            }
        }

        /// <summary>
        /// キャプチャ設定を変更します
        /// </summary>
        /// <param name="interval">キャプチャ間隔（秒）</param>
        /// <param name="bufferSize">バッファサイズ</param>
        /// <param name="scale">スクリーンショットのスケール（0.1-1.0）</param>
        /// <param name="quality">JPEG品質（1-100）、0の場合はPNG</param>
        public void ConfigureCapture(float interval = -1f, int bufferSize = -1, float scale = -1f, int quality = -1)
        {
            if (interval > 0)
            {
                captureInterval = interval;
            }

            if (bufferSize > 0)
            {
                this.bufferSize = bufferSize;
                // バッファサイズ変更時は再初期化
                lock (lockObject)
                {
                    var temp = new List<byte[]>(screenshotBuffer);
                    screenshotBuffer = new Queue<byte[]>(bufferSize);

                    // 既存のスクリーンショットを新しいバッファに追加（最新のものを優先）
                    int startIndex = Mathf.Max(0, temp.Count - bufferSize);
                    for (int i = startIndex; i < temp.Count; i++)
                    {
                        screenshotBuffer.Enqueue(temp[i]);
                    }
                }
            }

            if (scale > 0)
            {
                screenshotScale = Mathf.Clamp(scale, 0.1f, 1.0f);
            }

            if (quality >= 0)
            {
                jpegQuality = Mathf.Clamp(quality, 0, 100);
            }

            // キャプチャ中の場合は再起動
            if (isCapturing)
            {
                StopCapture();
                StartCapture();
            }
        }

        private IEnumerator CaptureScreenshotsCoroutine()
        {
            while (isCapturing)
            {
                yield return new WaitForSeconds(captureInterval);
                CaptureScreenshot();
            }
        }

        private void CaptureScreenshot()
        {
            try
            {
                // スクリーンショットを撮影
                Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                if (screenshot == null)
                {
                    Debug.LogWarning("EventScreenshotCapture: Failed to capture screenshot.");
                    return;
                }

                // スケーリング処理
                Texture2D scaledScreenshot = screenshot;
                if (screenshotScale < 1.0f)
                {
                    int width = Mathf.RoundToInt(screenshot.width * screenshotScale);
                    int height = Mathf.RoundToInt(screenshot.height * screenshotScale);
                    scaledScreenshot = ResizeTexture(screenshot, width, height);
                    Destroy(screenshot);
                }

                // バイト配列に変換（JPEGまたはPNG）
                byte[] bytes;
                if (jpegQuality > 0)
                {
                    bytes = scaledScreenshot.EncodeToJPG(jpegQuality);
                }
                else
                {
                    bytes = scaledScreenshot.EncodeToPNG();
                }

                Destroy(scaledScreenshot);

                // バッファに追加（循環バッファ）
                lock (lockObject)
                {
                    if (screenshotBuffer.Count >= bufferSize)
                    {
                        screenshotBuffer.Dequeue();
                    }
                    screenshotBuffer.Enqueue(bytes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Error capturing screenshot: {ex.Message}");
            }
        }

        /// <summary>
        /// テクスチャをリサイズします
        /// </summary>
        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            RenderTexture.active = rt;

            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
