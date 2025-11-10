using System;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// プロジェクト情報を表すインターフェース
    /// </summary>
    public interface IProject
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public DateTime CreatedAt { get; }
    }
}
