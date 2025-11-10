using System;
using Matuyuhi.LudiscanApi.Client.Model;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    public class Project : IProject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public static Project Empty { get; } = new();
        public static Project FromDto(ProjectResponseDto dto)
        {
            return new()
            {
                Id = (int)dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = dto.CreatedAt,
            };
        }
    }
}