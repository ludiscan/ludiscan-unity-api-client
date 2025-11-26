using System;
using Newtonsoft.Json;

namespace LudiscanApiClient.Runtime.ApiClient.Dto
{
    /// <summary>
    /// プレイセッション情報のレスポンスDTO
    /// </summary>
    public class PlaySessionResponseDto
    {
        /// <summary>
        /// セッションID
        /// </summary>
        [JsonProperty("sessionId")]
        public long SessionId { get; set; }

        /// <summary>
        /// プロジェクトID
        /// </summary>
        [JsonProperty("projectId")]
        public long ProjectId { get; set; }

        /// <summary>
        /// セッション名
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// セッション開始時刻
        /// </summary>
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// セッション終了時刻（終了していない場合はnull）
        /// </summary>
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// メタデータ
        /// </summary>
        [JsonProperty("metaData")]
        public object MetaData { get; set; }

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

        /// <summary>
        /// プレイ中かどうか
        /// </summary>
        [JsonProperty("isPlaying")]
        public bool IsPlaying { get; set; }
    }
}
