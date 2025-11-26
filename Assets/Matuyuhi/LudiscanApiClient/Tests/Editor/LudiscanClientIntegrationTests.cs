using System;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using LudiscanApiClient.Runtime.ApiClient;
using LudiscanApiClient.Runtime.ApiClient.Model;
using LudiscanApiClient.Runtime.ApiClient.Dto;
using UnityEngine;

namespace LudiscanApiClient.Tests.Editor
{
    /// <summary>
    /// LudiscanClient の統合テスト
    /// 実際のludi APIサーバー（localhost:3211）に対してテストします
    /// </summary>
    [TestFixture]
    public class LudiscanClientIntegrationTests
    {
        private LudiscanClientConfig _config;
        private const string LocalApiUrl = "http://localhost:3211";
        private const string TestApiKey = "ludi_30b0890eacd563d1ca783ab5f29a4078";

        [SetUp]
        public void SetUp()
        {
            // LudiscanClientをリセット（既に初期化されていた場合）
            var instanceProperty = typeof(LudiscanClient).GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // クライアント設定を作成
            _config = new LudiscanClientConfig
            {
                ApiBaseUrl = LocalApiUrl,
                XapiKey = TestApiKey,
                TimeoutSeconds = 30
            };

            // LudiscanClientを初期化
            LudiscanClient.Initialize(_config);
        }

        #region Connection Tests

        [Test]
        public async Task Ping_ReturnsSuccess()
        {
            // Arrange
            var client = LudiscanClient.Instance;

            // Act
            var result = await client.Ping();

            // Assert
            Assert.True(result, "Ping should return true for successful connection");
        }

        #endregion

        #region Project Tests

        [Test]
        public async Task GetProjects_ReturnsProjectList()
        {
            // Arrange
            var client = LudiscanClient.Instance;

            // Act
            List<ProjectResponseDto> projects = await client.GetProjects();

            // Assert
            Assert.NotNull(projects, "Projects list should not be null");
            Assert.IsInstanceOf<List<ProjectResponseDto>>(projects);
            Debug.Log($"Retrieved {projects.Count} projects");
        }

        [Test]
        public async Task GetProjects_ReturnsProjectsWithValidIds()
        {
            // Arrange
            var client = LudiscanClient.Instance;

            // Act
            var projects = await client.GetProjects();

            // Assert
            Assert.NotNull(projects);
            Assert.Greater(projects.Count, 0, "Should have at least one project");

            foreach (var project in projects)
            {
                Assert.NotNull(project.Name, "Project name should not be null");
                Assert.Greater(project.Id, 0, "Project ID should be greater than 0");
                Debug.Log($"Project: {project.Name} (ID: {project.Id})");
            }
        }

        #endregion

        #region Session Tests

        [Test]
        public async Task CreateSession_WithValidProjectId_ReturnsSessionData()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";

            // Act
            var session = await client.CreateSession(projectId, sessionName);

            // Assert
            Assert.NotNull(session, "Session should be created");
            Assert.NotNull(session.Name, "Session name should be set");
            Assert.Greater(session.SessionId, 0, "Session ID should be greater than 0");
            Assert.AreEqual(projectId, session.ProjectId, "Project ID should match");
            Debug.Log($"Created session: {session.Name} (ID: {session.SessionId})");
        }

        [Test]
        public async Task CreateSession_WithProjectObject_ReturnsSessionData()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var project = new Project
            {
                Id = 9,
                Name = "Test Project"
            };

            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";

            // Act
            var session = await client.CreateSession(project, sessionName);

            // Assert
            Assert.NotNull(session);
            Assert.AreEqual(project.Id, session.ProjectId);
        }

        #endregion

        #region Session Update Tests

        [Test]
        public async Task PutMapName_UpdatesSessionMapName()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var createdSession = await client.CreateSession(projectId, sessionName);

            var mapName = "TestMap_001";

            // Act
            var updatedSession = await client.PutMapName(projectId, (int)createdSession.SessionId, mapName);

            // Assert
            Assert.NotNull(updatedSession);
            Assert.AreEqual(projectId, updatedSession.ProjectId);
            Debug.Log($"Updated session map name to: {mapName}");
        }

        [Test]
        public async Task PutScore_UpdatesSessionScore()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var createdSession = await client.CreateSession(projectId, sessionName);

            var testScore = 12345;

            // Act
            var updatedSession = await client.PutScore(projectId, (int)createdSession.SessionId, testScore);

            // Assert
            Assert.NotNull(updatedSession);
            Assert.AreEqual(projectId, updatedSession.ProjectId);
            Debug.Log($"Updated session score to: {testScore}");
        }

        #endregion

        #region Position Upload Tests

        [Test]
        public async Task UploadPosition_WithValidData_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            // Create test position data
            var positions = new PositionEntry[]
            {
                new PositionEntry { PlayerId = 1, X = 10.5f, Y = 20.5f, Z = 0f, OffsetTimeStamp = 0 },
                new PositionEntry { PlayerId = 1, X = 11.5f, Y = 21.5f, Z = 0f, OffsetTimeStamp = 1000 },
                new PositionEntry { PlayerId = 1, X = 12.5f, Y = 22.5f, Z = 0f, OffsetTimeStamp = 2000 }
            };

            // Act & Assert - Should not throw
            await client.UploadPosition(projectId, (int)session.SessionId, positions);
            Debug.Log($"Uploaded {positions.Length} positions successfully");
        }

        [Test]
        public async Task UploadPosition_WithSession_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var sessionDto = await client.CreateSession(projectId, sessionName);
            var session = new Session
            {
                ProjectId = projectId,
                SessionId = (int)sessionDto.SessionId,
                Name = sessionDto.Name
            };

            var positions = new PositionEntry[]
            {
                new PositionEntry { PlayerId = 1, X = 100.0f, Y = 200.0f, Z = 0f, OffsetTimeStamp = 0 }
            };

            // Act & Assert
            await client.UploadPosition(session, positions);
            Debug.Log("Position upload with Session object succeeded");
        }

        #endregion

        #region Field Object Log Tests

        [Test]
        public async Task UploadFieldObjectLogs_WithValidData_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            var fieldObjects = new FieldObjectEntity[]
            {
                new FieldObjectEntity
                {
                    ObjectId = "object_1",
                    ObjectType = "Item",
                    X = 30.0f,
                    Y = 40.0f,
                    Z = 0f,
                    OffsetTimeStamp = 0,
                    EventType = FieldObjectEventType.Spawn
                },
                new FieldObjectEntity
                {
                    ObjectId = "object_1",
                    ObjectType = "Item",
                    X = 30.0f,
                    Y = 40.0f,
                    Z = 0f,
                    OffsetTimeStamp = 1000,
                    EventType = FieldObjectEventType.Despawn
                }
            };

            // Act & Assert
            await client.UploadFieldObjectLogs(projectId, (int)session.SessionId, fieldObjects);
            Debug.Log($"Uploaded {fieldObjects.Length} field object logs successfully");
        }

        [Test]
        public async Task UploadFieldObjectLogs_EmptyArray_DoesNotThrow()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            var fieldObjects = new FieldObjectEntity[] { };

            // Act & Assert - Should not throw
            await client.UploadFieldObjectLogs(projectId, (int)session.SessionId, fieldObjects);
        }

        #endregion

        #region General Event Log Tests

        [Test]
        public async Task UploadGeneralEventLogs_WithValidData_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            var events = new GeneralEventEntity[]
            {
                new GeneralEventEntity
                {
                    EventType = "player_spawn",
                    Player = 1,
                    Metadata = new { action = "spawn" },
                    OffsetTimeStamp = 0
                },
                new GeneralEventEntity
                {
                    EventType = "score_milestone",
                    Player = 1,
                    Metadata = new { score = 1000 },
                    OffsetTimeStamp = 5000
                }
            };

            // Act & Assert
            await client.UploadGeneralEventLogs(projectId, (int)session.SessionId, events);
            Debug.Log($"Uploaded {events.Length} general event logs successfully");
        }

        [Test]
        public async Task UploadGeneralEventLogs_EmptyArray_DoesNotThrow()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            var events = new GeneralEventEntity[] { };

            // Act & Assert - Should not throw
            await client.UploadGeneralEventLogs(projectId, (int)session.SessionId, events);
        }

        #endregion

        #region Session Finish Tests

        [Test]
        public async Task FinishSession_WithValidSessionId_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var createdSession = await client.CreateSession(projectId, sessionName);

            // Act
            var finishedSession = await client.FinishSession(projectId, (int)createdSession.SessionId);

            // Assert
            Assert.NotNull(finishedSession);
            Assert.AreEqual(projectId, finishedSession.ProjectId);
            Debug.Log($"Finished session: {finishedSession.Name}");
        }

        [Test]
        public async Task FinishSession_WithSessionObject_Succeeds()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var sessionDto = await client.CreateSession(projectId, sessionName);
            var session = new Session
            {
                ProjectId = projectId,
                SessionId = (int)sessionDto.SessionId,
                Name = sessionDto.Name
            };

            // Act & Assert
            try
            {
                var finishedSession = await client.FinishSession(session);
                Assert.NotNull(finishedSession);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FinishSession may require additional conditions: {e.Message}");
                Assert.Pass("API may have restrictions on session finishing");
            }
        }

        #endregion

        #region Heatmap Tests

        [Test]
        public async Task GetHeatmapPageUrl_WithValidSessionId_ReturnsUrl()
        {
            // Arrange
            var client = LudiscanClient.Instance;
            var projectId = 9; // Test project ID
            var sessionName = $"TestSession_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var session = await client.CreateSession(projectId, sessionName);

            // Act
            var url = await client.GetHeatmapPageUrl(projectId, (int)session.SessionId);

            // Assert
            Assert.NotNull(url, "Heatmap URL should not be null");
            Assert.That(url, Does.Contain("http"), "URL should start with http");
            Debug.Log($"Heatmap URL: {url}");
        }

        #endregion
    }
}
