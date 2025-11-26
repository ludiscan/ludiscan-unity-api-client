using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    /// <summary>
    /// フィールドオブジェクトのイベントタイプ
    /// </summary>
    public enum FieldObjectEventType : byte
    {
        /// <summary>オブジェクトがスポーン（出現）</summary>
        Spawn = 0,
        /// <summary>オブジェクトが移動</summary>
        Move = 1,
        /// <summary>オブジェクトの状態が更新</summary>
        Update = 2,
        /// <summary>オブジェクトがデスポーン（消滅）</summary>
        Despawn = 3
    }

    /// <summary>
    /// フィールドオブジェクト（アイテム、敵など）のイベント情報を表すエンティティ
    /// FieldObjectLoggerで使用され、オブジェクトの出現・移動・消滅などのイベントを記録します
    /// </summary>
    public struct FieldObjectEntity
    {
        /// <summary>
        /// オブジェクトの一意識別子
        /// </summary>
        public string ObjectId;

        /// <summary>
        /// オブジェクトの種類（例: "health_potion", "goblin", "chest"）
        /// </summary>
        public string ObjectType;

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
        /// オブジェクトの状態や追加情報（任意）
        /// 例: { "picked_by": 0 }, { "killed_by": 1 }
        /// </summary>
        public object Status;

        /// <summary>
        /// イベントの種類（Spawn, Move, Update, Despawn）
        /// </summary>
        public FieldObjectEventType EventType;
    }
}
