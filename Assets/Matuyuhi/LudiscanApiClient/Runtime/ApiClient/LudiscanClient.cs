using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Model;
using Matuyuhi.LudiscanApi.Client.Api;
using Matuyuhi.LudiscanApi.Client.Client;
using Matuyuhi.LudiscanApi.Client.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// Ludiscan APIと通信するためのクライアントクラス
    /// シングルトンパターンで実装されており、Initialize()で初期化後、Instanceプロパティでアクセスします
    /// </summary>
    public partial class LudiscanClient
    {
        private static ApiClient.LudiscanClient _instance;

        private LudiscanClientConfig config;

        /// <summary>
        /// LudiscanClientのシングルトンインスタンスを取得します
        /// Initialize()で初期化されていない場合は例外をスローします
        /// </summary>
        /// <exception cref="InvalidOperationException">クライアントが初期化されていない場合</exception>
        public static ApiClient.LudiscanClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "LudiscanClient is not initialized. Call Initialize(LudiscanClientConfig) first."
                    );
                }
                return _instance;
            }
        }

        /// <summary>
        /// LudiscanClientを初期化します
        /// </summary>
        /// <param name="config">クライアント設定</param>
        /// <param name="skipCertificateValidation">HTTPS証明書検証をスキップする場合はtrue（開発環境用）</param>
        public static void Initialize(LudiscanClientConfig config, bool skipCertificateValidation = false)
        {
            if (_instance != null)
            {
                Debug.LogWarning("LudiscanClient is already initialized. Reinitializing...");
            }
            _instance = new ApiClient.LudiscanClient(config);
            // Windows環境でのHTTPS証明書検証エラー対応（開発環境用）
            if (skipCertificateValidation)
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
        }

        /// <summary>
        /// LudiscanClientが初期化済みかどうかを確認します
        /// </summary>
        /// <returns>初期化済みの場合はtrue、未初期化の場合はfalse</returns>
        public static bool IsInitialized => _instance != null;
        private readonly AppApi defaultApi;
        private readonly GameClientAPIApi api;

        private LudiscanClient(LudiscanClientConfig clientConfig)
        {
            this.config = clientConfig;
            var userAgent = $"Matuyuhi.LudiscanApi.UnityClient/1.0.0 (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})";
            Debug.Log($"[LudiscanClient] Constructor called with clientConfig.ApiBaseUrl={clientConfig.ApiBaseUrl}");

            // 接続設定
            Debug.Log($"[LudiscanClient] Setting DefaultConnectionLimit to 10");
            ServicePointManager.DefaultConnectionLimit = 10;

            var configuration = new Configuration {
                BasePath = clientConfig.ApiBaseUrl,
                Timeout = TimeSpan.FromSeconds(clientConfig.TimeoutSeconds),
                DefaultHeaders = new Dictionary<string, string> {
                    {
                        "Accept", "*/*"
                    }, {
                        "Content-Type", "application/json"
                    },
                },
                UserAgent = userAgent,
            };

            Debug.Log($"[LudiscanClient] Configuration.BasePath set to: {configuration.BasePath}");
            Debug.Log($"[LudiscanClient] Configuration.Timeout: {configuration.Timeout.TotalSeconds} seconds");
            Debug.Log($"[LudiscanClient] ServicePointManager.DefaultConnectionLimit: {ServicePointManager.DefaultConnectionLimit}");

            defaultApi = new AppApi(configuration);
            api = new GameClientAPIApi(configuration);
            api.ExceptionFactory = (name, response) =>
            {
                if ((int)response.StatusCode >= 400)
                {
                    try
                    {
                        var error = JsonConvert.DeserializeObject<DefaultErrorResponse>(response.RawContent);
                        Debug.Log(error.Message);
                        return new ErrorResponseException(error);
                    }
                    catch (Exception)
                    {
                        return new ApiException((int)response.StatusCode, response.ErrorText);
                    }
                }
                return null;
            };
        }

        /// <summary>
        /// Ludiscan APIへの接続をテストします
        /// </summary>
        /// <returns>接続が成功した場合はtrue、失敗した場合はfalse</returns>
        public async Task<bool> Ping()
        {
            try
            {
                Debug.Log($"[LudiscanClient.Ping] ===== PING START =====");
                Debug.Log($"[LudiscanClient.Ping] Target URL: {config.ApiBaseUrl}");
                Debug.Log($"[LudiscanClient.Ping] XapiKey: {config.XapiKey}");
                Debug.Log($"[LudiscanClient.Ping] Timeout: {config.TimeoutSeconds}s");

                var task = await defaultApi.AppControllerGetPingWithHttpInfoAsync();

                Debug.Log($"[LudiscanClient.Ping] API call completed");
                Debug.Log($"[LudiscanClient.Ping] ===== Response Object Details =====");
                Debug.Log($"[LudiscanClient.Ping] Response StatusCode: {task.StatusCode}");
                Debug.Log($"[LudiscanClient.Ping] Response Data: {task.Data}");
                Debug.Log($"[LudiscanClient.Ping] Response Data type: {task.Data?.GetType().FullName ?? "null"}");
                Debug.Log($"[LudiscanClient.Ping] Response Data == null: {task.Data == null}");
                Debug.Log($"[LudiscanClient.Ping] Response Data string.IsNullOrEmpty: {string.IsNullOrEmpty(task.Data)}");

                // レスポンスオブジェクト自体の情報
                Debug.Log($"[LudiscanClient.Ping] Response object type: {task.GetType().FullName}");
                Debug.Log($"[LudiscanClient.Ping] Response object properties:");
                foreach (var prop in task.GetType().GetProperties())
                {
                    var value = prop.GetValue(task);
                    Debug.Log($"[LudiscanClient.Ping]   {prop.Name}: {value}");
                }
                Debug.Log($"[LudiscanClient.Ping] ===== End Response Details =====");

                bool success = task.Data == "pong";
                Debug.Log($"[LudiscanClient.Ping] Success check (Data == \"pong\"): {success}");
                Debug.Log($"[LudiscanClient.Ping] ===== PING END =====");

                return success;
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"[LudiscanClient.Ping] ===== HttpRequestException (Network issue) =====");
                Debug.LogError($"[LudiscanClient.Ping] Message: {e.Message}");

                Exception innerEx = e.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 5)
                {
                    Debug.LogError($"[LudiscanClient.Ping] InnerException (depth {depth}): {innerEx.GetType().Name} - {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    depth++;
                }

                Debug.LogError($"[LudiscanClient.Ping] StackTrace: {e.StackTrace}");
                Debug.LogException(e);
                return false;
            }
            catch (ApiException e)
            {
                Debug.LogError($"[LudiscanClient.Ping] ===== ApiException =====");
                Debug.LogError($"[LudiscanClient.Ping] ErrorCode: {e.ErrorCode}");
                Debug.LogError($"[LudiscanClient.Ping] Message: {e.Message}");

                Exception innerEx = e.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 5)
                {
                    Debug.LogError($"[LudiscanClient.Ping] InnerException (depth {depth}): {innerEx.GetType().FullName} - {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    depth++;
                }

                Debug.LogError($"[LudiscanClient.Ping] StackTrace: {e.StackTrace}");
                Debug.LogException(e);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LudiscanClient.Ping] ===== Unexpected Exception =====");
                Debug.LogError($"[LudiscanClient.Ping] Type: {e.GetType().FullName}");
                Debug.LogError($"[LudiscanClient.Ping] Message: {e.Message}");

                Exception innerEx = e.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 5)
                {
                    Debug.LogError($"[LudiscanClient.Ping] InnerException (depth {depth}): {innerEx.GetType().FullName} - {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    depth++;
                }

                Debug.LogError($"[LudiscanClient.Ping] StackTrace: {e.StackTrace}");
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// アクセス可能なプロジェクト一覧を取得します
        /// </summary>
        /// <returns>プロジェクトのリスト</returns>
        /// <exception cref="ApiException">API呼び出しが失敗した場合</exception>
        public async Task<List<ProjectResponseDto>> GetProjects()
        {
            try
            {
                Debug.Log($"[LudiscanClient.GetProjects] Making API request to {config.ApiBaseUrl} with XapiKey={config.XapiKey}");
                var res = await api.GameControllerGetProjectsWithHttpInfoAsync(config.XapiKey, 100, 0);
                return res.Data;
            }
            catch (ApiException e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// 新しいプレイセッションを作成します
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="name">セッション名</param>
        /// <returns>作成されたセッション情報</returns>
        /// <exception cref="ApiException">API呼び出しが失敗した場合</exception>
        public async Task<PlaySessionResponseDto> CreateSession(int projectId, string name)
        {
            try
            {
                CreatePlaySessionDto createPlaySessionDto = new CreatePlaySessionDto(name) {
                    AppVersion = Application.version,
                    DeviceId = SystemInfo.deviceUniqueIdentifier,
                    Platform = "Unity-" + Application.platform
                };
                var res = await api.GameControllerCreateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, createPlaySessionDto
                );
                return res.Data;
            } catch (ApiException e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// 新しいプレイセッションを作成します
        /// </summary>
        /// <param name="project">セッションを作成するプロジェクト</param>
        /// <param name="name">セッション名</param>
        /// <returns>作成されたセッション情報</returns>
        /// <exception cref="ApiException">API呼び出しが失敗した場合</exception>
        public async Task<PlaySessionResponseDto> CreateSession(IProject project, string name)
        {
            return await CreateSession(project.Id, name);
        }

        /// <summary>
        /// プレイヤーの位置情報をアップロードします
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <param name="position">アップロードする位置情報の配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadPosition(int projectId, int sessionId, PositionEntry[] position)
        {
            try
            {
                var stream = CreatePositionStream(position);
                var res = await api.GameControllerUploadPlayerPositionsWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// プレイヤーの位置情報をアップロードします
        /// </summary>
        /// <param name="session">対象のセッション</param>
        /// <param name="position">アップロードする位置情報の配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadPosition(Session session, PositionEntry[] position)
        {
            await UploadPosition(session.ProjectId, session.SessionId, position);
        }

        /// <summary>
        /// フィールドオブジェクト（アイテム、敵など）のログをアップロードします
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <param name="entries">アップロードするフィールドオブジェクトログの配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadFieldObjectLogs(int projectId, int sessionId, FieldObjectEntity[] entries)
        {
            try
            {
                if (entries == null || entries.Length == 0)
                {
                    Debug.LogWarning("No field object logs to upload");
                    return;
                }

                var stream = CreateFieldObjectStream(entries);
                var res = await api.GameControllerUploadFieldObjectLogsWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);

                Debug.Log($"Uploaded {entries.Length} field object logs successfully");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// フィールドオブジェクト（アイテム、敵など）のログをアップロードします
        /// </summary>
        /// <param name="session">対象のセッション</param>
        /// <param name="entries">アップロードするフィールドオブジェクトログの配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadFieldObjectLogs(Session session, FieldObjectEntity[] entries)
        {
            await UploadFieldObjectLogs(session.ProjectId, session.SessionId, entries);
        }

        /// <summary>
        /// 一般イベントログをアップロードします
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <param name="entries">アップロードする一般イベントログの配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadGeneralEventLogs(int projectId, int sessionId, GeneralEventEntity[] entries)
        {
            try
            {
                if (entries == null || entries.Length == 0)
                {
                    Debug.LogWarning("No general event logs to upload");
                    return;
                }

                var stream = CreateGeneralEventStream(entries);
                // Use the general log endpoint to upload batch of events
                // Note: This uses a bulk upload approach similar to field objects
                var res = await api.GameControllerUploadGeneralEventLogsWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);

                Debug.Log($"Uploaded {entries.Length} general event logs successfully");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// 一般イベントログをアップロードします
        /// </summary>
        /// <param name="session">対象のセッション</param>
        /// <param name="entries">アップロードする一般イベントログの配列</param>
        /// <exception cref="Exception">アップロードが失敗した場合</exception>
        public async Task UploadGeneralEventLogs(Session session, GeneralEventEntity[] entries)
        {
            await UploadGeneralEventLogs(session.ProjectId, session.SessionId, entries);
        }

        /// <summary>
        /// セッションを終了します
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">セッション終了が失敗した場合</exception>
        public async Task<Session> FinishSession(int projectId, int sessionId)
        {
            try
            {
                var res = await api.GameControllerFinishSessionWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// セッションを終了します
        /// </summary>
        /// <param name="session">終了するセッション</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">セッション終了が失敗した場合</exception>
        public async Task<Session> FinishSession(Session session)
        {
            return await FinishSession(session.ProjectId, session.SessionId);
        }

        /// <summary>
        /// セッションのマップ名を更新します
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <param name="mapName">設定するマップ名</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">更新が失敗した場合</exception>
        public async Task<Session> PutMapName(int projectId, int sessionId, string mapName)
        {
            try
            {
                var req = new UpdatePlaySessionDto();
                req.MetaData = new { mapName = mapName };
                var res = await api.GameControllerUpdateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId, req
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// セッションのマップ名を更新します
        /// </summary>
        /// <param name="session">対象のセッション</param>
        /// <param name="mapName">設定するマップ名</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">更新が失敗した場合</exception>
        public async Task<Session> PutMapName(Session session, string mapName)
        {
            return await PutMapName(session.ProjectId, session.SessionId, mapName);
        }

        /// <summary>
        /// セッションのスコアを更新します
        /// </summary>
        /// <param name="projectId">プロジェクトID</param>
        /// <param name="sessionId">セッションID</param>
        /// <param name="score">設定するスコア</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">更新が失敗した場合</exception>
        public async Task<Session> PutScore(int projectId, int sessionId, int score)
        {
            try
            {
                var req = new UpdatePlaySessionDto();
                req.MetaData = new { score = score };
                var res = await api.GameControllerUpdateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId, req
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// セッションのスコアを更新します
        /// </summary>
        /// <param name="session">対象のセッション</param>
        /// <param name="score">設定するスコア</param>
        /// <returns>更新されたセッション情報</returns>
        /// <exception cref="Exception">更新が失敗した場合</exception>
        public async Task<Session> PutScore(Session session, int score)
        {
            return await PutScore(session.ProjectId, session.SessionId, score);
        }

        /// <summary>
        /// 指定したプロジェクトIDとセッションIDに対応するヒートマップページのURLを取得します。
        /// </summary>
        /// <param name="projectId">プロジェクトの一意の識別子</param>
        /// <param name="sessionId">セッションの一意の識別子</param>
        /// <returns>ヒートマップページの埋め込み用URL</returns>
        public async Task<string> GetHeatmapPageUrl(int projectId, int sessionId)
        {
            try
            {
                var res = await api.GameControllerGetHeatmapEmbedUrlWithHttpInfoAsync(
                    config.XapiKey,
                    projectId, sessionId);
                return res.Data.Url;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// 一般イベントのログタイプ
        /// PascalCase形式で定義されており、内部的にsnake_caseに変換されます
        /// </summary>
        public enum GeneralLogType
        {
            /// <summary>プレイヤーが敵を捕まえた</summary>
            PlayerCatch,
            /// <summary>手を変えるアイテムを取得した</summary>
            GetHandChangeItem,
            /// <summary>ダッシュアイテムを取得した</summary>
            GetDashItem,
            /// <summary>ダッシュアイテムを使用した</summary>
            UseDashItem,
            /// <summary>プレイヤーが死亡した</summary>
            Death,
            /// <summary>プレイヤーがゴールに到達した</summary>
            Success,
            /// <summary>プレイヤーがスポーン/リスポーンした</summary>
            PlayerSpawn,
            /// <summary>プレイヤーの手が選択された</summary>
            HandSelected,
            /// <summary>ゲームフェーズが変更された</summary>
            GamePhaseChanged,
            /// <summary>敵との衝突を試みた</summary>
            CollisionAttempt,
            /// <summary>スコアマイルストーンに到達した</summary>
            ScoreMilestone,
            /// <summary>プレイヤーの手が変更された</summary>
            HandChanged,
        }

        /// <summary>
        /// Converts PascalCase enum value to snake_case string for API compatibility.
        /// Example: PlayerSpawn -> player_spawn
        /// </summary>
        /// <param name="pascalCase">The PascalCase string to convert</param>
        /// <returns>The snake_case version of the input</returns>
        private string ConvertEnumToSnakeCase(string pascalCase)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < pascalCase.Length; i++)
            {
                char c = pascalCase[i];
                if (i > 0 && char.IsUpper(c))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(c));
            }
            return result.ToString();
        }
    }
}