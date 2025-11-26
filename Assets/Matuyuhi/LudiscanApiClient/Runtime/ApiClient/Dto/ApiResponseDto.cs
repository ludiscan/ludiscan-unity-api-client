using Newtonsoft.Json;

namespace LudiscanApiClient.Runtime.ApiClient.Dto
{
    /// <summary>
    /// デフォルトのエラーレスポンスDTO
    /// </summary>
    public class DefaultErrorResponse
    {
        /// <summary>
        /// HTTPステータスコード
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>
        /// エラー識別子
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// アップロード結果のレスポンスDTO
    /// </summary>
    public class UploadResultDto
    {
        /// <summary>
        /// 成功したかどうか
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// メッセージ
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// ヒートマップ埋め込みURLのレスポンスDTO
    /// </summary>
    public class HeatmapEmbedUrlDto
    {
        /// <summary>
        /// 埋め込みURL
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
