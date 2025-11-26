# Ludiscan Unity API Client - テストガイド

このディレクトリには、UnityHttpClient の単体テストと統合テストが含まれています。

## テスト構成

### テストの種類

1. **単体テスト** (`UnityHttpClientTests.cs`)
   - URL構築のテスト
   - クエリパラメータのエスケープテスト
   - ヘッダー設定のテスト
   - HttpResponseオブジェクトのテスト
   - 初期化パラメータの保存確認

2. **統合テスト** (`HttpClientIntegrationTestFixture.cs`)
   - 実際のHTTPリクエスト送受信テスト
   - GET/POST/PUTメソッドのテスト
   - JSON シリアライズ/デシリアライズテスト
   - エラーハンドリングテスト
   - ヘッダー送信テスト

### テストの実行方法

#### Unity Editorでの実行

1. **テストウィンドウを開く**
   - メニューから `Window > Testing > Test Runner` を選択
   - または `Ctrl+Alt+T` (Windows) / `Cmd+Shift+T` (Mac)

2. **テストスイートを実行**
   - `EditMode` タブを選択
   - `LudiscanApiClient.Tests` を展開
   - テストを選択して実行
     - `Run Selected` - 選択したテストを実行
     - `Run All` - すべてのテストを実行

3. **個別テストの実行**
   - テスト名をクリック
   - "Run" ボタンを押す

#### コマンドラインでの実行

```bash
# エディタモードのすべてのテストを実行
Unity -runTests -testPlatform editmode

# 特定のテストクラスのみを実行
Unity -runTests -testPlatform editmode -testCategory "UnityHttpClientTests"

# ビルドに失敗した場合はExitCodeで終了
Unity -runTests -testPlatform editmode -logFile -
```

## テスト内容の詳細

### URL構築テスト

- **SimpleEndpoint**: 基本的なURL構築確認
- **TrailingSlash**: ベースURLの末尾スラッシュ処理
- **QueryParameters**: クエリパラメータの正しい追加
- **SpecialCharacters**: URLエンコーディング確認
- **EmptyParams**: 空パラメータの処理

### HTTPリクエストテスト

HTTPBin (https://httpbin.org) を使用した実際のリクエストテスト:

- **GET リクエスト**
  - 正常系: ステータス200、データ取得
  - クエリパラメータ付き
  - 存在しないエンドポイント: ステータス404

- **POST リクエスト**
  - JSON形式でのデータ送信
  - バイナリデータ送信
  - レスポンスのJSON デシリアライズ

- **PUT リクエスト**
  - JSON形式でのデータ送信
  - ボディなしのリクエスト

### エラーハンドリングテスト

- **無効なJSON**: JSONパース失敗時のエラー処理
- **サーバーエラー**: ステータス500の処理
- **ネットワークエラー**: 接続失敗時の処理（オプション）

## テストの実装パターン

### 単体テストのパターン

```csharp
[Test]
public void TestName_Scenario_ExpectedResult()
{
    // Arrange - テストデータの準備
    var client = new UnityHttpClient(baseUrl, apiKey);

    // Act - テスト実行
    var result = CallMethodToTest();

    // Assert - 結果の検証
    Assert.AreEqual(expected, result);
}
```

### 統合テストのパターン

```csharp
[UnityTest]
public IEnumerator TestName_Scenario_ExpectedResult()
{
    // Arrange
    var endpoint = "/api/endpoint";
    HttpResponse<T> response = null;

    // Act
    var task = _httpClient.GetAsync<T>(endpoint);
    yield return new WaitUntil(() => task.IsCompleted);
    response = task.Result;

    // Assert
    Assert.NotNull(response);
    Assert.True(response.IsSuccess);
}
```

## 外部依存関係

### HTTPBin.org

統合テストは以下のエンドポイントを使用します:

- `GET /get` - GETリクエストのテスト
- `POST /post` - POSTリクエストのテスト
- `PUT /put` - PUTリクエストのテスト
- `GET /status/{code}` - ステータスコードのテスト（404, 500など）
- `GET /html` - JSONでないレスポンスのテスト

**注意**: インターネット接続が必要です。オフラインでテストする場合は、ローカルのモックサーバーを設定してください。

## トラブルシューティング

### テストが見つからない

- アセンブリ定義ファイル (`LudiscanApiClient.Tests.asmdef`) が正しく配置されているか確認
- `UNITY_INCLUDE_TESTS` が defineConstraints に含まれているか確認

### 統合テストが失敗する

1. **インターネット接続を確認**
   - HTTPBin.org に接続可能か確認: `ping httpbin.org`

2. **タイムアウト設定を確認**
   - テストのタイムアウトは30秒に設定されています
   - ネットワークが遅い場合は増加させてください

3. **ファイアウォール設定を確認**
   - Unityがインターネットアクセスをブロックされていないか確認

### メモリ使用量

- テスト実行後にメモリがリークしていないか Monitor で確認
- `SetUp()` で割り当てたリソースが `TearDown()` で適切に解放されているか確認

## 今後の拡張

- [ ] 実際のAPIサーバーに対するエンドツーエンドテスト
- [ ] パフォーマンステスト（レスポンスタイムの計測）
- [ ] 証明書検証スキップ機能のテスト
- [ ] タイムアウト機能のテスト
- [ ] APIキー付与の動作確認テスト
- [ ] カスタムヘッダーのテスト

## 参考資料

- [Unity Test Framework ドキュメント](https://docs.unity3d.com/Packages/com.unity.test-framework@latest/)
- [NUnit Assertion ドキュメント](https://docs.nunit.org/articles/nunit/writing-tests/assertions/assertion-models.html)
- [HTTPBin.org](https://httpbin.org/)
