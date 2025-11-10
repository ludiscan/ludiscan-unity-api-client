using System;
using Matuyuhi.LudiscanApi.Client.Model;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    public struct Session
    {
        public string Name { get; set; }
        public int SessionId { get; set; }
        public int ProjectId { get; set; }
        public DateTime StartedAt { get; set; }
        public object MetaData { get; set; }
        public string DeviceId { get; set; }
        public string Platform { get; set; }
        public string AppVersion { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsPlaying { get; set; }

        public bool IsActive => IsPlaying && EndedAt == null && SessionId > 0;

        public static Session Empty { get; } = new();
        public static Session FromDto(PlaySessionResponseDto dto)
        {
            return new()
            {
                Name = dto.Name,
                SessionId = (int)dto.SessionId,
                ProjectId = (int)dto.ProjectId,
                StartedAt = dto.StartTime,
                MetaData = dto.MetaData,
                DeviceId = dto.DeviceId,
                Platform = dto.Platform,
                AppVersion = dto.AppVersion,
                EndedAt = dto.EndTime,
                IsPlaying = dto.IsPlaying,
            };
        }
    }
}