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

## Quick Start

### 1. Initialize the Client

```csharp
using Matuyuhi.LudiscanApi.Client;

var config = new LudiscanClientConfig
{
    ApiBaseUrl = "http://localhost:3000",
    ProjectId = "your-project-id"
};

var client = new LudiscanClient(config);
```

### 2. Log Player Positions

```csharp
var positionLogger = new PositionLogger(client, sessionId);

await positionLogger.LogPositionAsync(
    playerId: 1,
    x: 10.5f,
    y: 20.3f,
    z: 0.0f,
    timestamp: DateTime.UtcNow
);
```

### 3. Log Game Events

```csharp
var eventLogger = new GeneralEventLogger(client, sessionId);

await eventLogger.LogEventAsync(new GeneralEventEntity
{
    EventType = "player_died",
    EventData = new Dictionary<string, object> { { "playerId", 1 } },
    Timestamp = DateTime.UtcNow
});
```

## Documentation

- [Full README](Assets/Matuyuhi/LudiscanApiClient/README.md) - Comprehensive guide with API reference
- [Installation Guide](Assets/Matuyuhi/LudiscanApiClient/INSTALL.md) - Detailed installation methods and troubleshooting
- [Changelog](Assets/Matuyuhi/LudiscanApiClient/CHANGELOG.md) - Version history and release notes

## Dependencies

This package includes and requires:
- **RestSharp** (v107.3.0) - HTTP client library
- **Polly** - Resilience and transient-fault-handling library
- **Newtonsoft.Json** - JSON serialization (via `com.unity.nuget.newtonsoft-json`)

## Requirements

- Unity 2022.2 or later
- Ludiscan backend API server running and accessible

## Support

For issues, questions, or feature requests:
- [GitHub Issues](https://github.com/ludiscan/ludiscan-unity-api-client/issues)
- [Ludiscan Documentation](https://ludiscan.example.com/docs)

## License

MIT License - see [LICENSE](Assets/Matuyuhi/LudiscanApiClient/LICENSE) for details

## Contributing

To contribute improvements to this package:

1. Clone the repository
2. Make your changes
3. Test with a local Unity project
4. Regenerate API client if needed: `make gen`
5. Submit a pull request
