using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Dto;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Examples
{
    /// <summary>
    /// Ludiscan API Clientの基本的な使い方を示すサンプルスクリプト
    /// </summary>
    public class LudiscanBasicExample : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiBaseUrl = "https://ludiscan.net/api";
        [SerializeField] private string apiKey = "your-api-key-here";

        [Header("Session Info")]
        [SerializeField] private string sessionName = "Test Session";

        private Session currentSession;
        private Project selectedProject;

        private async void Start()
        {
            // 1. クライアントの初期化
            InitializeClient();

            // 2. API接続テスト
            bool pingSuccess = await TestConnection();
            if (!pingSuccess)
            {
                Debug.LogError("Failed to connect to Ludiscan API");
                return;
            }

            // 3. プロジェクトの取得
            var projects = await GetProjects();
            if (projects == null || projects.Count == 0)
            {
                Debug.LogError("No projects found");
                return;
            }

            // 4. 最初のプロジェクトを選択
            selectedProject = Project.FromDto(projects[0]);
            Debug.Log($"Selected project: {selectedProject.Name}");

            // 5. セッションの作成
            currentSession = await CreateSession();
            if (currentSession.IsActive)
            {
                Debug.Log($"Session created: {currentSession.Name} (ID: {currentSession.SessionId})");
            }

            // 6. マップ名の設定
            await UpdateMapName("ExampleMap_01");

            // 7. スコアの更新
            await UpdateScore(100);
        }

        /// <summary>
        /// クライアントを初期化
        /// </summary>
        private void InitializeClient()
        {
            var config = new LudiscanClientConfig(apiBaseUrl, apiKey)
            {
                TimeoutSeconds = 10
            };
            LudiscanClient.Initialize(config);
            Debug.Log("LudiscanClient initialized");
        }

        /// <summary>
        /// API接続テスト
        /// </summary>
        private async Task<bool> TestConnection()
        {
            Debug.Log("Testing connection to Ludiscan API...");
            bool result = await LudiscanClient.Instance.Ping();
            Debug.Log(result ? "Connection successful" : "Connection failed");
            return result;
        }

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        private async Task<List<ProjectResponseDto>> GetProjects()
        {
            Debug.Log("Fetching projects...");
            try
            {
                var projects = await LudiscanClient.Instance.GetProjects();
                Debug.Log($"Found {projects.Count} projects");
                return projects;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get projects: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// セッションを作成
        /// </summary>
        private async Task<Session> CreateSession()
        {
            Debug.Log("Creating session...");
            try
            {
                var sessionDto = await LudiscanClient.Instance.CreateSession(selectedProject, sessionName);
                return Session.FromDto(sessionDto);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create session: {e.Message}");
                return Session.Empty;
            }
        }

        /// <summary>
        /// マップ名を更新
        /// </summary>
        private async Task UpdateMapName(string mapName)
        {
            if (!currentSession.IsActive) return;

            Debug.Log($"Updating map name to: {mapName}");
            try
            {
                currentSession = await LudiscanClient.Instance.PutMapName(currentSession, mapName);
                Debug.Log("Map name updated");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update map name: {e.Message}");
            }
        }

        /// <summary>
        /// スコアを更新
        /// </summary>
        private async Task UpdateScore(int score)
        {
            if (!currentSession.IsActive) return;

            Debug.Log($"Updating score to: {score}");
            try
            {
                currentSession = await LudiscanClient.Instance.PutScore(currentSession, score);
                Debug.Log("Score updated");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update score: {e.Message}");
            }
        }

        /// <summary>
        /// セッションを終了
        /// </summary>
        private async Task FinishSession()
        {
            if (!currentSession.IsActive) return;

            Debug.Log("Finishing session...");
            try
            {
                currentSession = await LudiscanClient.Instance.FinishSession(currentSession);
                Debug.Log("Session finished");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to finish session: {e.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            // アプリケーション終了時にセッションを終了
            await FinishSession();
        }
    }
}
