using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// イベント発火時のスクリーンショットキャプチャを管理するクラス
    /// 複数プレイヤー/カメラに対応し、AsyncGPUReadbackで非同期キャプチャを行います
    /// </summary>
    public class EventScreenshotCapture : MonoBehaviour
    {
        private static EventScreenshotCapture _instance;

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

        public static bool IsInitialized => _instance != null;

        [Header("Capture Settings")]
        [SerializeField] private float captureInterval = 0.5f;
        [SerializeField] private int bufferSize = 4;
        [SerializeField] [Range(0.1f, 1.0f)] private float screenshotScale = 1.0f;
        [SerializeField] [Range(1, 100)] private int jpegQuality = 50;

        [Header("Texture Processing")]
        [SerializeField] private bool flipTextureVertically = true;

        [Header("Runtime Status")]
        [SerializeField] private bool isCapturing = false;

        // プレイヤー別のキャプチャデータ
        private Dictionary<int, PlayerCaptureData> playerCaptures = new Dictionary<int, PlayerCaptureData>();
        private readonly object captureLock = new object();

        // デフォルトキャプチャ（カメラ未登録時のScreenCapture用）
        private PlayerCaptureData defaultCapture;

        private float lastCaptureTime;
        private int captureWidth;
        private int captureHeight;

        /// <summary>
        /// プレイヤー別のキャプチャデータ
        /// </summary>
        private class PlayerCaptureData
        {
            public int PlayerId;
            public Camera Camera;
            public RenderTexture CaptureRT;
            public FrameData[] FrameBuffer;
            public int WriteIndex;
            public int FrameCount;
            public bool PendingCapture;
            public Rect LastViewportRect;  // ビューポート変更を検出

            public struct FrameData
            {
                public byte[] RawPixels;
                public byte[] JpegData;     // Pre-encoded JPEG data
                public float Timestamp;
                public int Width;
                public int Height;
                public bool IsValid;
            }
        }

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
                ReleaseAllResources();
                _instance = null;
            }
        }

        /// <summary>
        /// プレイヤーのカメラを登録します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="camera">キャプチャ対象のカメラ</param>
        public void RegisterCamera(int playerId, Camera camera)
        {
            lock (captureLock)
            {
                if (playerCaptures.ContainsKey(playerId))
                {
                    Debug.LogWarning($"EventScreenshotCapture: Player {playerId} already registered. Updating camera.");
                    ReleasePlayerResources(playerCaptures[playerId]);
                }

                var captureData = CreatePlayerCaptureData(playerId, camera);
                playerCaptures[playerId] = captureData;
                Debug.Log($"EventScreenshotCapture: Registered camera for player {playerId} ('{camera.name}')");
            }
        }

        /// <summary>
        /// プレイヤーのカメラ登録を解除します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void UnregisterCamera(int playerId)
        {
            lock (captureLock)
            {
                if (playerCaptures.TryGetValue(playerId, out var captureData))
                {
                    ReleasePlayerResources(captureData);
                    playerCaptures.Remove(playerId);
                    Debug.Log($"EventScreenshotCapture: Unregistered camera for player {playerId}");
                }
            }
        }

        /// <summary>
        /// 全カメラの登録を解除します
        /// </summary>
        public void UnregisterAllCameras()
        {
            lock (captureLock)
            {
                foreach (var kvp in playerCaptures)
                {
                    ReleasePlayerResources(kvp.Value);
                }
                playerCaptures.Clear();
                Debug.Log("EventScreenshotCapture: Unregistered all cameras");
            }
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

            captureWidth = Mathf.RoundToInt(Screen.width * screenshotScale);
            captureHeight = Mathf.RoundToInt(Screen.height * screenshotScale);

            // デフォルトキャプチャを初期化（カメラ未登録時用）
            defaultCapture = CreatePlayerCaptureData(-1, null);

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
        /// 指定プレイヤーのカメラを今すぐキャプチャします（イベント発火時用）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void CaptureImmediate(int playerId)
        {
            if (!isCapturing) return;

            lock (captureLock)
            {
                PlayerCaptureData captureData = null;

                // 登録済みカメラがあればそれを使用、なければデフォルト
                captureData = playerCaptures.GetValueOrDefault(playerId, defaultCapture);

                if (captureData != null && !captureData.PendingCapture && captureData.CaptureRT != null)
                {
                    // 次のフレーム終了時にキャプチャを実行
                    StartCoroutine(CaptureImmediateAsync(captureData));
                }
            }
        }

        private IEnumerator CaptureImmediateAsync(PlayerCaptureData data)
        {
            yield return new WaitForEndOfFrame();

            lock (captureLock)
            {
                if (isCapturing && data.Camera != null)
                {
                    CapturePlayerCamera(data);
                }
                else if (isCapturing && data.Camera == null)
                {
                    CaptureScreenDefault(data);
                }
            }
        }

        /// <summary>
        /// 指定プレイヤーの最新スクリーンショットを取得します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="count">取得枚数</param>
        /// <returns>JPEGバイト配列の配列</returns>
        public byte[][] GetRecentScreenshots(int playerId, int count = 4)
        {
            lock (captureLock)
            {
                PlayerCaptureData captureData;

                // プレイヤーIDに対応するカメラがあればそれを使用、なければデフォルト
                if (!playerCaptures.TryGetValue(playerId, out captureData))
                {
                    captureData = defaultCapture;
                }

                if (captureData == null || captureData.FrameBuffer == null)
                {
                    Debug.LogWarning($"EventScreenshotCapture: No capture data for player {playerId}");
                    return new byte[0][];
                }

                int availableCount = Mathf.Min(count, captureData.FrameCount);
                if (availableCount == 0)
                {
                    Debug.LogWarning($"EventScreenshotCapture: No frames in buffer for player {playerId}");
                    return new byte[0][];
                }

                byte[][] result = new byte[availableCount][];
                int startIndex = (captureData.WriteIndex - availableCount + bufferSize) % bufferSize;

                for (int i = 0; i < availableCount; i++)
                {
                    int idx = (startIndex + i) % bufferSize;
                    ref var frame = ref captureData.FrameBuffer[idx];

                    if (frame.IsValid)
                    {
                        // Return pre-encoded JPEG if available (fast path)
                        if (frame.JpegData != null && frame.JpegData.Length > 0)
                        {
                            result[i] = frame.JpegData;
                        }
                        else
                        {
                            // Fallback: encode now if pre-encoding failed (rare case)
                            result[i] = EncodeToJpegFallback(frame);
                        }
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
        /// 後方互換性のため、playerId省略時はデフォルトキャプチャを返す
        /// </summary>
        public byte[][] GetRecentScreenshots(int count = 4)
        {
            return GetRecentScreenshots(-1, count);
        }

        /// <summary>
        /// 指定プレイヤーのバッファ数を取得
        /// </summary>
        public int GetBufferedCount(int playerId)
        {
            lock (captureLock)
            {
                if (playerCaptures.TryGetValue(playerId, out var captureData))
                {
                    return captureData.FrameCount;
                }
                return defaultCapture?.FrameCount ?? 0;
            }
        }

        public int BufferedCount => GetBufferedCount(-1);

        /// <summary>
        /// キャプチャ設定を変更します
        /// </summary>
        public void ConfigureCapture(float interval = -1f, int bufferSize = -1, float scale = -1f, int quality = -1, bool? flipVertical = null)
        {
            bool wasCapturing = isCapturing;
            if (wasCapturing) StopCapture();

            if (interval > 0) captureInterval = interval;
            if (bufferSize > 0) this.bufferSize = bufferSize;
            if (scale > 0) screenshotScale = Mathf.Clamp(scale, 0.1f, 1.0f);
            if (quality > 0) jpegQuality = Mathf.Clamp(quality, 1, 100);
            if (flipVertical.HasValue) flipTextureVertically = flipVertical.Value;

            ReleaseAllResources();

            if (wasCapturing) StartCapture();
        }

        public void ClearBuffer()
        {
            lock (captureLock)
            {
                foreach (var kvp in playerCaptures)
                {
                    ClearPlayerBuffer(kvp.Value);
                }
                if (defaultCapture != null)
                {
                    ClearPlayerBuffer(defaultCapture);
                }
            }
        }

        private void ClearPlayerBuffer(PlayerCaptureData data)
        {
            data.WriteIndex = 0;
            data.FrameCount = 0;
            if (data.FrameBuffer != null)
            {
                for (int i = 0; i < data.FrameBuffer.Length; i++)
                {
                    data.FrameBuffer[i].IsValid = false;
                    data.FrameBuffer[i].JpegData = null;
                }
            }
        }

        private PlayerCaptureData CreatePlayerCaptureData(int playerId, Camera camera)
        {
            // カメラのビューポートを考慮したキャプチャ解像度を計算
            int effectiveWidth = captureWidth;
            int effectiveHeight = captureHeight;
            Rect viewportRect = new Rect(0, 0, 1, 1);  // デフォルト：フルスクリーン

            if (camera != null)
            {
                viewportRect = camera.rect;
                // ビューポートのサイズに応じてキャプチャ解像度を調整
                effectiveWidth = Mathf.Max(64, Mathf.RoundToInt(Screen.width * screenshotScale * viewportRect.width));
                effectiveHeight = Mathf.Max(64, Mathf.RoundToInt(Screen.height * screenshotScale * viewportRect.height));

                // Debug.Log($"EventScreenshotCapture: Creating capture data for player {playerId} " +
                //           $"(Camera: '{camera.name}', Viewport: {viewportRect}, " +
                //           $"Resolution: {effectiveWidth}x{effectiveHeight})");
            }

            var data = new PlayerCaptureData
            {
                PlayerId = playerId,
                Camera = camera,
                CaptureRT = new RenderTexture(effectiveWidth, effectiveHeight, 24, RenderTextureFormat.ARGB32),
                FrameBuffer = new PlayerCaptureData.FrameData[bufferSize],
                WriteIndex = 0,
                FrameCount = 0,
                PendingCapture = false,
                LastViewportRect = viewportRect
            };

            data.CaptureRT.Create();

            // RenderTexture 作成後の検証ログ
            // Debug.Log($"EventScreenshotCapture: RenderTexture created for player {playerId} " +
            //           $"({data.CaptureRT.width}x{data.CaptureRT.height}, format: {data.CaptureRT.format})");

            for (int i = 0; i < bufferSize; i++)
            {
                data.FrameBuffer[i] = new PlayerCaptureData.FrameData
                {
                    RawPixels = new byte[effectiveWidth * effectiveHeight * 3],
                    JpegData = null,
                    Width = effectiveWidth,
                    Height = effectiveHeight,
                    IsValid = false
                };
            }

            return data;
        }

        private void ReleasePlayerResources(PlayerCaptureData data)
        {
            if (data?.CaptureRT != null)
            {
                data.CaptureRT.Release();
                Destroy(data.CaptureRT);
                data.CaptureRT = null;
            }
            data.FrameBuffer = null;
        }

        private void ReleaseAllResources()
        {
            lock (captureLock)
            {
                foreach (var kvp in playerCaptures)
                {
                    ReleasePlayerResources(kvp.Value);
                }
                playerCaptures.Clear();

                if (defaultCapture != null)
                {
                    ReleasePlayerResources(defaultCapture);
                    defaultCapture = null;
                }
            }
        }

        private void LateUpdate()
        {
            if (!isCapturing) return;
            if (Time.time - lastCaptureTime < captureInterval) return;

            lastCaptureTime = Time.time;
            StartCoroutine(CaptureAllPlayersAsync());
        }

        private IEnumerator CaptureAllPlayersAsync()
        {
            yield return new WaitForEndOfFrame();

            if (!isCapturing) yield break;

            lock (captureLock)
            {
                // 登録済みカメラをキャプチャ
                foreach (var kvp in playerCaptures)
                {
                    var data = kvp.Value;
                    if (!data.PendingCapture && data.Camera != null && data.CaptureRT != null)
                    {
                        CapturePlayerCamera(data);
                    }
                }

                // デフォルトキャプチャ（ScreenCapture）
                if (defaultCapture != null && !defaultCapture.PendingCapture && defaultCapture.CaptureRT != null)
                {
                    CaptureScreenDefault(defaultCapture);
                }
            }
        }

        private void CapturePlayerCamera(PlayerCaptureData data)
        {
            try
            {
                data.PendingCapture = true;

                // ビューポート変更を検出して、RenderTextureを再作成
                if (data.Camera.rect != data.LastViewportRect)
                {
                    // Debug.Log($"EventScreenshotCapture: Viewport changed for player {data.PlayerId}. Recreating RenderTexture.");
                    RecreateRenderTextureForCamera(data);
                }

                var prevRT = data.Camera.targetTexture;
                var prevViewport = data.Camera.rect;

                // デバッグ情報
                // Debug.Log($"EventScreenshotCapture [Player {data.PlayerId}] Capture start:" +
                //           $"\n  Camera: '{data.Camera.name}'" +
                //           $"\n  Original Viewport: {prevViewport.x:F3},{prevViewport.y:F3} {prevViewport.width:F3}x{prevViewport.height:F3}" +
                //           $"\n  Screen Size: {Screen.width}x{Screen.height}" +
                //           $"\n  RenderTexture: {data.CaptureRT.width}x{data.CaptureRT.height}" +
                //           $"\n  Expected Data Size: {data.CaptureRT.width * data.CaptureRT.height * 3} bytes (RGB24)");

                // RenderTexture にレンダリング時は全ビューポートを使用
                data.Camera.targetTexture = data.CaptureRT;
                data.Camera.rect = new Rect(0, 0, 1, 1);
                data.Camera.Render();

                // 復元
                data.Camera.targetTexture = prevRT;
                data.Camera.rect = prevViewport;

                AsyncGPUReadback.Request(data.CaptureRT, 0, TextureFormat.RGB24, (request) =>
                {
                    OnGPUReadbackComplete(request, data);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Capture failed for player {data.PlayerId}: {ex.Message}");
                data.PendingCapture = false;
            }
        }

        /// <summary>
        /// ビューポート変更時にRenderTextureを再作成します
        /// </summary>
        private void RecreateRenderTextureForCamera(PlayerCaptureData data)
        {
            // 古いRenderTextureを解放
            int oldWidth = data.CaptureRT?.width ?? 0;
            int oldHeight = data.CaptureRT?.height ?? 0;

            if (data.CaptureRT != null)
            {
                data.CaptureRT.Release();
                Destroy(data.CaptureRT);
            }

            // ビューポート情報を更新
            data.LastViewportRect = data.Camera.rect;
            Rect viewportRect = data.Camera.rect;

            // 新しいキャプチャ解像度を計算
            int newWidth = Mathf.Max(64, Mathf.RoundToInt(Screen.width * screenshotScale * viewportRect.width));
            int newHeight = Mathf.Max(64, Mathf.RoundToInt(Screen.height * screenshotScale * viewportRect.height));

            // 新しいRenderTextureを作成
            data.CaptureRT = new RenderTexture(newWidth, newHeight, 24, RenderTextureFormat.ARGB32);
            data.CaptureRT.Create();

            // フレームバッファを再作成
            for (int i = 0; i < bufferSize; i++)
            {
                data.FrameBuffer[i] = new PlayerCaptureData.FrameData
                {
                    RawPixels = new byte[newWidth * newHeight * 3],
                    JpegData = null,
                    Width = newWidth,
                    Height = newHeight,
                    IsValid = false
                };
            }

            // Debug.Log($"EventScreenshotCapture: RenderTexture recreated for player {data.PlayerId} " +
            //           $"({oldWidth}x{oldHeight} → {newWidth}x{newHeight}, " +
            //           $"viewport: {viewportRect.x:F2},{viewportRect.y:F2} {viewportRect.width:F2}x{viewportRect.height:F2})");
        }

        private void CaptureScreenDefault(PlayerCaptureData data)
        {
            try
            {
                data.PendingCapture = true;

                ScreenCapture.CaptureScreenshotIntoRenderTexture(data.CaptureRT);

                AsyncGPUReadback.Request(data.CaptureRT, 0, TextureFormat.RGB24, (request) =>
                {
                    OnGPUReadbackComplete(request, data);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Default capture failed: {ex.Message}");
                data.PendingCapture = false;
            }
        }

        private void OnGPUReadbackComplete(AsyncGPUReadbackRequest request, PlayerCaptureData data)
        {
            data.PendingCapture = false;

            if (request.hasError)
            {
                Debug.LogWarning($"EventScreenshotCapture: GPU readback error for player {data.PlayerId}");
                return;
            }

            if (!isCapturing || data.FrameBuffer == null) return;

            try
            {
                var rawData = request.GetData<byte>();

                lock (captureLock)
                {
                    var frame = data.FrameBuffer[data.WriteIndex];
                    if (frame.RawPixels == null || frame.RawPixels.Length != rawData.Length)
                    {
                        frame.RawPixels = new byte[rawData.Length];
                    }

                    NativeArray<byte>.Copy(rawData, frame.RawPixels, rawData.Length);

                    // RenderTexture の実際のサイズを使用（ビューポート変更対応）
                    frame.Width = data.CaptureRT.width;
                    frame.Height = data.CaptureRT.height;
                    frame.Timestamp = Time.time;
                    frame.IsValid = true;
                    frame.JpegData = null;  // Clear old JPEG data

                    // データサイズの整合性をチェック
                    int expectedSize = frame.Width * frame.Height * 3;  // RGB24 = 3 bytes per pixel

                    // Debug.Log($"EventScreenshotCapture [Player {data.PlayerId}] GPU Readback complete:" +
                    //           $"\n  Frame Size: {frame.Width}x{frame.Height}" +
                    //           $"\n  Expected Data: {expectedSize} bytes" +
                    //           $"\n  Actual Data: {rawData.Length} bytes" +
                    //           $"\n  Match: {(rawData.Length == expectedSize ? "OK" : "MISMATCH")}");

                    if (rawData.Length != expectedSize)
                    {
                        Debug.LogWarning(
                            $"EventScreenshotCapture: Data size mismatch for player {data.PlayerId}. " +
                            $"Expected {expectedSize} bytes ({frame.Width}x{frame.Height}), " +
                            $"but got {rawData.Length} bytes.");
                    }

                    data.FrameBuffer[data.WriteIndex] = frame;

                    // Pre-encode to JPEG immediately
                    // This spreads the CPU cost over time instead of at collision
                    PreEncodeJpeg(ref data.FrameBuffer[data.WriteIndex]);

                    data.WriteIndex = (data.WriteIndex + 1) % bufferSize;
                    data.FrameCount = Mathf.Min(data.FrameCount + 1, bufferSize);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Readback processing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Pre-encodes raw pixel data to JPEG and stores it in the frame.
        /// Called immediately after GPU readback completes.
        /// </summary>
        /// <param name="frame">Reference to the frame data to encode</param>
        /// <returns>True if encoding succeeded</returns>
        private bool PreEncodeJpeg(ref PlayerCaptureData.FrameData frame)
        {
            if (!frame.IsValid || frame.RawPixels == null) return false;

            try
            {
                int expectedSize = frame.Width * frame.Height * 3;
                if (frame.RawPixels.Length != expectedSize)
                {
                    Debug.LogWarning($"EventScreenshotCapture: PreEncode skipped due to size mismatch");
                    return false;
                }

                // Step 1: Flip raw bytes vertically (in-place, minimal allocation)
                if (flipTextureVertically)
                {
                    FlipRawBytesVertically(frame.RawPixels, frame.Width, frame.Height);
                }

                // Step 2: Create Texture2D and encode to JPEG
                var tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGB24, false);
                tex.LoadRawTextureData(frame.RawPixels);
                tex.Apply();  // Only 1 GPU sync needed (no flip via Texture2D)

                frame.JpegData = tex.EncodeToJPG(jpegQuality);

                Destroy(tex);

                // Optional: Clear raw pixels to save memory (commented out to allow fallback)
                // frame.RawPixels = null;

                return frame.JpegData != null && frame.JpegData.Length > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: PreEncode failed: {ex.Message}");
                frame.JpegData = null;
                return false;
            }
        }

        /// <summary>
        /// Fallback JPEG encoding for frames that weren't pre-encoded.
        /// This should rarely be called in normal operation.
        /// </summary>
        private byte[] EncodeToJpegFallback(PlayerCaptureData.FrameData frame)
        {
            Debug.LogWarning("EventScreenshotCapture: Using fallback JPEG encoding (pre-encoding may have failed)");

            if (!frame.IsValid || frame.RawPixels == null) return new byte[0];

            try
            {
                // データサイズの検証
                int expectedSize = frame.Width * frame.Height * 3;  // RGB24 = 3 bytes per pixel
                if (frame.RawPixels.Length != expectedSize)
                {
                    Debug.LogError(
                        $"EventScreenshotCapture: Cannot encode JPEG due to data size mismatch. " +
                        $"Expected {expectedSize} bytes ({frame.Width}x{frame.Height}), " +
                        $"but RawPixels has {frame.RawPixels.Length} bytes. Skipping this frame.");
                    return new byte[0];
                }

                var tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGB24, false);
                tex.LoadRawTextureData(frame.RawPixels);
                tex.Apply();

                if (flipTextureVertically)
                {
                    FlipTextureVertically(tex);
                }

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
        /// Flips raw RGB24 pixel data vertically in-place.
        /// This is much faster than using Texture2D.GetPixels/SetPixels.
        /// </summary>
        /// <param name="rawPixels">RGB24 byte array (width * height * 3 bytes)</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        private void FlipRawBytesVertically(byte[] rawPixels, int width, int height)
        {
            int bytesPerRow = width * 3;  // RGB24 = 3 bytes per pixel
            byte[] tempRow = new byte[bytesPerRow];  // Single allocation for swap buffer

            int halfHeight = height / 2;
            for (int y = 0; y < halfHeight; y++)
            {
                int topRowStart = y * bytesPerRow;
                int bottomRowStart = (height - 1 - y) * bytesPerRow;

                // Swap rows using Buffer.BlockCopy (optimized memory copy)
                Buffer.BlockCopy(rawPixels, topRowStart, tempRow, 0, bytesPerRow);
                Buffer.BlockCopy(rawPixels, bottomRowStart, rawPixels, topRowStart, bytesPerRow);
                Buffer.BlockCopy(tempRow, 0, rawPixels, bottomRowStart, bytesPerRow);
            }
        }

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
