using System;
using System.Collections;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// イベント発火時のスクリーンショットキャプチャを管理するクラス
    /// 一定間隔でRenderTextureにキャプチャし、リングバッファに保持します（軽量）
    /// イベント発火時にのみJPEGエンコードを行い、バイト配列を返します
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

        // RenderTextureリングバッファ（軽量な常時キャプチャ用）
        private RenderTexture[] renderTextureBuffer;
        private float[] captureTimestamps;
        private int currentBufferIndex = 0;
        private int filledCount = 0;
        private Coroutine captureCoroutine;
        private readonly object lockObject = new object();

        // キャプチャ用の一時リソース
        private int captureWidth;
        private int captureHeight;

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
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                StopCapture();
                ReleaseRenderTextures();
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

            InitializeRenderTextures();
            isCapturing = true;
            captureCoroutine = StartCoroutine(CaptureScreenshotsCoroutine());
            Debug.Log($"EventScreenshotCapture: Started capturing to RenderTexture buffer every {captureInterval} seconds.");
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
                currentBufferIndex = 0;
                filledCount = 0;
            }
        }

        /// <summary>
        /// 最新のスクリーンショットをN枚取得します（イベント発火時に呼び出し）
        /// この時点でJPEGエンコードを実行します
        /// </summary>
        /// <param name="count">取得する枚数（デフォルト: 4枚）</param>
        /// <returns>スクリーンショットのバイト配列の配列（古い順）</returns>
        public byte[][] GetRecentScreenshots(int count = 4)
        {
            lock (lockObject)
            {
                int availableCount = Mathf.Min(count, filledCount);
                if (availableCount == 0)
                {
                    Debug.LogWarning("EventScreenshotCapture: No screenshots available in buffer.");
                    return new byte[0][];
                }

                byte[][] result = new byte[availableCount][];

                // 古い順に取得するためのインデックス計算
                int startIndex;
                if (filledCount >= bufferSize)
                {
                    // バッファが一周している場合
                    startIndex = (currentBufferIndex - availableCount + bufferSize) % bufferSize;
                }
                else
                {
                    // まだ一周していない場合
                    startIndex = Mathf.Max(0, filledCount - availableCount);
                }

                // RenderTextureからJPEGバイト配列に変換（イベント時のみ実行）
                for (int i = 0; i < availableCount; i++)
                {
                    int bufferIndex = (startIndex + i) % bufferSize;
                    result[i] = EncodeRenderTextureToBytes(renderTextureBuffer[bufferIndex]);
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
                    return filledCount;
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
            bool needsRestart = isCapturing;

            if (needsRestart)
            {
                StopCapture();
            }

            if (interval > 0)
            {
                captureInterval = interval;
            }

            if (bufferSize > 0)
            {
                this.bufferSize = bufferSize;
            }

            if (scale > 0)
            {
                screenshotScale = Mathf.Clamp(scale, 0.1f, 1.0f);
            }

            if (quality >= 0)
            {
                jpegQuality = Mathf.Clamp(quality, 0, 100);
            }

            // バッファを再初期化
            ReleaseRenderTextures();

            if (needsRestart)
            {
                StartCapture();
            }
        }

        private void InitializeRenderTextures()
        {
            ReleaseRenderTextures();

            captureWidth = Mathf.RoundToInt(Screen.width * screenshotScale);
            captureHeight = Mathf.RoundToInt(Screen.height * screenshotScale);

            renderTextureBuffer = new RenderTexture[bufferSize];
            captureTimestamps = new float[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                renderTextureBuffer[i] = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
                renderTextureBuffer[i].Create();
            }

            currentBufferIndex = 0;
            filledCount = 0;

            Debug.Log($"EventScreenshotCapture: Initialized {bufferSize} RenderTextures at {captureWidth}x{captureHeight}");
        }

        private void ReleaseRenderTextures()
        {
            if (renderTextureBuffer != null)
            {
                foreach (var rt in renderTextureBuffer)
                {
                    if (rt != null)
                    {
                        rt.Release();
                        Destroy(rt);
                    }
                }
                renderTextureBuffer = null;
            }
            captureTimestamps = null;
        }

        private IEnumerator CaptureScreenshotsCoroutine()
        {
            while (isCapturing)
            {
                // フレーム終了時にキャプチャ（WaitForEndOfFrameが必要）
                yield return new WaitForEndOfFrame();
                CaptureToRenderTexture();
                yield return new WaitForSeconds(captureInterval);
            }
        }

        private void CaptureToRenderTexture()
        {
            try
            {
                if (renderTextureBuffer == null || renderTextureBuffer[currentBufferIndex] == null)
                {
                    return;
                }

                // 現在の画面をRenderTextureにキャプチャ（軽量）
                var currentRT = renderTextureBuffer[currentBufferIndex];

                // ScreenCapture.CaptureScreenshotIntoRenderTextureがないため、
                // Texture2Dを経由してBlitする
                Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
                if (screenTexture == null)
                {
                    return;
                }

                // RenderTextureにBlit（スケーリング込み）
                Graphics.Blit(screenTexture, currentRT);
                Destroy(screenTexture);

                lock (lockObject)
                {
                    captureTimestamps[currentBufferIndex] = Time.time;
                    currentBufferIndex = (currentBufferIndex + 1) % bufferSize;
                    filledCount = Mathf.Min(filledCount + 1, bufferSize);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Error capturing to RenderTexture: {ex.Message}");
            }
        }

        /// <summary>
        /// RenderTextureをバイト配列（JPEG/PNG）にエンコードします
        /// イベント発火時のみ呼び出される重い処理
        /// </summary>
        private byte[] EncodeRenderTextureToBytes(RenderTexture rt)
        {
            if (rt == null)
            {
                return new byte[0];
            }

            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            byte[] bytes;
            if (jpegQuality > 0)
            {
                bytes = texture.EncodeToJPG(jpegQuality);
            }
            else
            {
                bytes = texture.EncodeToPNG();
            }

            Destroy(texture);
            return bytes;
        }
    }
}
