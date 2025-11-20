# Ludiscan API Client for Unity

A C# API client package for integrating Ludiscan player tracking and position logging into Unity games.

[![Unity Version](https://img.shields.io/badge/Unity-2022.2%2B-blue)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

This package provides high-level APIs for:
- **Session Management** - Create and manage game sessions
- **Position Logging** - Track player positions in real-time with automatic buffering
- **Event Logging** - Log custom game events
- **Event Screenshots** - Automatically capture screenshots for critical events (death, success)
- **Field Object Tracking** - Track items, enemies, and other game objects

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
1. Go to `Window > Package Manager`
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient`

## Quick Start

```csharp
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Session currentSession;

    private async void Start()
    {
        // 1. Initialize client
        var config = new LudiscanClientConfig("https://ludiscan.net/api", "your-api-key")
        {
            TimeoutSeconds = 10
        };
        LudiscanClient.Initialize(config);

        // 2. Create session
        var project = new Project { ProjectId = "your-project-id" };
        var sessionDto = await LudiscanClient.Instance.CreateSession(project, "Game Session");
        currentSession = Session.FromDto(sessionDto);

        // 3. Initialize loggers
        PositionLogger.Initialize(1000);
        GeneralEventLogger.Initialize(2000);
        FieldObjectLogger.Initialize(1000);
    }

    private async void OnApplicationQuit()
    {
        if (currentSession != null && currentSession.IsActive)
        {
            await LudiscanClient.Instance.FinishSession(currentSession);
        }
    }
}
```

## Documentation

**📖 For detailed implementation guide, API reference, and complete examples:**

**See [Package README](Assets/Matuyuhi/LudiscanApiClient/README.md)** - Complete documentation including:
- Minimal implementation example
- Complete game loop implementation (GameManager, PlayerController, ItemManager)
- Detailed API reference for all loggers
- Common event types reference
- Best practices and troubleshooting

**Additional Resources:**
- [Installation Guide](Assets/Matuyuhi/LudiscanApiClient/INSTALL.md) - Detailed installation and troubleshooting
- [Changelog](Assets/Matuyuhi/LudiscanApiClient/CHANGELOG.md) - Version history

## Features

### Position Logging
```csharp
PositionLogger.Instance.OnLogPosition = GetAllPlayerPositions;
PositionLogger.Instance.StartLogging(250); // 250ms interval
```

### Event Logging
```csharp
GeneralEventLogger.Instance.AddLog(
    "player_spawn",
    metadata: new { spawn_point = "start" },
    offsetTimestamp: GetOffsetTimestamp(),
    position: transform.position,
    playerId: 0
);
```

### Event Screenshots (NEW in v1.3.0)
Automatically capture screenshots for critical game events (death, success):
```csharp
// Initialize screenshot capture (typically in Start())
EventScreenshotCapture.Initialize(autoStartCapture: true);
EventScreenshotCapture.Instance.ConfigureCapture(
    interval: 0.5f,    // Capture every 0.5 seconds
    bufferSize: 5,     // Keep latest 5 screenshots (~2.5 seconds)
    scale: 0.5f,       // Half resolution for smaller file size
    quality: 75        // JPEG quality (0-100, or 0 for PNG)
);

// Screenshots are automatically attached to "death" and "success" events
// Customize which events trigger screenshot capture:
GeneralEventLogger.Instance.ScreenshotEventTypes.Add("boss_defeated");
```

### Field Object Tracking
```csharp
FieldObjectLogger.Instance.LogItemSpawn(
    itemId: "item_001",
    itemType: "health_potion",
    position: Vector3.zero,
    offsetTimestamp: GetOffsetTimestamp(),
    metadata: new { spawn_reason = "game_start" }
);
```

## Requirements

- Unity 2022.2 or later
- .NET Standard 2.1 compatible
- Ludiscan backend API server

## Dependencies

All dependencies are included in the package:
- RestSharp (v107.3.0) - HTTP client
- Polly - Resilience and fault-handling
- Newtonsoft.Json - JSON serialization (via `com.unity.nuget.newtonsoft-json`)

## Development

### Package Structure

```
ludiscan-unity-api-client/
├── Assets/Matuyuhi/LudiscanApiClient/    # Unity Package
│   ├── Runtime/
│   │   ├── ApiClient/                     # Client code
│   │   └── Plugins/                       # DLL dependencies
│   ├── Examples/                          # Sample scripts
│   ├── README.md                          # 📖 Complete documentation
│   └── package.json
├── README.md                              # This file (quick reference)
└── Makefile                               # Build tools
```

### Regenerating API Client

If the Ludiscan backend API changes:

```bash
# Start Ludiscan API on port 3211
cd ludiscan-v0-api
npm run start:dev

# Generate new client
cd ludiscan-unity-api-client
make gen
```

**Prerequisites:**
- `openapi-generator-cli` installed
- .NET 6+ SDK with `dotnet` CLI
- Swagger JSON accessible at `http://localhost:3211/swagger/api/v0/json`

## Support

- [GitHub Issues](https://github.com/ludiscan/ludiscan-unity-api-client/issues)
- [Ludiscan Documentation](https://ludiscan.net/docs)

## License

MIT License - see [LICENSE](Assets/Matuyuhi/LudiscanApiClient/LICENSE) for details

## Contributing

1. Clone the repository
2. Make your changes in a feature branch
3. Test with a local Unity project
4. Regenerate API client if needed: `make gen`
5. Submit a pull request

---

**For complete implementation guide and detailed examples, see [Assets/Matuyuhi/LudiscanApiClient/README.md](Assets/Matuyuhi/LudiscanApiClient/README.md)**
