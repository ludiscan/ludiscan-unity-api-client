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
        [SerializeField] [Range(0.1f, 1.0f)] private float screenshotScale = 0.25f;
        [SerializeField] [Range(1, 100)] private int jpegQuality = 50;

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

            public struct FrameData
            {
                public byte[] RawPixels;
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
                    if (captureData.FrameBuffer[idx].IsValid)
                    {
                        result[i] = EncodeToJpeg(captureData.FrameBuffer[idx]);
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
        public void ConfigureCapture(float interval = -1f, int bufferSize = -1, float scale = -1f, int quality = -1)
        {
            bool wasCapturing = isCapturing;
            if (wasCapturing) StopCapture();

            if (interval > 0) captureInterval = interval;
            if (bufferSize > 0) this.bufferSize = bufferSize;
            if (scale > 0) screenshotScale = Mathf.Clamp(scale, 0.1f, 1.0f);
            if (quality > 0) jpegQuality = Mathf.Clamp(quality, 1, 100);

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
                }
            }
        }

        private PlayerCaptureData CreatePlayerCaptureData(int playerId, Camera camera)
        {
            var data = new PlayerCaptureData
            {
                PlayerId = playerId,
                Camera = camera,
                CaptureRT = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32),
                FrameBuffer = new PlayerCaptureData.FrameData[bufferSize],
                WriteIndex = 0,
                FrameCount = 0,
                PendingCapture = false
            };

            data.CaptureRT.Create();

            for (int i = 0; i < bufferSize; i++)
            {
                data.FrameBuffer[i] = new PlayerCaptureData.FrameData
                {
                    RawPixels = new byte[captureWidth * captureHeight * 3],
                    Width = captureWidth,
                    Height = captureHeight,
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

                var prevRT = data.Camera.targetTexture;
                data.Camera.targetTexture = data.CaptureRT;
                data.Camera.Render();
                data.Camera.targetTexture = prevRT;

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
                    frame.Width = captureWidth;
                    frame.Height = captureHeight;
                    frame.Timestamp = Time.time;
                    frame.IsValid = true;

                    data.FrameBuffer[data.WriteIndex] = frame;
                    data.WriteIndex = (data.WriteIndex + 1) % bufferSize;
                    data.FrameCount = Mathf.Min(data.FrameCount + 1, bufferSize);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventScreenshotCapture: Readback processing failed: {ex.Message}");
            }
        }

        private byte[] EncodeToJpeg(PlayerCaptureData.FrameData frame)
        {
            if (!frame.IsValid || frame.RawPixels == null) return new byte[0];

            try
            {
                var tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGB24, false);
                tex.LoadRawTextureData(frame.RawPixels);
                tex.Apply();

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
