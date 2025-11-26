using System;
using LudiscanApiClient.Runtime.ApiClient.Dto;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    /// <summary>
    /// Ludiscanプロジェクトの情報を表すクラス
    /// ゲームプロジェクトの基本情報を管理します
    /// </summary>
    public class Project : IProject
    {
        /// <summary>
        /// プロジェクトID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// プロジェクト名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// プロジェクトの説明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// プロジェクトの作成日時
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 空のプロジェクトを取得します
        /// </summary>
        public static Project Empty { get; } = new();

        /// <summary>
        /// DTOからProjectを生成します
        /// </summary>
        /// <param name="dto">ProjectResponseDto</param>
        /// <returns>Project</returns>
        public static Project FromDto(ProjectResponseDto dto)
        {
            return new()
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = dto.CreatedAt,
            };
        }
    }
}
