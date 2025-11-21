using Newtonsoft.Json;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    /// <summary>
    /// 一般イベントログエンティティ
    /// GamePlayMode の拡張イベント（player_spawn, collision_attempt, score_milestone など）を記録
    /// </summary>
    public struct GeneralEventEntity
    {
        /// <summary>
        /// イベントタイプ（snake_case）
        /// 例: player_spawn, collision_attempt, score_milestone, hand_changed
        /// </summary>
        [JsonProperty("event_type")]
        public string EventType;

        /// <summary>
        /// イベントのメタデータ（拡張イベント用）
        /// 例: { "aggressorId": 0, "targetId": 1, "result": "WIN" }
        /// </summary>
        [JsonProperty("metadata")]
        public object Metadata;

        /// <summary>
        /// ゲーム開始からのオフセット時間（ミリ秒）
        /// </summary>
        [JsonProperty("offset_timestamp")]
        public ulong OffsetTimeStamp;

        /// <summary>
        /// プレイヤーID（0-3）
        /// 複数プレイヤーイベント（collision_attemptなど）の場合は主体となるプレイヤーID
        /// </summary>
        [JsonProperty("player")]
        public int Player;

        /// <summary>
        /// イベントの位置情報（X, Y, Z座標）
        /// </summary>
        [JsonProperty("position")]
        public object Position;

        /// <summary>
        /// イベント発火時のスクリーンショット（PNG/JPEGバイト配列の配列）
        /// death, successなどの重要イベント時に、直前1-2秒間のスクリーンショット（通常4枚程度）を保持
        /// オプショナルフィールドで、nullの場合はスクリーンショットなし
        /// </summary>
        [JsonProperty("screenshots", NullValueHandling = NullValueHandling.Ignore)]
        public byte[][] Screenshots;
    }
}
