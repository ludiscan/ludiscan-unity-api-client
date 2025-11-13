# Ludiscan API Client - Examples

このディレクトリには、Ludiscan API Clientの使い方を示すサンプルスクリプトが含まれています。

## サンプルスクリプト一覧

### 1. LudiscanBasicExample.cs

Ludiscan API Clientの基本的な使い方を示すサンプルです。

**主な機能:**
- クライアントの初期化
- API接続テスト (Ping)
- プロジェクトの取得
- セッションの作成と管理
- マップ名の設定
- スコアの更新
- セッションの終了

**使い方:**
1. 空のGameObjectを作成
2. `LudiscanBasicExample` スクリプトをアタッチ
3. Inspector で `API Base URL` と `API Key` を設定
4. 再生して動作を確認

### 2. PositionLoggerExample.cs

プレイヤーの位置を定期的に記録してアップロードするサンプルです。

**主な機能:**
- PositionLoggerの初期化と設定
- 定期的な位置情報の記録 (デフォルト: 250ms間隔)
- バッファへの蓄積
- 定期的なアップロード (デフォルト: 10秒間隔)

**使い方:**
1. プレイヤーのGameObjectに `PositionLoggerExample` スクリプトをアタッチ
2. Inspector で以下を設定:
   - `Player Transform`: プレイヤーのTransform
   - `Player Id`: プレイヤーID (マルチプレイヤーの場合)
   - `Record Interval Milliseconds`: 記録間隔 (ミリ秒)
   - `Upload Interval Seconds`: アップロード間隔 (秒)
3. セッションを開始してから使用

**Context Menu:**
- `Upload Position Data Now`: 手動でデータをアップロード
- `Toggle Logging`: ロギングの開始/停止を切り替え

### 3. FieldObjectLoggerExample.cs

アイテム、敵などのフィールドオブジェクトのイベントを記録するサンプルです。

**主な機能:**
- アイテムの出現/消滅の記録
- 敵の出現/移動/倒された記録
- カスタムオブジェクトの状態変更の記録
- バッファへの蓄積とアップロード

**使い方:**
1. GameManagerなどの管理用オブジェクトに `FieldObjectLoggerExample` スクリプトをアタッチ
2. ゲーム内のイベント発生時に対応するメソッドを呼び出し:
   - `OnItemSpawned()`: アイテムが出現したとき
   - `OnItemPickedUp()`: アイテムが取得されたとき
   - `OnEnemySpawned()`: 敵が出現したとき
   - `OnEnemyMoved()`: 敵が移動したとき
   - `OnEnemyDefeated()`: 敵が倒されたとき
3. セッション終了時に `UploadAndClearLogs()` を呼び出してアップロード

**Context Menu:**
- `Upload Field Object Logs`: 蓄積されたログをアップロード
- `Test: Spawn Item`: テストアイテムを記録
- `Test: Spawn Enemy`: テスト敵を記録

### 4. GeneralEventLoggerExample.cs

ゲーム内の一般的なイベントを記録するサンプルです。

**主な機能:**
- プレイヤーイベント (スポーン、死亡、リスポーン)
- ゲームプレイイベント (フェーズ変更、スコアマイルストーン、ゴール到達)
- アイテム/アビリティイベント (アイテム取得、ダッシュ使用、手の変更)
- 衝突/戦闘イベント (衝突試行、敵を捕まえた)

**使い方:**
1. GameManagerなどの管理用オブジェクトに `GeneralEventLoggerExample` スクリプトをアタッチ
2. ゲーム内のイベント発生時に対応するメソッドを呼び出し:
   - `OnPlayerSpawned()`: プレイヤーがスポーンしたとき
   - `OnPlayerDeath()`: プレイヤーが死亡したとき
   - `OnGamePhaseChanged()`: ゲームフェーズが変更されたとき
   - `OnScoreMilestone()`: スコアマイルストーンに到達したとき
   - `OnItemCollected()`: アイテムを取得したとき
   - その他、ゲーム内のイベントに応じて
3. セッション終了時に `UploadAndClearLogs()` を呼び出してアップロード

**Context Menu:**
- `Upload General Event Logs`: 蓄積されたログをアップロード
- `Test: Log Player Spawn`: テストイベント (プレイヤースポーン) を記録
- `Test: Log Player Death`: テストイベント (プレイヤー死亡) を記録
- `Test: Log Score Milestone`: テストイベント (スコアマイルストーン) を記録

## 統合例

実際のゲームでは、これらのLoggerを組み合わせて使用します。すべてのLoggerはシングルトンパターンで実装されています:

```csharp
public class GameManager : MonoBehaviour
{
    private Session currentSession;

    private async void Start()
    {
        // 1. クライアント初期化（シングルトン）
        var config = new LudiscanClientConfig(apiBaseUrl, apiKey);
        LudiscanClient.Initialize(config);

        // 2. プロジェクト取得とセッション作成
        var projects = await LudiscanClient.Instance.GetProjects();
        var project = Project.FromDto(projects[0]);
        var sessionDto = await LudiscanClient.Instance.CreateSession(project, "My Session");
        currentSession = Session.FromDto(sessionDto);

        // 3. Logger初期化（すべてシングルトン）
        PositionLogger.Initialize(1000);
        PositionLogger.Instance.OnLogPosition = GetPlayerPositions;
        PositionLogger.Instance.StartLogging(250);

        FieldObjectLogger.Initialize(1000);
        GeneralEventLogger.Initialize(2000);
    }

    // ゲーム内イベントの例
    void OnEnemySpawned(string enemyId, Vector3 position)
    {
        // FieldObjectLoggerを使用してログ記録
        var offsetTime = GetOffsetTimestamp();
        FieldObjectLogger.Instance.LogEnemySpawn(enemyId, "goblin", position, offsetTime);
    }

    void OnPlayerDeath(int playerId, Vector3 position)
    {
        // GeneralEventLoggerを使用してログ記録
        var metadata = new { death_cause = "enemy_collision" };
        GeneralEventLogger.Instance.AddLog("death", metadata, GetOffsetTimestamp(), position, playerId);
    }

    private async void OnApplicationQuit()
    {
        // セッション終了時にログをアップロードして終了
        if (PositionLogger.IsInitialized)
        {
            await LudiscanClient.Instance.UploadPosition(currentSession, PositionLogger.Instance.Buffer);
        }
        if (FieldObjectLogger.IsInitialized)
        {
            await LudiscanClient.Instance.UploadFieldObjectLogs(currentSession, FieldObjectLogger.Instance.GetLogsAndClear());
        }
        if (GeneralEventLogger.IsInitialized)
        {
            await LudiscanClient.Instance.UploadGeneralEventLogs(currentSession, GeneralEventLogger.Instance.GetLogsAndClear());
        }
        await LudiscanClient.Instance.FinishSession(currentSession);
    }

    private List<PositionEntry> GetPlayerPositions()
    {
        // プレイヤーの位置情報を返す
        return new List<PositionEntry>
        {
            new PositionEntry { PlayerId = 0, Position = playerTransform.position }
        };
    }

    private uint GetOffsetTimestamp()
    {
        // セッション開始からの経過時間をミリ秒で返す
        // 実際の実装では、セッション開始時刻を記録しておく必要があります
        return (uint)(Time.time * 1000);
    }
}
```

### シングルトンパターンのメリット

- **グローバルアクセス**: どこからでも `Logger.Instance` でアクセス可能
- **インスタンス管理不要**: `new` でインスタンスを作成する必要がない
- **初期化チェック**: `IsInitialized` プロパティで初期化状態を確認可能
- **一貫性**: LudiscanClient と同じパターンで統一された API

## 注意事項

- サンプルスクリプトは簡略化のため、エラーハンドリングを最小限にしています
- 実際のゲームでは、適切なエラーハンドリングとリトライロジックを実装してください
- API Keyなどのクレデンシャルはコードにハードコーディングせず、ScriptableObjectや環境変数から読み込むようにしてください
- ログのアップロード頻度は、ゲームの特性やネットワーク状況に応じて調整してください

## サポート

詳細なAPIドキュメントは [Ludiscan公式ドキュメント](https://ludiscan.net/docs) を参照してください。
