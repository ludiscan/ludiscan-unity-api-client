# UnityHttpClient テスト環境セットアップガイド

このドキュメントは、UnityHttpClient のテストを効果的に実行するための環境セットアップ方法を説明します。

## 前提条件

### 必須

- Unity 2022.2 以上
- Test Framework パッケージ（通常は標準インストール）
- インターネット接続（統合テスト用）

### オプション

- Rider IDE（テスト実行の強化デバッグ）
- Visual Studio Code with C# extension

## テスト環境の設定

### 1. テストフレームワークの確認

Unity Test Framework は通常プリインストールされています。確認方法：

1. `Window > TextMesh Pro > Import TMP Essential Resources` は無視
2. `Window > Testing > Test Runner` で Test Runner ウィンドウを開く
3. テストスイートが表示されることを確認

### 2. アセンブリ定義の確認

テストが実行されるには以下のファイルが必須です：

```
Assets/Matuyuhi/LudiscanApiClient/Tests/
├── LudiscanApiClient.Tests.asmdef        ← 重要
├── Editor/
│   ├── UnityHttpClientTests.cs
│   └── HttpClientIntegrationTestFixture.cs
├── README.md
└── TESTING_SETUP.md
```

### 3. 依存関係の確認

`LudiscanApiClient.Tests.asmdef` に以下が含まれているか確認：

```json
{
    "name": "LudiscanApiClient.Tests",
    "references": [
        "GUID:65a5f7c38c8754a45b26e063399e35f9",  // LudiscanApiClient.Runtime
        "GUID:df380645baf17461fa137f75e86a10ad"   // LudiscanApiClient.Runtime.ApiClient
    ],
    "precompiledReferences": [
        "nunit.framework.dll",
        "Newtonsoft.Json.dll"
    ]
}
```

**GUIDが異なる場合:**

1. Finder/Explorer で asmdef ファイルを右クリック
2. 「Show in Explorer」を選択
3. ファイル内の GUID を確認し、実際の Runtime.asmdef の GUID に置き換え

## テストの実行方法

### Unity Editor での実行

#### 方法1: Test Runner ウィンドウ

```
1. Window > Testing > Test Runner
2. "EditMode" タブをクリック
3. "Run All" または個別テストを実行
```

#### 方法2: テストファイルから直接実行

テストファイル内で右クリック → "Run Tests"

### コマンドラインでの実行

#### 全テストを実行

```bash
/Applications/Unity/Hub/Versions/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -runTests \
  -testPlatform editmode \
  -logFile - \
  -quit
```

#### 特定のテストクラスを実行

```bash
/Applications/Unity/Hub/Versions/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -runTests \
  -testPlatform editmode \
  -testFilter "LudiscanApiClient.Tests.UnityHttpClientTests" \
  -logFile - \
  -quit
```

#### 特定のテストメソッドを実行

```bash
/Applications/Unity/Hub/Versions/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -runTests \
  -testPlatform editmode \
  -testFilter "LudiscanApiClient.Tests.UnityHttpClientTests.BuildUrl_SimpleEndpoint_ReturnsCorrectUrl" \
  -logFile - \
  -quit
```

## テストの種類と実行時間

| テストクラス | テスト数 | 実行時間 | 説明 |
|---|---|---|---|
| UnityHttpClientTests | 11 | <1秒 | 単体テスト（ネットワークなし） |
| HttpClientIntegrationTestFixture | 11 | 20-30秒 | 統合テスト（httpbin.org使用） |

## デバッグモード

### Rider IDE でのデバッグ

1. Rider でテストファイルを開く
2. テストメソッド名の左側のアイコンをクリック
3. "Debug Tests" を選択
4. ブレークポイントを設定してステップ実行可能

### Visual Studio でのデバッグ

1. Visual Studio で Unity Debugger をアタッチ
2. テストを実行
3. ブレークポイントでアタッチ可能

## CI/CD統合

### GitHub Actions の例

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - uses: game-ci/unity-test-runner@v4
        with:
          projectPath: ludiscan-unity-api-client
          testMode: editmode
          artifactsPath: artifacts

      - uses: actions/upload-artifact@v2
        with:
          name: test-results
          path: artifacts
```

## トラブルシューティング

### テストが見つからない

**症状:** Test Runner に何もテストが表示されない

**解決方法:**
1. `LudiscanApiClient.Tests.asmdef` が存在するか確認
2. ファイル名の大文字小文字を確認（区別される）
3. Assets > Reimport All を実行
4. Editor > Clear Console をしてからリロード

### "Missing Assembly" エラー

**症状:** `Assembly reference not found`

**解決方法:**
1. asmdef の references セクションを確認
2. GUID が正しいか確認：
   ```
   Assets > [.asmdef ファイル] を右クリック > Select In Inspector
   ```
3. Reimport が必要な場合がある

### HTTPBin 統合テストがタイムアウト

**症状:** `WaitUntil timeout` エラー

**解決方法:**
1. インターネット接続を確認
2. httpbin.org が利用可能か確認：`https://httpbin.org/get`
3. タイムアウト時間を増加：
   ```csharp
   _httpClient = new UnityHttpClient(HttpBinUrl, "", timeoutSeconds: 60);
   ```
4. ファイアウォール設定を確認

### JSON デシリアライズ エラー

**症状:** `JSON parse error`

**解決方法:**
1. レスポンス型が正しいか確認
2. Newtonsoft.Json が正しくインストールされているか確認
3. キャメルケース / パスカルケースの対応を確認：
   ```csharp
   [JsonProperty("propertyName")]
   public string PropertyName { get; set; }
   ```

## パフォーマンス最適化

### テスト実行時間の短縮

1. **単体テストのみを実行**
   ```
   Test Runner > EditMode > Filter: "UnityHttpClientTests"
   ```

2. **並列実行の設定**（複数テストを同時実行）
   - Edit > Project Settings > Test Framework
   - Run Tests in Parallel: On

3. **不要なログの削除**
   - Window > General > Console
   - ログレベルを WARNING 以上に

### メモリリーク検出

テスト後にメモリがリークしていないか確認：

1. Window > Analysis > Profiler を開く
2. Memory セクションを確認
3. テスト前後のメモリ使用量を比較

## ベストプラクティス

### テスト命名規則

```
[TestClass]_[Scenario]_[ExpectedResult]

例: BuildUrl_WithQueryParameters_AddsCorrectlyFormatted
```

### テストの独立性

- 各テストは独立して実行可能であること
- `SetUp()` で初期化、`TearDown()` でクリーンアップ
- 他のテストの結果に依存しない

### アサーション

```csharp
// 推奨
Assert.AreEqual(expected, actual);
Assert.True(condition);
Assert.That(value, Does.Contain("substring"));

// 避けるべき
Assert.AreNotEqual(null, actual);  // Use Assert.NotNull instead
```

## 次のステップ

1. すべてのテストを実行して成功を確認
2. 新しい機能追加時にテストを追加
3. CI/CD パイプラインにテストを統合
4. 定期的にテスト結果を確認

## 参考資料

- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest/)
- [NUnit Documentation](https://docs.nunit.org/)
- [HTTPBin API](https://httpbin.org/)
- [Newtonsoft.Json Documentation](https://www.newtonsoft.com/json)
