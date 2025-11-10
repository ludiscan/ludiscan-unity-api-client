namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// LudiscanClient用の設定クラス
    /// </summary>
    public class LudiscanClientConfig
    {
        /// <summary>
        /// API ベースURL
        /// </summary>
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// APIアクセストークン
        /// </summary>
        public string XapiKey { get; set; }

        /// <summary>
        /// タイムアウト秒数（デフォルト: 3秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 3;

        public LudiscanClientConfig(string apiBaseUrl, string _xapiKey)
        {
            ApiBaseUrl = apiBaseUrl;
            XapiKey = _xapiKey;
        }
    }
}
