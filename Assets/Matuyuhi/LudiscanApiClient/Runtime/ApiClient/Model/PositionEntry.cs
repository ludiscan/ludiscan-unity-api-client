using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    /// <summary>
    /// プレイヤーの位置情報を表すエンティティ
    /// PositionLoggerで使用され、定期的に記録されます
    /// </summary>
    public struct PositionEntry
    {
        private Vector3 position;

        /// <summary>
        /// X座標
        /// </summary>
        public float X
        {
            get => position.x;
            set => position.x = value;
        }

        /// <summary>
        /// Y座標
        /// </summary>
        public float Y
        {
            get => position.y;
            set => position.y = value;
        }

        /// <summary>
        /// Z座標
        /// </summary>
        public float Z
        {
            get => position.z;
            set => position.z = value;
        }

        /// <summary>
        /// Unity座標（Vector3形式）
        /// </summary>
        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// ゲーム開始からのオフセット時間（ミリ秒）
        /// </summary>
        public ulong OffsetTimeStamp;

        /// <summary>
        /// プレイヤーID
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// 追加情報（任意）
        /// </summary>
        public object Status;
    }
}