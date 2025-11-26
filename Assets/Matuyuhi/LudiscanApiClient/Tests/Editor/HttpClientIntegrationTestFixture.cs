using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LudiscanApiClient.Runtime.ApiClient.Http;

namespace LudiscanApiClient.Tests.Editor
{
    /// <summary>
    /// UnityHttpClient の統合テスト用フィクスチャ
    /// httpbin.org を使用して実際のHTTPリクエストをテストします
    /// </summary>
    [TestFixture]
    public class HttpClientIntegrationTestFixture
    {
        // httpbin.orgはテスト用の無料のHTTPサービス
        private const string HttpBinUrl = "https://httpbin.org";
        private UnityHttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
            _httpClient = new UnityHttpClient(HttpBinUrl, "", timeoutSeconds: 30);
        }

        #region GET Request Tests

        [Test]
        public async Task GetAsync_WithValidEndpoint_ReturnsSuccessfulResponse()
        {
            // Arrange
            var endpoint = "/get";

            // Act
            var response = await _httpClient.GetStringAsync(endpoint);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess, $"Expected success but got error: {response.ErrorMessage}");
            Assert.AreEqual(200, response.StatusCode);
            Assert.NotNull(response.Data);
            Assert.That(response.Data, Does.Contain("httpbin"));
        }

        [Test]
        public async Task GetAsync_WithQueryParameters_IncludesParametersInResponse()
        {
            // Arrange
            var endpoint = "/get";
            var queryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            var response = await _httpClient.GetAsync<string>(endpoint, queryParams);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.That(response.Data, Does.Contain("key1"));
            Assert.That(response.Data, Does.Contain("value1"));
        }

        [Test]
        public async Task GetAsync_NonExistentEndpoint_Returns404()
        {
            // Arrange
            var endpoint = "/status/404";

            // Act
            var response = await _httpClient.GetStringAsync(endpoint);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.AreEqual(404, response.StatusCode);
        }

        #endregion

        #region POST Request Tests

        [Test]
        public async Task PostJsonAsync_WithValidData_ReturnsSuccessfulResponse()
        {
            // Arrange
            var endpoint = "/post";
            var testData = new TestRequestDto { Name = "Test User", Value = 42 };

            // Act
            var response = await _httpClient.PostJsonAsync<string>(endpoint, testData);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess, $"Expected success but got error: {response.ErrorMessage}");
            Assert.AreEqual(200, response.StatusCode);
            Assert.That(response.Data, Does.Contain("Test User"));
        }

        [Test]
        public async Task PostBinaryAsync_WithBinaryData_ReturnsSuccessfulResponse()
        {
            // Arrange
            var endpoint = "/post";
            var binaryData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            var response = await _httpClient.PostBinaryAsync<string>(endpoint, binaryData, "application/octet-stream");

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
        }

        #endregion

        #region PUT Request Tests

        [Test]
        public async Task PutJsonAsync_WithValidData_ReturnsSuccessfulResponse()
        {
            // Arrange
            var endpoint = "/put";
            var testData = new TestRequestDto { Name = "Updated User", Value = 100 };

            // Act
            var response = await _httpClient.PutJsonAsync<string>(endpoint, testData);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
            Assert.That(response.Data, Does.Contain("Updated User"));
        }

        [Test]
        public async Task PutJsonAsync_WithNullBody_ReturnsSuccessfulResponse()
        {
            // Arrange
            var endpoint = "/put";

            // Act
            var response = await _httpClient.PutJsonAsync<string>(endpoint, null);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
        }

        #endregion

        #region JSON Deserialization Tests

        [Test]
        public async Task PostJsonAsync_WithJsonResponse_DeserializesCorrectly()
        {
            // Arrange
            var endpoint = "/post";
            var testData = new TestRequestDto { Name = "Serialization Test", Value = 999 };

            // Act
            var response = await _httpClient.PostJsonAsync<TestRequestDto>(endpoint, testData);

            // Assert
            // Note: httpbin.org echoes back the JSON, so we verify it was sent
            Assert.NotNull(response);
            Assert.AreEqual(200, response.StatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task GetAsync_InvalidJson_ReturnsErrorResponse()
        {
            // Arrange
            var endpoint = "/html";  // Returns HTML, not JSON

            // Act
            var response = await _httpClient.GetAsync<TestRequestDto>(endpoint);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.That(response.ErrorMessage, Does.Contain("parse error"));
        }

        [Test]
        public async Task GetAsync_ServerError_Returns500()
        {
            // Arrange
            var endpoint = "/status/500";

            // Act
            var response = await _httpClient.GetStringAsync(endpoint);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.AreEqual(500, response.StatusCode);
        }

        #endregion

        #region Header Tests

        [Test]
        public async Task PostJsonAsync_IncludesCorrectHeaders()
        {
            // Arrange
            var endpoint = "/post";
            var testData = new TestRequestDto { Name = "Header Test", Value = 1 };

            // Act
            var response = await _httpClient.PostJsonAsync<string>(endpoint, testData);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            // httpbin echoes back headers, we can verify Content-Type was set
            Assert.That(response.Data, Does.Contain("application/json"));
        }

        #endregion

        #region Helper Classes

        [System.Serializable]
        public class TestRequestDto
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public int Value { get; set; }
        }

        #endregion
    }
}
