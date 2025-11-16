# Ludiscan API Client for Unity

A C# API client package for integrating Ludiscan player tracking and position logging into Unity games. Provides high-level APIs for session management, position tracking, and event logging.

## Features

- **Position Logging**: Track player positions (X, Y, Z coordinates) in real-time with automatic buffering
- **Session Management**: Create and manage game sessions with automatic tracking
- **Event Logging**: Log custom game events and general gameplay events
- **Field Object Tracking**: Track field objects (items, enemies, etc.) and their states
- **Project-based Tracking**: Organize tracking data by game projects
- **Async Support**: Full async/await support for network operations
- **Singleton Pattern**: Easy-to-use singleton instances for all loggers

## Installation

### Via Unity Package Manager (Git URL)

Add this to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.matuyuhi.ludiscan-api-client": "https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient",
    "com.unity.nuget.newtonsoft-json": "3.0.0"
  }
}
```

Or use the Package Manager UI in Unity:
- Go to `Window > Package Manager`
- Click the `+` button and select `Add package from git URL`
- Enter: `https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient`

For detailed installation instructions, see [INSTALL.md](Assets/Matuyuhi/LudiscanApiClient/INSTALL.md).

## Quick Start (Minimal Implementation)

この最小限の実装例で、基本的なセッショントラッキングを開始できます。

```csharp
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

public class MinimalLudiscanExample : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "https://ludiscan.net/api";
    [SerializeField] private string apiKey = "your-api-key-here";
    [SerializeField] private string projectId = "your-project-id";

    private Session currentSession;

    private async void Start()
    {
        // 1. クライアントを初期化
        var config = new LudiscanClientConfig(apiBaseUrl, apiKey)
        {
            TimeoutSeconds = 10
        };
        LudiscanClient.Initialize(config);

        // 2. セッションを作成
        var project = new Project { ProjectId = projectId };
        var sessionDto = await LudiscanClient.Instance.CreateSession(project, "My Game Session");
        currentSession = Session.FromDto(sessionDto);

        Debug.Log($"Session started: {currentSession.SessionId}");
    }

    private async void OnApplicationQuit()
    {
        // 3. セッションを終了
        if (currentSession != null && currentSession.IsActive)
        {
            await LudiscanClient.Instance.FinishSession(currentSession);
            Debug.Log("Session finished");
        }
    }
}
```

## Complete Game Loop Implementation

ゲームの1ループ全体を実装する完全な例です。プレイヤーの位置トラッキング、イベントロギング、フィールドオブジェクトの追跡を含みます。

### Step 1: GameManager でセッション管理

```csharp
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Ludiscan API Configuration")]
    [SerializeField] private string apiBaseUrl = "https://ludiscan.net/api";
    [SerializeField] private string apiKey = "your-api-key-here";
    [SerializeField] private string projectId = "your-project-id";

    [Header("Session Settings")]
    [SerializeField] private string sessionName = "Game Session";
    [SerializeField] private string mapName = "Level_01";

    [Header("Logger Settings")]
    [SerializeField] private int positionBufferSize = 1000;
    [SerializeField] private int eventLogCapacity = 2000;
    [SerializeField] private int fieldObjectCapacity = 1000;

    [Header("Upload Intervals")]
    [SerializeField] private float positionUploadInterval = 10f; // 秒
    [SerializeField] private float eventUploadInterval = 15f; // 秒

    private Session currentSession;
    private Project selectedProject;
    private float positionUploadTimer;
    private float eventUploadTimer;
    private long sessionStartTime;

    public static GameManager Instance { get; private set; }
    public Session CurrentSession => currentSession;
    public bool IsSessionActive => currentSession != null && currentSession.IsActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await InitializeLudiscan();
    }

    /// <summary>
    /// Ludiscanの初期化とセッション作成
    /// </summary>
    private async Task InitializeLudiscan()
    {
        // 1. クライアントの初期化
        var config = new LudiscanClientConfig(apiBaseUrl, apiKey)
        {
            TimeoutSeconds = 10
        };
        LudiscanClient.Initialize(config);
        Debug.Log("LudiscanClient initialized");

        // 2. API接続テスト
        bool pingSuccess = await LudiscanClient.Instance.Ping();
        if (!pingSuccess)
        {
            Debug.LogError("Failed to connect to Ludiscan API");
            return;
        }
        Debug.Log("API connection successful");

        // 3. プロジェクトの設定（Project IDを直接指定）
        selectedProject = new Project { ProjectId = projectId };

        // 4. セッションの作成
        var sessionDto = await LudiscanClient.Instance.CreateSession(selectedProject, sessionName);
        currentSession = Session.FromDto(sessionDto);
        sessionStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Debug.Log($"Session created: {currentSession.SessionId}");

        // 5. マップ名の設定
        currentSession = await LudiscanClient.Instance.PutMapName(currentSession, mapName);
        Debug.Log($"Map name set: {mapName}");

        // 6. 各種ロガーの初期化
        PositionLogger.Initialize(positionBufferSize);
        GeneralEventLogger.Initialize(eventLogCapacity);
        FieldObjectLogger.Initialize(fieldObjectCapacity);
        Debug.Log("All loggers initialized");

        // 7. ポジションロギングの開始（250ms間隔）
        PositionLogger.Instance.OnLogPosition = GetAllPlayerPositions;
        PositionLogger.Instance.StartLogging(250);
        Debug.Log("Position logging started");
    }

    private void Update()
    {
        if (!IsSessionActive) return;

        // 定期的にデータをアップロード
        positionUploadTimer += Time.deltaTime;
        if (positionUploadTimer >= positionUploadInterval)
        {
            positionUploadTimer = 0f;
            _ = UploadPositionData();
        }

        eventUploadTimer += Time.deltaTime;
        if (eventUploadTimer >= eventUploadInterval)
        {
            eventUploadTimer = 0f;
            _ = UploadEventAndFieldObjectData();
        }
    }

    /// <summary>
    /// 全プレイヤーの位置を取得
    /// </summary>
    private System.Collections.Generic.List<PositionEntry> GetAllPlayerPositions()
    {
        var positions = new System.Collections.Generic.List<PositionEntry>();

        // 例: GameManagerで管理しているプレイヤーから位置を取得
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            positions.Add(new PositionEntry
            {
                PlayerId = player.PlayerId,
                Position = player.transform.position
            });
        }

        return positions;
    }

    /// <summary>
    /// 位置データをアップロード
    /// </summary>
    private async Task UploadPositionData()
    {
        if (!PositionLogger.IsInitialized) return;

        try
        {
            var buffer = PositionLogger.Instance.Buffer;
            if (buffer == null || buffer.Length == 0) return;

            await LudiscanClient.Instance.UploadPosition(currentSession, buffer);
            Debug.Log("Position data uploaded");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to upload position data: {e.Message}");
        }
    }

    /// <summary>
    /// イベントとフィールドオブジェクトデータをアップロード
    /// </summary>
    private async Task UploadEventAndFieldObjectData()
    {
        // イベントログのアップロード
        if (GeneralEventLogger.IsInitialized)
        {
            try
            {
                var eventLogs = GeneralEventLogger.Instance.GetLogsAndClear();
                if (eventLogs.Length > 0)
                {
                    await LudiscanClient.Instance.UploadGeneralEventLogs(currentSession, eventLogs);
                    Debug.Log($"Uploaded {eventLogs.Length} event logs");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upload event logs: {e.Message}");
            }
        }

        // フィールドオブジェクトログのアップロード
        if (FieldObjectLogger.IsInitialized)
        {
            try
            {
                var fieldLogs = FieldObjectLogger.Instance.GetLogsAndClear();
                if (fieldLogs.Length > 0)
                {
                    await LudiscanClient.Instance.UploadFieldObjectLogs(currentSession, fieldLogs);
                    Debug.Log($"Uploaded {fieldLogs.Length} field object logs");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upload field object logs: {e.Message}");
            }
        }
    }

    /// <summary>
    /// スコアを更新
    /// </summary>
    public async Task UpdateScore(int score)
    {
        if (!IsSessionActive) return;

        try
        {
            currentSession = await LudiscanClient.Instance.PutScore(currentSession, score);
            Debug.Log($"Score updated: {score}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update score: {e.Message}");
        }
    }

    /// <summary>
    /// セッション開始からのオフセットタイムスタンプを取得
    /// </summary>
    public ulong GetOffsetTimestamp()
    {
        long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (ulong)(currentTime - sessionStartTime);
    }

    private async void OnApplicationQuit()
    {
        // ロギング停止
        if (PositionLogger.IsInitialized)
        {
            PositionLogger.Instance.StopLogging();
        }

        // 残りのデータをアップロード
        await UploadPositionData();
        await UploadEventAndFieldObjectData();

        // セッション終了
        if (IsSessionActive)
        {
            await LudiscanClient.Instance.FinishSession(currentSession);
            Debug.Log("Session finished");
        }
    }
}
```

### Step 2: PlayerController でイベントロギング

```csharp
using LudiscanApiClient.Runtime.ApiClient;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int playerId = 0;

    public int PlayerId => playerId;

    private void Start()
    {
        // プレイヤースポーンイベントを記録
        LogPlayerSpawn();
    }

    /// <summary>
    /// プレイヤースポーンを記録
    /// </summary>
    private void LogPlayerSpawn()
    {
        if (!GeneralEventLogger.IsInitialized) return;

        var metadata = new
        {
            spawn_point = "start_position",
            is_respawn = false
        };

        GeneralEventLogger.Instance.AddLog(
            "player_spawn",
            metadata,
            GameManager.Instance.GetOffsetTimestamp(),
            transform.position,
            playerId
        );

        Debug.Log($"Player {playerId} spawned at {transform.position}");
    }

    /// <summary>
    /// プレイヤー死亡を記録
    /// </summary>
    public void OnPlayerDeath(string deathCause)
    {
        if (!GeneralEventLogger.IsInitialized) return;

        var metadata = new
        {
            death_cause = deathCause
        };

        GeneralEventLogger.Instance.AddLog(
            "death",
            metadata,
            GameManager.Instance.GetOffsetTimestamp(),
            transform.position,
            playerId
        );

        Debug.Log($"Player {playerId} died: {deathCause}");
    }

    /// <summary>
    /// アイテム取得を記録
    /// </summary>
    public void OnItemCollected(string itemType)
    {
        if (!GeneralEventLogger.IsInitialized) return;

        var metadata = new
        {
            item_type = itemType,
            collect_method = "pickup"
        };

        GeneralEventLogger.Instance.AddLog(
            "get_hand_change_item",
            metadata,
            GameManager.Instance.GetOffsetTimestamp(),
            transform.position,
            playerId
        );

        Debug.Log($"Player {playerId} collected item: {itemType}");
    }
}
```

### Step 3: ItemManager でフィールドオブジェクトトラッキング

```csharp
using LudiscanApiClient.Runtime.ApiClient;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string itemType = "health_potion";

    private void Start()
    {
        // ユニークなIDを生成
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = System.Guid.NewGuid().ToString();
        }

        // アイテムスポーンを記録
        LogItemSpawn();
    }

    /// <summary>
    /// アイテムスポーンを記録
    /// </summary>
    private void LogItemSpawn()
    {
        if (!FieldObjectLogger.IsInitialized) return;

        uint offsetTimestamp = GetOffsetTimestamp();
        var metadata = new { spawn_reason = "level_start" };

        FieldObjectLogger.Instance.LogItemSpawn(
            itemId,
            itemType,
            transform.position,
            offsetTimestamp,
            metadata
        );

        Debug.Log($"Item spawned: {itemType} ({itemId})");
    }

    /// <summary>
    /// アイテムピックアップを記録
    /// </summary>
    public void OnPickedUp(int playerId)
    {
        if (!FieldObjectLogger.IsInitialized) return;

        uint offsetTimestamp = GetOffsetTimestamp();

        FieldObjectLogger.Instance.LogItemDespawn(
            itemId,
            itemType,
            transform.position,
            offsetTimestamp,
            playerId
        );

        Debug.Log($"Item picked up: {itemType} by player {playerId}");
        Destroy(gameObject);
    }

    private uint GetOffsetTimestamp()
    {
        if (!FieldObjectLogger.IsInitialized) return 0;

        long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (uint)(currentTime - FieldObjectLogger.Instance.SessionStartTime);
    }
}
```

## API Reference

### LudiscanClient

クライアントの初期化とセッション管理を行います。

```csharp
// 初期化
var config = new LudiscanClientConfig(apiBaseUrl, apiKey)
{
    TimeoutSeconds = 10
};
LudiscanClient.Initialize(config);

// 接続テスト
bool isConnected = await LudiscanClient.Instance.Ping();

// プロジェクト一覧取得（オプション）
List<ProjectResponseDto> projects = await LudiscanClient.Instance.GetProjects();

// セッション作成
var project = new Project { ProjectId = "your-project-id" };
PlaySessionResponseDto sessionDto = await LudiscanClient.Instance.CreateSession(project, "Session Name");
Session session = Session.FromDto(sessionDto);

// マップ名設定
session = await LudiscanClient.Instance.PutMapName(session, "Level_01");

// スコア更新
session = await LudiscanClient.Instance.PutScore(session, 100);

// セッション終了
session = await LudiscanClient.Instance.FinishSession(session);
```

### PositionLogger

プレイヤー位置の自動トラッキングを行います。

```csharp
// 初期化
PositionLogger.Initialize(bufferSize: 1000);

// 位置取得コールバックの設定
PositionLogger.Instance.OnLogPosition = () =>
{
    var positions = new List<PositionEntry>
    {
        new PositionEntry
        {
            PlayerId = 0,
            Position = playerTransform.position
        }
    };
    return positions;
};

// ロギング開始（250ms間隔）
PositionLogger.Instance.StartLogging(intervalMilliseconds: 250);

// バッファのアップロード
PositionEntry[] buffer = PositionLogger.Instance.Buffer;
await LudiscanClient.Instance.UploadPosition(session, buffer);

// ロギング停止
PositionLogger.Instance.StopLogging();
```

### GeneralEventLogger

ゲームイベントのロギングを行います。

```csharp
// 初期化
GeneralEventLogger.Initialize(initialCapacity: 2000);

// イベントログの追加
GeneralEventLogger.Instance.AddLog(
    eventType: "player_spawn",
    metadata: new { spawn_point = "checkpoint_1" },
    offsetTimestamp: GetOffsetTimestamp(),
    position: Vector3.zero,
    playerId: 0
);

// ログの取得とクリア
GeneralEventEntity[] logs = GeneralEventLogger.Instance.GetLogsAndClear();

// アップロード
await LudiscanClient.Instance.UploadGeneralEventLogs(session, logs);

// ログ数の確認
int count = GeneralEventLogger.Instance.LogCount;
```

### FieldObjectLogger

フィールドオブジェクト（アイテム、敵など）のトラッキングを行います。

```csharp
// 初期化
FieldObjectLogger.Initialize(initialCapacity: 1000);

// アイテムスポーン
FieldObjectLogger.Instance.LogItemSpawn(
    itemId: "item_001",
    itemType: "health_potion",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    metadata: new { spawn_reason = "game_start" }
);

// アイテムデスポーン（ピックアップ）
FieldObjectLogger.Instance.LogItemDespawn(
    itemId: "item_001",
    itemType: "health_potion",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    playerId: 0 // 取得したプレイヤーID
);

// 敵スポーン
FieldObjectLogger.Instance.LogEnemySpawn(
    enemyId: "enemy_001",
    enemyType: "goblin",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    metadata: new { difficulty = "normal" }
);

// 敵移動
FieldObjectLogger.Instance.LogEnemyMove(
    enemyId: "enemy_001",
    enemyType: "goblin",
    position: new Vector3(10, 0, 5),
    offsetTimestamp: GetOffsetTimestamp()
);

// 敵死亡
FieldObjectLogger.Instance.LogEnemyDeath(
    enemyId: "enemy_001",
    enemyType: "goblin",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    killedByPlayerId: 0
);

// カスタムオブジェクトの更新
FieldObjectLogger.Instance.LogObjectUpdate(
    objectId: "obj_001",
    objectType: "door",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    status: new { is_open = true }
);

// ログの取得とクリア
FieldObjectEntity[] logs = FieldObjectLogger.Instance.GetLogsAndClear();

// アップロード
await LudiscanClient.Instance.UploadFieldObjectLogs(session, logs);
```

## Common Event Types

### GeneralEventLogger でよく使われるイベントタイプ

| Event Type | Description | Metadata Example |
|------------|-------------|------------------|
| `player_spawn` | プレイヤースポーン | `{ spawn_point, is_respawn }` |
| `death` | プレイヤー死亡 | `{ death_cause, lives_remaining }` |
| `success` | ゴール到達 | `{ completion_time, ranking }` |
| `game_phase_changed` | ゲームフェーズ変更 | `{ from_phase, to_phase }` |
| `get_hand_change_item` | アイテム取得 | `{ item_type, collect_method }` |
| `use_dash_item` | ダッシュアイテム使用 | `{ dash_direction, dash_distance }` |
| `hand_changed` | 手の変更 | `{ from_hand, to_hand }` |
| `collision_attempt` | 衝突試行 | `{ target_type, collision_success }` |
| `player_catch` | 敵を捕獲 | `{ enemy_type, catch_method }` |
| `score_milestone` | スコアマイルストーン | `{ current_score, milestone }` |

## Configuration

### LudiscanClientConfig

```csharp
public class LudiscanClientConfig
{
    public string ApiBaseUrl { get; set; }      // Ludiscan APIのベースURL
    public string XapiKey { get; set; }         // APIキー
    public int TimeoutSeconds { get; set; }     // タイムアウト（デフォルト: 30秒）
}

// 使用例
var config = new LudiscanClientConfig(
    apiBaseUrl: "https://ludiscan.net/api",
    apiKey: "your-api-key"
)
{
    TimeoutSeconds = 10
};
```

### API Key と Project ID の取得方法

1. Ludiscanのダッシュボードにログイン
2. プロジェクト設定からAPI Keyを取得
3. プロジェクトページからProject IDを確認
4. これらの値をUnityのInspectorまたは環境変数で設定

## Dependencies

このパッケージは以下のライブラリに依存しています：

- **RestSharp** (v107.3.0) - HTTP client library（パッケージに含まれています）
- **Polly** - Resilience and transient-fault-handling library（パッケージに含まれています）
- **Newtonsoft.Json** - JSON serialization（`com.unity.nuget.newtonsoft-json` 経由）

## Requirements

- Unity 2022.2 or later
- Ludiscan backend API server running and accessible
- .NET Standard 2.1 compatible

## Best Practices

### 1. アップロード頻度の最適化

```csharp
// 推奨設定
- Position Logger: 10-15秒ごと
- Event Logger: 15-30秒ごと
- Field Object Logger: 15-30秒ごと

// バッファサイズ
- Position Buffer: 1000-2000エントリ
- Event Capacity: 2000-5000エントリ
- Field Object Capacity: 1000-2000エントリ
```

### 2. エラーハンドリング

```csharp
try
{
    await LudiscanClient.Instance.UploadPosition(session, buffer);
}
catch (ApiException e)
{
    // API呼び出しエラー
    Debug.LogError($"API Error: {e.Message}");
}
catch (ErrorResponseException e)
{
    // Ludiscan固有のエラー
    Debug.LogError($"Ludiscan Error: {e.Error.Message}");
}
catch (Exception e)
{
    // その他のエラー
    Debug.LogError($"Unexpected Error: {e.Message}");
}
```

### 3. アプリケーション終了時の処理

```csharp
private async void OnApplicationQuit()
{
    // 1. ロギング停止
    if (PositionLogger.IsInitialized)
    {
        PositionLogger.Instance.StopLogging();
    }

    // 2. 残りのデータをアップロード
    await UploadAllPendingData();

    // 3. セッション終了
    if (GameManager.Instance.IsSessionActive)
    {
        await LudiscanClient.Instance.FinishSession(GameManager.Instance.CurrentSession);
    }
}
```

## Troubleshooting

### 接続エラー

```csharp
// Ping でAPI接続をテスト
bool isConnected = await LudiscanClient.Instance.Ping();
if (!isConnected)
{
    Debug.LogError("Cannot connect to Ludiscan API");
    // API URLとネットワーク設定を確認
}
```

### タイムアウトエラー

```csharp
// タイムアウトを長くする
var config = new LudiscanClientConfig(apiBaseUrl, apiKey)
{
    TimeoutSeconds = 30 // デフォルトより長く設定
};
```

### データが送信されない

1. `LudiscanClient.IsInitialized` が `true` か確認
2. `Session.IsActive` が `true` か確認
3. バッファにデータが存在するか確認
4. ネットワーク接続を確認

## Examples

完全なサンプルコードは以下のディレクトリにあります：

- [LudiscanBasicExample.cs](Assets/Matuyuhi/LudiscanApiClient/Examples/Scripts/LudiscanBasicExample.cs) - 基本的な使い方
- [PositionLoggerExample.cs](Assets/Matuyuhi/LudiscanApiClient/Examples/Scripts/PositionLoggerExample.cs) - 位置ロギング
- [GeneralEventLoggerExample.cs](Assets/Matuyuhi/LudiscanApiClient/Examples/Scripts/GeneralEventLoggerExample.cs) - イベントロギング
- [FieldObjectLoggerExample.cs](Assets/Matuyuhi/LudiscanApiClient/Examples/Scripts/FieldObjectLoggerExample.cs) - フィールドオブジェクトトラッキング

## Documentation

- [Full README](Assets/Matuyuhi/LudiscanApiClient/README.md) - Package内の詳細ドキュメント
- [Installation Guide](Assets/Matuyuhi/LudiscanApiClient/INSTALL.md) - インストール方法とトラブルシューティング
- [Changelog](Assets/Matuyuhi/LudiscanApiClient/CHANGELOG.md) - バージョン履歴とリリースノート

## Support

For issues, questions, or feature requests:
- [GitHub Issues](https://github.com/ludiscan/ludiscan-unity-api-client/issues)
- [Ludiscan Documentation](https://ludiscan.net/docs)

## License

MIT License - see [LICENSE](Assets/Matuyuhi/LudiscanApiClient/LICENSE) for details

## Contributing

To contribute improvements to this package:

1. Clone the repository
2. Make your changes in a feature branch
3. Test with a local Unity project
4. Regenerate API client if needed: `make gen`
5. Submit a pull request

---

## Release Notes

### v1.1.1 (Latest)
- Updated documentation with complete implementation guide
- Added comprehensive code examples for all features
- Improved API reference documentation
