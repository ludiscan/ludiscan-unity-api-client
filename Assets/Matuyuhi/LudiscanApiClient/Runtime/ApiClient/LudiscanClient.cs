using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Dto;
using LudiscanApiClient.Runtime.ApiClient.Http;
using LudiscanApiClient.Runtime.ApiClient.Model;
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
        private static LudiscanClient _instance;

        private readonly LudiscanClientConfig _config;
        private readonly UnityHttpClient _httpClient;

        /// <summary>
        /// LudiscanClientのシングルトンインスタンスを取得します
        /// Initialize()で初期化されていない場合は例外をスローします
        /// </summary>
        /// <exception cref="InvalidOperationException">クライアントが初期化されていない場合</exception>
        public static LudiscanClient Instance
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
            _instance = new LudiscanClient(config, skipCertificateValidation);
        }

        /// <summary>
        /// LudiscanClientが初期化済みかどうかを確認します
        /// </summary>
        /// <returns>初期化済みの場合はtrue、未初期化の場合はfalse</returns>
        public static bool IsInitialized => _instance != null;

        private LudiscanClient(LudiscanClientConfig config, bool skipCertificateValidation)
        {
            _config = config;
            Debug.Log($"[LudiscanClient] Constructor called with ApiBaseUrl={config.ApiBaseUrl}");
            Debug.Log($"[LudiscanClient] Timeout: {config.TimeoutSeconds} seconds");
            Debug.Log($"[LudiscanClient] SkipCertificateValidation: {skipCertificateValidation}");

            _httpClient = new UnityHttpClient(
                config.ApiBaseUrl,
                config.XapiKey,
                config.TimeoutSeconds,
                skipCertificateValidation
            );
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
                Debug.Log($"[LudiscanClient.Ping] Target URL: {_config.ApiBaseUrl}");
                Debug.Log($"[LudiscanClient.Ping] XapiKey: {_config.XapiKey}");
                Debug.Log($"[LudiscanClient.Ping] Timeout: {_config.TimeoutSeconds}s");

                var response = await _httpClient.GetStringAsync("/ping");

                Debug.Log($"[LudiscanClient.Ping] API call completed");
                Debug.Log($"[LudiscanClient.Ping] Response StatusCode: {response.StatusCode}");
                Debug.Log($"[LudiscanClient.Ping] Response Data: {response.Data}");
                Debug.Log($"[LudiscanClient.Ping] Response IsSuccess: {response.IsSuccess}");

                bool success = response.IsSuccess && response.Data == "pong";
                Debug.Log($"[LudiscanClient.Ping] Success check: {success}");
                Debug.Log($"[LudiscanClient.Ping] ===== PING END =====");

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LudiscanClient.Ping] ===== Exception =====");
                Debug.LogError($"[LudiscanClient.Ping] Type: {e.GetType().FullName}");
                Debug.LogError($"[LudiscanClient.Ping] Message: {e.Message}");
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
                Debug.Log($"[LudiscanClient.GetProjects] Making API request to {_config.ApiBaseUrl}");

                var queryParams = new Dictionary<string, string>
                {
                    { "limit", "100" },
                    { "offset", "0" }
                };

                var response = await _httpClient.GetAsync<List<ProjectResponseDto>>("/api/v0/game/projects", queryParams);

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return response.Data;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
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
                var requestDto = new CreatePlaySessionDto(name)
                {
                    AppVersion = Application.version,
                    DeviceId = SystemInfo.deviceUniqueIdentifier,
                    Platform = "Unity-" + Application.platform
                };

                var response = await _httpClient.PostJsonAsync<PlaySessionResponseDto>(
                    $"/api/v0/game/projects/{projectId}/sessions",
                    requestDto
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return response.Data;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
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
                var data = ReadStreamToBytes(stream);

                var response = await _httpClient.PostFormFileAsync<DefaultSuccessResponse>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}/player-positions",
                    data,
                    "file"
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                if (!response.Data.Success)
                {
                    throw new Exception(response.Data.Message);
                }
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
                var data = ReadStreamToBytes(stream);

                var response = await _httpClient.PostFormFileAsync<DefaultSuccessResponse>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}/field-object-logs",
                    data,
                    "file"
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                if (!response.Data.Success)
                {
                    throw new Exception(response.Data.Message);
                }

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
                var data = ReadStreamToBytes(stream);

                var response = await _httpClient.PostFormFileAsync<DefaultSuccessResponse>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}/general-events/upload",
                    data,
                    "file"
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                if (!response.Data.Success)
                {
                    throw new Exception(response.Data.Message);
                }

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
                var response = await _httpClient.PostJsonAsync<PlaySessionResponseDto>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}/finish",
                new object()
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return Session.FromDto(response.Data);
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
                var requestDto = new UpdatePlaySessionDto
                {
                    MetaData = new { mapName = mapName }
                };

                var response = await _httpClient.PutJsonAsync<PlaySessionResponseDto>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}",
                    requestDto
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return Session.FromDto(response.Data);
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
                var requestDto = new UpdatePlaySessionDto
                {
                    MetaData = new { score = score }
                };

                var response = await _httpClient.PutJsonAsync<PlaySessionResponseDto>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}",
                    requestDto
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return Session.FromDto(response.Data);
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
                var response = await _httpClient.GetAsync<HeatmapEmbedUrlResponseDto>(
                    $"/api/v0/game/projects/{projectId}/sessions/{sessionId}/heatmap-embed-url"
                );

                if (!response.IsSuccess)
                {
                    throw CreateApiException(response.StatusCode, response.ErrorMessage, response.RawContent);
                }

                return response.Data.Url;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// StreamをByte配列に変換します
        /// </summary>
        private byte[] ReadStreamToBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// APIエラー例外を生成します
        /// </summary>
        private ApiException CreateApiException(int statusCode, string errorMessage, string rawContent)
        {
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<DefaultErrorResponse>(rawContent);
                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                {
                    Debug.LogError(errorResponse.Message);
                    return new ErrorResponseException(errorResponse);
                }
            }
            catch
            {
                // JSON parse failed, use generic error
            }

            return new ApiException(statusCode, errorMessage);
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
