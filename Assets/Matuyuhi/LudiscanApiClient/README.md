# Ludiscan API Client for Unity

A C# API client package for integrating Ludiscan player tracking and position logging into Unity games. Provides high-level APIs for session management, position tracking, and event logging.

## Features

- **Position Logging**: Track player positions (X, Y, Z coordinates) in real-time
- **Session Management**: Create and manage game sessions with automatic tracking
- **Event Logging**: Log custom game events and general gameplay events
- **Field Object Tracking**: Track field objects and game state
- **Project-based Tracking**: Organize tracking data by game projects
- **Async Support**: Full async/await support for network operations

## Installation

### Via Unity Package Manager (UPM) with Git URL

1. Open `Window > TextMesh Pro > Import TMP Essential Resources` (if needed)
2. Open your project's `Packages/manifest.json` file
3. Add the following entry to the `dependencies` section:

```json
{
  "dependencies": {
    "com.matuyuhi.ludiscan-api-client": "https://github.com/yourusername/ludiscan-unity-api-client.git#main",
    ...
  }
}
```

Or use the Package Manager UI:
- Go to `Window > Package Manager`
- Click the `+` button and select `Add package from git URL`
- Enter: `https://github.com/yourusername/ludiscan-unity-api-client.git#main`

## Quick Start

### 1. Initialize the Client

```csharp
using Matuyuhi.LudiscanApi.Client;

// Configure the client
LudiscanClientConfig config = new LudiscanClientConfig
{
    ApiBaseUrl = "http://localhost:3000",
    ProjectId = "your-project-id"
};

var client = new LudiscanClient(config);
```

### 2. Create a Game Session

```csharp
// Create a new session
var session = await client.CreateSessionAsync(new CreateSessionRequest
{
    GameName = "YourGame",
    Timestamp = DateTime.UtcNow
});

string sessionId = session.Id;
```

### 3. Log Player Positions

```csharp
// Use PositionLogger for continuous position tracking
var positionLogger = new PositionLogger(client, sessionId);

// Log a player position (typically called every frame or at intervals)
await positionLogger.LogPositionAsync(
    playerId: 1,
    x: 10.5f,
    y: 20.3f,
    z: 0.0f,
    timestamp: DateTime.UtcNow
);
```

### 4. Log Game Events

```csharp
var eventLogger = new GeneralEventLogger(client, sessionId);

// Log a custom game event
await eventLogger.LogEventAsync(new GeneralEventEntity
{
    EventType = "player_died",
    EventData = new Dictionary<string, object>
    {
        { "playerId", 1 },
        { "cause", "fall_damage" }
    },
    Timestamp = DateTime.UtcNow
});
```

### 5. Log Field Objects

```csharp
var fieldObjectLogger = new FieldObjectLogger(client, sessionId);

// Log field object state
await fieldObjectLogger.LogFieldObjectAsync(new CreateFieldObjectStream
{
    ObjectId = "obj_1",
    ObjectType = "obstacle",
    X = 15.0f,
    Y = 25.0f,
    Z = 5.0f,
    Timestamp = DateTime.UtcNow
});
```

## Configuration

### Environment Variables

The API client respects these environment variables:

- `LUDISCAN_API_URL`: Base URL for the Ludiscan API (default: `http://localhost:3000`)
- `LUDISCAN_PROJECT_ID`: Default project ID for sessions
- `LUDISCAN_API_KEY`: API key for authentication (if required)

### Programmatic Configuration

```csharp
var config = new LudiscanClientConfig
{
    ApiBaseUrl = "https://ludiscan.example.com",
    ProjectId = "project-123",
    Timeout = TimeSpan.FromSeconds(30),
    RetryCount = 3
};

var client = new LudiscanClient(config);
```

## Architecture

### Directory Structure

```
Runtime/
├── Plugins/
│   └── Matuyuhi.LudiscanApi.Client/    # Generated OpenAPI client DLL
└── Scripts/
    └── ApiClient/
        ├── LudiscanClient.cs            # Main client wrapper
        ├── LudiscanClientConfig.cs      # Configuration
        ├── PositionLogger.cs            # Position tracking utilities
        ├── GeneralEventLogger.cs        # Event logging utilities
        ├── FieldObjectLogger.cs         # Field object tracking
        ├── ErrorResponseException.cs    # Custom exception handling
        ├── IProject.cs                  # Project interface
        ├── ApiClient.asmdef             # Assembly definition
        └── Model/
            ├── PositionEntry.cs
            ├── Session.cs
            ├── GeneralEventEntity.cs
            └── ...
```

### Assembly Definition

The package uses Unity's Assembly Definition (`ApiClient.asmdef`) to organize code and manage dependencies on the precompiled `Matuyuhi.LudiscanApi.Client.dll`.

## API Client Generation

The OpenAPI API client is auto-generated from the Ludiscan backend Swagger specification.

### Regenerating the Client

If the backend API changes:

1. Ensure your local Ludiscan API is running on port 3211 (or update the Makefile):
   ```bash
   cd ludiscan-v0-api
   npm run start:dev
   ```

2. Generate the client:
   ```bash
   make gen
   ```

3. The generated DLL will be placed in `Runtime/Plugins/Matuyuhi.LudiscanApi.Client/`

### Prerequisites for Generation

- `openapi-generator-cli` installed globally or locally
- `.NET 6+` SDK with `dotnet` CLI
- Swagger JSON accessible at `http://localhost:3211/swagger/api/v0/json`

## Troubleshooting

### DLL Not Found
If you see errors about missing `Matuyuhi.LudiscanApi.Client.dll`:
1. Ensure the DLL is in `Runtime/Plugins/Matuyuhi.LudiscanApi.Client/`
2. Force Unity to reimport: `Assets > Reimport All`
3. Check your Assembly Definition is correctly configured

### Connection Issues
- Verify the API base URL is correct
- Check network connectivity to the Ludiscan server
- Ensure the project ID is valid
- Review logs for HTTP error codes

### Timeout Errors
Increase the timeout in configuration:
```csharp
config.Timeout = TimeSpan.FromSeconds(60);
```

## Contributing

To contribute improvements to this package:

1. Clone the repository
2. Make your changes
3. Test with a local Unity project
4. Submit a pull request

## Development Setup for Rider/Visual Studio

### Rider で開く場合（コード補完有効）

1. **Solution ファイルを開く**
   ```bash
   # Rider または Visual Studio で開く
   Open → LudiscanApiClient.sln
   ```

2. **Unity Path の自動検出**
   - Unity がインストールされていれば、自動的に検出されます
   - Mac: `/Applications/Unity/Hub/Editor/[version]`
   - Windows: `C:\Program Files\Unity\Hub\Editor\[version]`

3. **手動で Unity Path を設定**
   ```bash
   # Rider のターミナルで実行
   dotnet build /p:UnityInstallPath="/Applications/Unity/Hub/Editor/6000.2.6f2"
   ```

4. **コード補完が効かない場合**
   - `Preferences > Languages & Frameworks > C# > Code Completion` で IntelliSense を再読み込み
   - `File > Invalidate Caches and Restart` を実行
   - Solution をリロード

### VS Code での開発

C# Dev Kit 拡張機能をインストールして、同様に `.sln` ファイルを開くことでコード補完が有効になります。

### Troubleshooting: "UnityEngine not found"

**問題**: ApiClient.Dev.csproj 開いた時に UnityEngine が見つからないエラー

**解決方法**:

1. **Unity インストール確認**
   ```bash
   # Mac
   ls /Applications/Unity/Hub/Editor/

   # Windows
   dir "C:\Program Files\Unity\Hub\Editor\"
   ```

2. **Unity Path を明示的に設定**
   ```bash
   # プロジェクトルートで実行
   export UnityInstallPath="/Applications/Unity/Hub/Editor/6000.2.6f2"
   # または Windows
   set UnityInstallPath=C:\Program Files\Unity\Hub\Editor\2022.2.0f1
   ```

3. **Rider を再起動**
   - Unity Path の自動検出は Rider 起動時に行われるため、環境変数設定後は再起動が必要です

## License

MIT License - see LICENSE file for details

## Support

For issues, questions, or feature requests, please visit:
- GitHub Issues: [ludiscan-unity-api-client/issues](https://github.com/yourusername/ludiscan-unity-api-client/issues)
- Documentation: [Ludiscan Docs](https://ludiscan.example.com/docs)

## Dependencies

- Unity 2022.2 or later
- Matuyuhi.LudiscanApi.Client (generated from OpenAPI)
- RestSharp (included in generated DLL)
- Newtonsoft.Json (included in generated DLL)
- Polly (included in generated DLL)
