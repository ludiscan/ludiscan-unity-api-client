using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// イベント発火時のスクリーンショットキャプチャを管理するクラス
    /// AsyncGPUReadbackを使用してメインスレッドをブロックせずにキャプチャします
    /// death/successなどの重要イベント発火時に、直前の数秒間のスクリーンショットを提供します
    /// </summary>
    public class EventScreenshotCapture : MonoBehaviour
    {
        private static EventScreenshotCapture _instance;

        /// <summary>
        /// EventScreenshotCaptureのシングルトンインスタンスを取得します
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

        [Tooltip("JPEG品質（1-100、低いほどファイルサイズが小さい）")]
        [SerializeField]
        [Range(1, 100)]
        private int jpegQuality = 50;

        [Header("Runtime Status")]
        [SerializeField]
        private bool isCapturing = false;

        // リングバッファ（軽量なbyte[]で保持、イベント時のみJPEGエンコード不要に）
        private FrameData[] frameBuffer;
        private int writeIndex = 0;
        private int frameCount = 0;
        private readonly object bufferLock = new object();

        // キャプチャ用リソース
        private RenderTexture captureRT;
        private int captureWidth;
        private int captureHeight;
        private float lastCaptureTime;
        private bool pendingCapture = false;

        // 登録されたカメラ（nullの場合はScreenCapture使用）
        private Camera registeredCamera;

        private struct FrameData
        {
            public byte[] RawPixels;  // RGB24 raw pixels
            public float Timestamp;
            public int Width;
            public int Height;
            public bool IsValid;
        }

        /// <summary>
        /// EventScreenshotCaptureを初期化します
        /// </summary>
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
                ReleaseResources();
                _instance = null;
            }
        }

        /// <summary>
        /// キャプチャ対象のカメラを登録します
        /// 登録しない場合はScreenCaptureを使用します（やや重い）
        /// </summary>
        /// <param name="camera">キャプチャ対象のカメラ</param>
        public void RegisterCamera(Camera camera)
        {
            registeredCamera = camera;
            Debug.Log($"EventScreenshotCapture: Registered camera '{camera.name}'");
        }

        /// <summary>
        /// カメラの登録を解除します
        /// </summary>
        public void UnregisterCamera()
        {
            registeredCamera = null;
            Debug.Log("EventScreenshotCapture: Unregistered camera");
        }

        /// <summary>
        /// スクリーンショットのキャプチャを開始します
        /// </summary>
        public void StartCapture(float interval = -1f)
        {
            if (isCapturing)
            {
                Debug.LogWarning("EventScreenshotCapture: Already capturing.");
                return;
            }

            if (interval > 0)
            {
                captureInterval = interval;
            }

            InitializeResources();
            isCapturing = true;
            lastCaptureTime = Time.time;
            Debug.Log($"EventScreenshotCapture: Started (interval={captureInterval}s, scale={screenshotScale}, buffer={bufferSize})");
        }

        /// <summary>
        /// スクリーンショットのキャプチャを停止します
        /// </summary>
        public void StopCapture()
        {
            if (!isCapturing) return;

            isCapturing = false;
            Debug.Log("EventScreenshotCapture: Stopped");
        }

        /// <summary>
        /// バッファをクリアします
        /// </summary>
        public void ClearBuffer()
        {
            lock (bufferLock)
            {
                writeIndex = 0;
                frameCount = 0;
                if (frameBuffer != null)
                {
                    for (int i = 0; i < frameBuffer.Length; i++)
                    {
                        frameBuffer[i].IsValid = false;
                    }
                }
            }
        }

        /// <summary>
        /// 最新のスクリーンショットをN枚取得します（イベント発火時に呼び出し）
        /// この時点でJPEGエンコードを実行します
        /// </summary>
        public byte[][] GetRecentScreenshots(int count = 4)
        {
            lock (bufferLock)
            {
                int availableCount = Mathf.Min(count, frameCount);
                if (availableCount == 0)
                {
                    Debug.LogWarning("EventScreenshotCapture: No frames in buffer.");
                    return new byte[0][];
                }

                byte[][] result = new byte[availableCount][];

                // 古い順に取得
                int startIndex = (writeIndex - availableCount + bufferSize) % bufferSize;

                for (int i = 0; i < availableCount; i++)
                {
                    int idx = (startIndex + i) % bufferSize;
                    if (frameBuffer[idx].IsValid)
                    {
                        result[i] = EncodeToJpeg(frameBuffer[idx]);
                    }
                    else
                    {
                        result[i] = new byte[0];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// 現在のバッファに保持されているフレーム数を取得します
        /// </summary>
        public int BufferedCount
        {
            get
            {
                lock (bufferLock)
                {
                    return frameCount;
                }
            }
        }

        /// <summary>
        /// キャプチャ設定を変更します
        /// </summary>
        public void ConfigureCapture(float interval = -1f, int bufferSize = -1, float scale = -1f, int quality = -1)
        {
            bool wasCapturing = isCapturing;
            if (wasCapturing) StopCapture();

            if (interval > 0) captureInterval = interval;
            if (bufferSize > 0) this.bufferSize = bufferSize;
            if (scale > 0) screenshotScale = Mathf.Clamp(scale, 0.1f, 1.0f);
            if (quality > 0) jpegQuality = Mathf.Clamp(quality, 1, 100);

            ReleaseResources();

            if (wasCapturing) StartCapture();
        }

        private void InitializeResources()
        {
            captureWidth = Mathf.RoundToInt(Screen.width * screenshotScale);
            captureHeight = Mathf.RoundToInt(Screen.height * screenshotScale);

            // RenderTexture作成
            captureRT = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
            captureRT.Create();

            // フレームバッファ初期化
            frameBuffer = new FrameData[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                frameBuffer[i] = new FrameData
                {
                    RawPixels = new byte[captureWidth * captureHeight * 3], // RGB24
                    Width = captureWidth,
                    Height = captureHeight,
                    IsValid = false
                };
            }

            writeIndex = 0;
            frameCount = 0;

            Debug.Log($"EventScreenshotCapture: Initialized {bufferSize} frame buffers at {captureWidth}x{captureHeight}");
        }

        private void ReleaseResources()
        {
            if (captureRT != null)
            {
                captureRT.Release();
                Destroy(captureRT);
                captureRT = null;
            }
            frameBuffer = null;
        }

        private void LateUpdate()
        {
            if (!isCapturing || pendingCapture) return;

            // インターバルチェック
            if (Time.time - lastCaptureTime < captureInterval) return;

            lastCaptureTime = Time.time;
            StartCoroutine(CaptureFrameAsync());
        }

        private IEnumerator CaptureFrameAsync()
        {
            pendingCapture = true;

            // フレーム終了を待つ
            yield return new WaitForEndOfFrame();

            if (!isCapturing || captureRT == null)
            {
                pendingCapture = false;
                yield break;
            }

            try
            {
                // カメラが登録されている場合はカメラからレンダリング
                if (registeredCamera != null)
                {
                    var prevRT = registeredCamera.targetTexture;
                    registeredCamera.targetTexture = captureRT;
                    registeredCamera.Render();
                    registeredCamera.targetTexture = prevRT;
                }
                else
                {
                    // ScreenCapture経由（やや重いが汎用的）
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(captureRT);
                }

                // AsyncGPUReadbackでGPU→CPU転送（非同期、メインスレッドブロックなし）
                AsyncGPUReadback.Request(captureRT, 0, TextureFormat.RGB24, OnGPUReadbackComplete);
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Capture failed: {ex.Message}");
                pendingCapture = false;
            }
        }

        private void OnGPUReadbackComplete(AsyncGPUReadbackRequest request)
        {
            pendingCapture = false;

            if (request.hasError)
            {
                Debug.LogWarning("EventScreenshotCapture: GPU readback error");
                return;
            }

            if (!isCapturing || frameBuffer == null)
            {
                return;
            }

            try
            {
                // NativeArrayからbyte[]にコピー
                var data = request.GetData<byte>();

                lock (bufferLock)
                {
                    if (frameBuffer[writeIndex].RawPixels == null ||
                        frameBuffer[writeIndex].RawPixels.Length != data.Length)
                    {
                        frameBuffer[writeIndex].RawPixels = new byte[data.Length];
                    }

                    NativeArray<byte>.Copy(data, frameBuffer[writeIndex].RawPixels, data.Length);
                    frameBuffer[writeIndex].Width = captureWidth;
                    frameBuffer[writeIndex].Height = captureHeight;
                    frameBuffer[writeIndex].Timestamp = Time.time;
                    frameBuffer[writeIndex].IsValid = true;

                    writeIndex = (writeIndex + 1) % bufferSize;
                    frameCount = Mathf.Min(frameCount + 1, bufferSize);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Readback processing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// RawピクセルデータをJPEGにエンコード（イベント時のみ実行）
        /// </summary>
        private byte[] EncodeToJpeg(FrameData frame)
        {
            if (!frame.IsValid || frame.RawPixels == null)
            {
                return new byte[0];
            }

            try
            {
                // Texture2D作成してエンコード
                var tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGB24, false);
                tex.LoadRawTextureData(frame.RawPixels);
                tex.Apply();

                // 上下反転（GPUから読み取ったデータは上下逆）
                FlipTextureVertically(tex);

                byte[] jpeg = tex.EncodeToJPG(jpegQuality);
                Destroy(tex);

                return jpeg;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: JPEG encode failed: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// テクスチャを上下反転します
        /// </summary>
        private void FlipTextureVertically(Texture2D tex)
        {
            var pixels = tex.GetPixels();
            var flipped = new Color[pixels.Length];
            int width = tex.width;
            int height = tex.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flipped[y * width + x] = pixels[(height - 1 - y) * width + x];
                }
            }

            tex.SetPixels(flipped);
            tex.Apply();
        }
    }
}
