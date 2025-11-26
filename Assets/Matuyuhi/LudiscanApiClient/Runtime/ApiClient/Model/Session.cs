using System;
using LudiscanApiClient.Runtime.ApiClient.Dto;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    /// <summary>
    /// プレイセッションの情報を表す構造体
    /// ゲームのプレイセッション（開始から終了まで）の情報を管理します
    /// </summary>
    public struct Session
    {
        /// <summary>
        /// セッション名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// セッションID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// プロジェクトID
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// セッション開始日時
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// メタデータ（マップ名、スコアなど）
        /// </summary>
        public object MetaData { get; set; }

        /// <summary>
        /// デバイスID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// プラットフォーム（例: "Unity-Windows", "Unity-Android"）
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// アプリケーションバージョン
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// セッション終了日時（終了していない場合はnull）
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// プレイ中かどうか
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// セッションがアクティブ（有効）かどうか
        /// プレイ中で、終了しておらず、有効なセッションIDを持つ場合にtrue
        /// </summary>
        public bool IsActive => IsPlaying && EndedAt == null && SessionId > 0;

        /// <summary>
        /// 空のセッションを取得します
        /// </summary>
        public static Session Empty { get; } = new();

        /// <summary>
        /// DTOからSessionを生成します
        /// </summary>
        /// <param name="dto">PlaySessionResponseDto</param>
        /// <returns>Session</returns>
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
