using Newtonsoft.Json;

namespace LudiscanApiClient.Runtime.ApiClient.Dto
{
    /// <summary>
    /// セッション作成リクエストDTO
    /// </summary>
    public class CreatePlaySessionDto
    {
        /// <summary>
        /// セッション名
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// デバイスID
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// プラットフォーム
        /// </summary>
        [JsonProperty("platform")]
        public string Platform { get; set; }

        /// <summary>
        /// アプリバージョン
        /// </summary>
        [JsonProperty("appVersion")]
        public string AppVersion { get; set; }

        public CreatePlaySessionDto(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// セッション更新リクエストDTO
    /// </summary>
    public class UpdatePlaySessionDto
    {
        /// <summary>
        /// メタデータ
        /// </summary>
        [JsonProperty("metaData")]
        public object MetaData { get; set; }
    }
}
