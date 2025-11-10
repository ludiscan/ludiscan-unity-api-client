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

1. Open your project's `Packages/manifest.json` file
2. Add the following entry to the `dependencies` section:

```json
{
  "dependencies": {
    "com.matuyuhi.ludiscan-api-client": "https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient",
    "com.unity.nuget.newtonsoft-json": "3.0.0"
  }
}
```

Or use the Package Manager UI:
- Go to `Window > Package Manager`
- Click the `+` button and select `Add package from git URL`
- Enter: `https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient`

### Dependencies

This package automatically includes:
- **RestSharp** - HTTP client library
- **Polly** - Resilience and transient-fault-handling library
- **Newtonsoft.Json** - JSON serialization (via `com.unity.nuget.newtonsoft-json`)

All necessary dependencies are bundled in the `Runtime/Plugins/` directory.

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

## Package Structure

```
Assets/Matuyuhi/LudiscanApiClient/
├── Runtime/
│   ├── Scripts/ApiClient/
│   │   ├── LudiscanClient.cs
│   │   ├── LudiscanClientConfig.cs
│   │   ├── PositionLogger.cs
│   │   ├── GeneralEventLogger.cs
│   │   ├── FieldObjectLogger.cs
│   │   ├── Model/
│   │   │   ├── PositionEntry.cs
│   │   │   ├── Session.cs
│   │   │   ├── GeneralEventEntity.cs
│   │   │   └── ...
│   │   └── ApiClient.asmdef
│   └── Plugins/
│       ├── Matuyuhi.LudiscanApi.Client.dll
│       ├── RestSharp/
│       │   ├── RestSharp.dll
│       │   ├── System.ComponentModel.Annotations.dll
│       │   ├── System.IO.Pipelines.dll
│       │   ├── System.Text.Encodings.Web.dll
│       │   └── System.Text.Json.dll
│       └── Polly/
│           ├── Polly.dll
│           ├── Polly.Core.dll
│           ├── Microsoft.Bcl.AsyncInterfaces.dll
│           └── Microsoft.Bcl.TimeProvider.dll
├── package.json
├── README.md
├── Makefile
└── api-generate-config.json
```

## API Client Generation

The OpenAPI API client is auto-generated from the Ludiscan backend Swagger specification.

### Regenerating the Client

If the backend API changes:

1. Ensure your local Ludiscan API is running on port 3211:
   ```bash
   cd ludiscan-v0-api
   npm run start:dev
   ```

2. In the package directory, run:
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
If you see errors about missing DLLs:
1. Ensure all DLL files are in `Runtime/Plugins/`
2. Force Unity to reimport: `Assets > Reimport All`
3. Check that `.meta` files exist for all DLLs

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

### Assembly Definition Issues
If you see assembly definition errors:
1. Check that `Runtime/Scripts/ApiClient/ApiClient.asmdef` is correctly configured
2. Verify all referenced DLLs are in the `precompiledReferences` list
3. Use `Assets > Reimport All` to refresh

## Development

### Updating the Package

When the Ludiscan API changes:

1. Update `api-generate-config.json` if needed
2. Run `make gen` to regenerate the client DLL
3. Test with a local Ludiscan instance
4. Commit changes to the repository

### Package Versioning

Update `package.json` version when releasing new versions:
```json
{
  "version": "1.0.1",
  "name": "com.matuyuhi.ludiscan-api-client"
}
```

## API Reference

### LudiscanClient

Main client class for API operations.

**Methods:**
- `CreateSessionAsync(request)` - Create a new tracking session
- `GetProjectAsync(projectId)` - Get project information
- `UpdateSessionAsync(sessionId, data)` - Update session data

### PositionLogger

Utility class for logging player positions.

**Methods:**
- `LogPositionAsync(playerId, x, y, z, timestamp)` - Log a single position

### GeneralEventLogger

Utility class for logging game events.

**Methods:**
- `LogEventAsync(eventEntity)` - Log a game event

### FieldObjectLogger

Utility class for logging field object states.

**Methods:**
- `LogFieldObjectAsync(fieldObject)` - Log field object state

## Contributing

To contribute improvements to this package:

1. Clone the repository
2. Make your changes
3. Test with a local Unity project
4. Regenerate API client if needed: `make gen`
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues, questions, or feature requests, please visit:
- GitHub Issues: https://github.com/ludiscan/ludiscan-unity-api-client/issues
- Ludiscan Documentation: https://ludiscan.example.com/docs

## Changelog

### v1.0.0
- Initial release
- Support for position logging, session management, and event logging
- Pre-built API client DLL
- All dependencies included
