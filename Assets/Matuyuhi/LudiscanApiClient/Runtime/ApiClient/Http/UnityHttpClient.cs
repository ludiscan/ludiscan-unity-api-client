using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace LudiscanApiClient.Runtime.ApiClient.Http
{
    /// <summary>
    /// UnityWebRequestベースのHTTPクライアント
    /// RESTful APIとの通信を行います
    /// </summary>
    public class UnityHttpClient
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly int _timeoutSeconds;
        private readonly bool _skipCertificateValidation;
        private readonly string _userAgent;

        /// <summary>
        /// UnityHttpClientを初期化します
        /// </summary>
        /// <param name="baseUrl">API のベースURL</param>
        /// <param name="apiKey">APIキー</param>
        /// <param name="timeoutSeconds">タイムアウト秒数</param>
        /// <param name="skipCertificateValidation">証明書検証をスキップするか</param>
        public UnityHttpClient(string baseUrl, string apiKey, int timeoutSeconds = 10, bool skipCertificateValidation = false)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _timeoutSeconds = timeoutSeconds;
            _skipCertificateValidation = skipCertificateValidation;
            _userAgent = $"Matuyuhi.LudiscanApi.UnityClient/1.0.0 (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})";
        }

        /// <summary>
        /// GETリクエストを送信します
        /// </summary>
        /// <typeparam name="T">レスポンスの型</typeparam>
        /// <param name="endpoint">エンドポイント</param>
        /// <param name="queryParams">クエリパラメータ</param>
        /// <returns>デシリアライズされたレスポンス</returns>
        public async Task<HttpResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null)
        {
            var url = BuildUrl(endpoint, queryParams);
            using var request = UnityWebRequest.Get(url);
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// GETリクエストを送信します（文字列レスポンス用）
        /// </summary>
        /// <param name="endpoint">エンドポイント</param>
        /// <returns>文字列レスポンス</returns>
        public async Task<HttpResponse<string>> GetStringAsync(string endpoint)
        {
            var url = BuildUrl(endpoint, null);
            using var request = UnityWebRequest.Get(url);
            return await SendRequestStringAsync(request);
        }

        /// <summary>
        /// POSTリクエストを送信します（JSON）
        /// </summary>
        /// <typeparam name="T">レスポンスの型</typeparam>
        /// <param name="endpoint">エンドポイント</param>
        /// <param name="body">リクエストボディ</param>
        /// <returns>デシリアライズされたレスポンス</returns>
        public async Task<HttpResponse<T>> PostJsonAsync<T>(string endpoint, object body)
        {
            var url = BuildUrl(endpoint, null);
            var json = JsonConvert.SerializeObject(body);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// POSTリクエストを送信します（バイナリ）
        /// </summary>
        /// <typeparam name="T">レスポンスの型</typeparam>
        /// <param name="endpoint">エンドポイント</param>
        /// <param name="data">バイナリデータ</param>
        /// <param name="contentType">Content-Type</param>
        /// <returns>デシリアライズされたレスポンス</returns>
        public async Task<HttpResponse<T>> PostBinaryAsync<T>(string endpoint, byte[] data, string contentType = "application/octet-stream")
        {
            var url = BuildUrl(endpoint, null);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", contentType);

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// PUTリクエストを送信します（JSON）
        /// </summary>
        /// <typeparam name="T">レスポンスの型</typeparam>
        /// <param name="endpoint">エンドポイント</param>
        /// <param name="body">リクエストボディ（nullの場合は空ボディ）</param>
        /// <returns>デシリアライズされたレスポンス</returns>
        public async Task<HttpResponse<T>> PutJsonAsync<T>(string endpoint, object body = null)
        {
            var url = BuildUrl(endpoint, null);
            var json = body != null ? JsonConvert.SerializeObject(body) : "{}";
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// リクエストを送信してレスポンスを取得します
        /// </summary>
        private async Task<HttpResponse<T>> SendRequestAsync<T>(UnityWebRequest request)
        {
            ConfigureRequest(request);

            await request.SendWebRequest();

            var statusCode = (int)request.responseCode;
            var responseText = request.downloadHandler?.text ?? string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                // エラーレスポンスの場合、APIエラーとして処理
                return new HttpResponse<T>
                {
                    StatusCode = statusCode,
                    IsSuccess = false,
                    ErrorMessage = request.error,
                    RawContent = responseText
                };
            }

            try
            {
                var data = JsonConvert.DeserializeObject<T>(responseText);
                return new HttpResponse<T>
                {
                    StatusCode = statusCode,
                    IsSuccess = true,
                    Data = data,
                    RawContent = responseText
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse<T>
                {
                    StatusCode = statusCode,
                    IsSuccess = false,
                    ErrorMessage = $"JSON parse error: {ex.Message}",
                    RawContent = responseText
                };
            }
        }

        /// <summary>
        /// リクエストを送信して文字列レスポンスを取得します
        /// </summary>
        private async Task<HttpResponse<string>> SendRequestStringAsync(UnityWebRequest request)
        {
            ConfigureRequest(request);

            await request.SendWebRequest();

            var statusCode = (int)request.responseCode;
            var responseText = request.downloadHandler?.text ?? string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                return new HttpResponse<string>
                {
                    StatusCode = statusCode,
                    IsSuccess = false,
                    ErrorMessage = request.error,
                    RawContent = responseText
                };
            }

            return new HttpResponse<string>
            {
                StatusCode = statusCode,
                IsSuccess = true,
                Data = responseText,
                RawContent = responseText
            };
        }

        /// <summary>
        /// リクエストの共通設定を行います
        /// </summary>
        private void ConfigureRequest(UnityWebRequest request)
        {
            request.timeout = _timeoutSeconds;
            request.SetRequestHeader("Accept", "*/*");
            request.SetRequestHeader("User-Agent", _userAgent);

            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.SetRequestHeader("x-api-key", _apiKey);
            }

            if (_skipCertificateValidation)
            {
                request.certificateHandler = new BypassCertificateHandler();
            }
        }

        /// <summary>
        /// URLを構築します
        /// </summary>
        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams)
        {
            var url = $"{_baseUrl}{endpoint}";

            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = new StringBuilder("?");
                var first = true;
                foreach (var kvp in queryParams)
                {
                    if (!first) queryString.Append("&");
                    queryString.Append(UnityWebRequest.EscapeURL(kvp.Key));
                    queryString.Append("=");
                    queryString.Append(UnityWebRequest.EscapeURL(kvp.Value));
                    first = false;
                }
                url += queryString.ToString();
            }

            return url;
        }
    }

    /// <summary>
    /// HTTPレスポンスを表すクラス
    /// </summary>
    /// <typeparam name="T">レスポンスデータの型</typeparam>
    public class HttpResponse<T>
    {
        /// <summary>
        /// HTTPステータスコード
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// リクエストが成功したかどうか
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// デシリアライズされたレスポンスデータ
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 生のレスポンス内容
        /// </summary>
        public string RawContent { get; set; }
    }

    /// <summary>
    /// UnityWebRequestAsyncOperationのawait対応拡張
    /// </summary>
    public static class UnityWebRequestAsyncOperationExtensions
    {
        public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation operation)
        {
            var tcs = new TaskCompletionSource<bool>();
            operation.completed += _ => tcs.SetResult(true);
            return ((Task)tcs.Task).GetAwaiter();
        }
    }
}
