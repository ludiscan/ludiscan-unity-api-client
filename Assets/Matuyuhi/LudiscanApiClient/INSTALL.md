# Installation Guide

## Prerequisites

- Unity 2022.2 or later
- A Ludiscan backend API server running and accessible
- Git (for Git URL-based installation)

## Installation Methods

### Method 1: Git URL (Recommended)

This method allows you to always use the latest version from the repository.

1. **Open Package Manager**
   - In Unity, go to `Window > Package Manager`

2. **Add from Git URL**
   - Click the `+` button in the Package Manager
   - Select `Add package from git URL...`

3. **Enter the Git URL**
   ```
   https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient
   ```

4. **Click Add**
   - Unity will download and import the package

### Method 2: Manual manifest.json

1. **Open Packages/manifest.json**
   - In your project root, find `Packages/manifest.json`
   - Open it in a text editor

2. **Add the dependency**
   ```json
   {
     "dependencies": {
       "com.matuyuhi.ludiscan-api-client": "https://github.com/ludiscan/ludiscan-unity-api-client.git?path=Assets/Matuyuhi/LudiscanApiClient",
       "com.unity.nuget.newtonsoft-json": "3.0.0"
     }
   }
   ```

3. **Save and return to Unity**
   - Unity will automatically fetch and import the package

### Method 3: Local Development

For developing or modifying the package locally:

1. **Clone the repository**
   ```bash
   git clone https://github.com/ludiscan/ludiscan-unity-api-client.git
   cd ludiscan-unity-api-client
   ```

2. **Open in Unity**
   - Open this directory as a Unity project

3. **Dependencies will auto-import**
   - Unity will automatically resolve the `Newtonsoft.Json` dependency

## Verification

After installation, verify the package is correctly installed:

1. **Check Package Manager**
   - Go to `Window > Package Manager`
   - Look for "Ludiscan API Client" in the list (it should show version 1.0.0+)

2. **Check Assembly Definition**
   - Navigate to `Assets > Matuyuhi > LudiscanApiClient > Runtime > Scripts > ApiClient`
   - You should see `ApiClient.asmdef`

3. **Verify DLLs**
   - Check that `Runtime/Plugins/` contains:
     - `Matuyuhi.LudiscanApi.Client.dll`
     - `RestSharp/` folder with DLLs
     - `Polly/` folder with DLLs

4. **Check Console**
   - Open the Console window (`Window > General > Console`)
   - You should not see any assembly loading errors

## Troubleshooting Installation

### Issue: Package not found

**Solution:**
- Verify the Git URL is correct
- Ensure your GitHub repository is public (or you have access)
- Check your internet connection
- Try adding a version tag: `https://...git#v1.0.0`

### Issue: Newtonsoft.Json dependency error

**Solution:**
- Ensure `com.unity.nuget.newtonsoft-json` is in your dependencies
- Run `Assets > Reimport All` to refresh

### Issue: DLL import errors

**Solution:**
1. Check that all `.dll` files are in `Runtime/Plugins/`
2. Verify `.meta` files exist for each DLL
3. Run `Assets > Reimport All`
4. Restart Unity

### Issue: Assembly definition conflicts

**Solution:**
- Check that no other assemblies reference the same DLLs
- Verify `ApiClient.asmdef` has correct references
- Use `Assets > Reimport All` if needed

## Configuration After Installation

Once installed, you need to configure the API client:

### 1. Set API Endpoint

```csharp
using Matuyuhi.LudiscanApi.Client;

// In your initialization code
var config = new LudiscanClientConfig
{
    ApiBaseUrl = "http://your-ludiscan-server:3000",
    ProjectId = "your-project-id"
};

LudiscanClient.Initialize(config);
```

### 2. Environment Variables (Optional)

Set these environment variables for your Unity player:

```bash
LUDISCAN_API_URL=http://ludiscan-server:3000
LUDISCAN_PROJECT_ID=your-project-id
```

### 3. Test Connection

```csharp
// Test that the client can connect
if (LudiscanClient.IsInitialized)
{
    Debug.Log("Ludiscan API Client initialized successfully");
}
```

## Updating the Package

To update to a newer version:

### Via Package Manager UI

1. Open `Window > Package Manager`
2. Find "Ludiscan API Client"
3. Click the package name
4. Use the version dropdown to select a new version
5. Click "Update"

### Via manifest.json

Edit `Packages/manifest.json` and update the version tag:

```json
"com.matuyuhi.ludiscan-api-client": "https://github.com/ludiscan/ludiscan-unity-api-client.git#v1.1.0"
```

## Uninstallation

To remove the package:

1. **Via Package Manager**
   - Right-click the package in Package Manager
   - Select "Remove"

2. **Via manifest.json**
   - Remove the dependency line from `Packages/manifest.json`
   - Save and return to Unity

## Next Steps

After successful installation:

1. Read the [README.md](README.md) for usage examples
2. Check the [Changelog](CHANGELOG.md) for what's new
3. Review API reference in README.md
4. Test with a simple example project

## Support

If you encounter installation issues:

1. Check the [Troubleshooting](README.md#troubleshooting) section
2. Review [GitHub Issues](https://github.com/ludiscan/ludiscan-unity-api-client/issues)
3. Check the Ludiscan documentation: https://ludiscan.example.com/docs
