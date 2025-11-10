using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Model;
using Matuyuhi.LudiscanApi.Client.Api;
using Matuyuhi.LudiscanApi.Client.Client;
using Matuyuhi.LudiscanApi.Client.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    public partial class LudiscanClient
    {
        private static ApiClient.LudiscanClient _instance;

        private LudiscanClientConfig config;

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
        /// <param name="_config">クライアント設定</param>
        public static void Initialize(LudiscanClientConfig _config)
        {
            if (_instance != null)
            {
                Debug.LogWarning("LudiscanClient is already initialized. Reinitializing...");
            }
            _instance = new ApiClient.LudiscanClient(_config);
        }

        /// <summary>
        /// 初期化済みかどうかを確認します
        /// </summary>
        public static bool IsInitialized => _instance != null;
        private readonly AppApi defaultApi;
        private readonly GameClientAPIApi api;

        private LudiscanClient(LudiscanClientConfig _clientConfig)
        {
            this.config = _clientConfig;
            var userAgent = $"Matuyuhi.LudiscanApi.UnityClient/1.0.0 (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})";
            var configuration = new Configuration {
                BasePath = _clientConfig.ApiBaseUrl,
                Timeout = TimeSpan.FromSeconds(_clientConfig.TimeoutSeconds),
                DefaultHeaders = new Dictionary<string, string> {
                    {
                        "Accept", "*/*"
                    }, {
                        "Content-Type", "application/json"
                    },
                },
                UserAgent = userAgent,
            };
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

        public async Task<bool> Ping()
        {
            try
            {
                var task = await defaultApi.AppControllerGetPingWithHttpInfoAsync();
                return task.Data == "pong";
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public async Task<List<ProjectResponseDto>> GetProjects()
        {
            try
            {
                var res = await api.GameControllerGetProjectsWithHttpInfoAsync(config.XapiKey, 100, 0);
                return res.Data;
            }
            catch (ApiException e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task<PlaySessionResponseDto> CreateSession(IProject _project, string _name)
        {
            try
            {
                CreatePlaySessionDto createPlaySessionDto = new CreatePlaySessionDto(_name) {
                    AppVersion = Application.version,
                    DeviceId = SystemInfo.deviceUniqueIdentifier,
                    Platform = "Unity-" + Application.platform
                };
                var res = await api.GameControllerCreateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    _project.Id, createPlaySessionDto
                );
                return res.Data;
            } catch (ApiException e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task UploadPosition(Session _session, PositionEntry[] _position)
        {
            try
            {
                var stream = CreatePositionStream(_position);
                var res = await api.GameControllerUploadPlayerPositionsWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task UploadFieldObjectLogs(Session _session, FieldObjectEntity[] _entries)
        {
            try
            {
                if (_entries == null || _entries.Length == 0)
                {
                    Debug.LogWarning("No field object logs to upload");
                    return;
                }

                var stream = CreateFieldObjectStream(_entries);
                var res = await api.GameControllerUploadFieldObjectLogsWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);

                Debug.Log($"Uploaded {_entries.Length} field object logs successfully");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task UploadGeneralEventLogs(Session _session, GeneralEventEntity[] _entries)
        {
            try
            {
                if (_entries == null || _entries.Length == 0)
                {
                    Debug.LogWarning("No general event logs to upload");
                    return;
                }

                var stream = CreateGeneralEventStream(_entries);
                // Use the general log endpoint to upload batch of events
                // Note: This uses a bulk upload approach similar to field objects
                var res = await api.GameControllerUploadGeneralEventLogsWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId, stream
                );
                if (!res.Data.Success) throw new Exception(res.Data.Message);

                Debug.Log($"Uploaded {_entries.Length} general event logs successfully");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task<Session> FinishSession(Session _session)
        {
            try
            {
                var res = await api.GameControllerFinishSessionWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task<Session> PutMapName(Session _session, string _mapName)
        {
            try
            {
                var req = new UpdatePlaySessionDto();
                req.MetaData = new { mapName = _mapName };
                var res = await api.GameControllerUpdateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId, req
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public async Task<Session> PutScore(Session _session, int _score)
        {
            try
            {
                var req = new UpdatePlaySessionDto();
                req.MetaData = new { score = _score };
                var res = await api.GameControllerUpdateSessionWithHttpInfoAsync(
                    config.XapiKey,
                    _session.ProjectId, _session.SessionId, req
                );
                return Session.FromDto(res.Data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public enum GeneralLogType
        {
            PlayerCatch,
            GetHandChangeItem,
            GetDashItem,
            UseDashItem,
            Death,
            Success,
            PlayerSpawn,      // NEW: Tier 1 - Player spawn/respawn event
            HandSelected,     // NEW: Tier 1 - Player hand selection
            GamePhaseChanged, // NEW: Tier 1 - Game state transition
            CollisionAttempt, // NEW: Tier 2 - Enemy collision event
            ScoreMilestone,   // NEW: Tier 2 - Score change event
            HandChanged,      // NEW: Tier 2 - Hand change event
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