using System;
using Newtonsoft.Json;

namespace LudiscanApiClient.Runtime.ApiClient.Dto
{
    /// <summary>
    /// プロジェクト情報のレスポンスDTO
    /// </summary>
    public class ProjectResponseDto : IProject
    {
        /// <summary>
        /// プロジェクトID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// プロジェクト名
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// プロジェクトの説明
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// プロジェクト情報のインターフェース
    /// </summary>
    public interface IProject
    {
        /// <summary>
        /// プロジェクトID
        /// </summary>
        int Id { get; }

        /// <summary>
        /// プロジェクト名
        /// </summary>
        string Name { get; }
    }
}
