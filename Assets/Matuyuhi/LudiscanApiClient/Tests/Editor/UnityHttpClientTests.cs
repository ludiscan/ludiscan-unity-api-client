using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using LudiscanApiClient.Runtime.ApiClient.Http;

namespace LudiscanApiClient.Tests.Editor
{
    /// <summary>
    /// UnityHttpClient の単体テスト
    /// </summary>
    [TestFixture]
    public class UnityHttpClientTests
    {
        private UnityHttpClient _httpClient;
        private const string TestBaseUrl = "http://localhost:3211";
        private const string TestApiKey = "ludi_30b0890eacd563d1ca783ab5f29a4078";

        [SetUp]
        public void SetUp()
        {
            // テスト用のHTTPクライアントを初期化
            _httpClient = new UnityHttpClient(TestBaseUrl, TestApiKey, timeoutSeconds: 10, skipCertificateValidation: false);
        }

        #region URL Building Tests

        [Test]
        public void BuildUrl_SimpleEndpoint_ReturnsCorrectUrl()
        {
            // Arrange
            var endpoint = "/api/v0/projects";
            var expectedUrl = "http://localhost:3211/api/v0/projects";

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, null);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }

        [Test]
        public void BuildUrl_WithTrailingSlashBaseUrl_RemovesTrailingSlash()
        {
            // Arrange
            var clientWithTrailingSlash = new UnityHttpClient(TestBaseUrl + "/", TestApiKey);
            var endpoint = "/api/v0/projects";
            var expectedUrl = "http://localhost:3211/api/v0/projects";

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, null, clientWithTrailingSlash);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }

        [Test]
        public void BuildUrl_WithQueryParameters_AddsCorrectlyFormatted()
        {
            // Arrange
            var endpoint = "/api/v0/projects";
            var queryParams = new Dictionary<string, string>
            {
                { "limit", "10" },
                { "offset", "0" }
            };
            var expectedUrlPattern = "http://localhost:3211/api/v0/projects?";

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, queryParams);

            // Assert
            Assert.That(actualUrl, Does.StartWith(expectedUrlPattern));
            Assert.That(actualUrl, Does.Contain("limit=10"));
            Assert.That(actualUrl, Does.Contain("offset=0"));
            Assert.That(actualUrl, Does.Contain("&"));
        }

        [Test]
        public void BuildUrl_WithSpecialCharactersInQueryParams_UrlEncodesCorrectly()
        {
            // Arrange
            var endpoint = "/api/v0/search";
            var queryParams = new Dictionary<string, string>
            {
                { "q", "hello world" },
                { "category", "games & sports" }
            };

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, queryParams);

            // Assert
            // UnityWebRequest.EscapeURL() uses + for spaces (application/x-www-form-urlencoded style)
            Assert.That(actualUrl, Does.Contain("hello+world"));
            Assert.That(actualUrl, Does.Contain("games+%26+sports"));
            // Verify ampersand is properly encoded
            Assert.That(actualUrl, Does.Contain("%26"));
        }

        [Test]
        public void BuildUrl_EmptyQueryParams_DoesNotAddQueryString()
        {
            // Arrange
            var endpoint = "/api/v0/projects";
            var emptyParams = new Dictionary<string, string>();
            var expectedUrl = "http://localhost:3211/api/v0/projects";

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, emptyParams);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }

        [Test]
        public void BuildUrl_NullQueryParams_DoesNotAddQueryString()
        {
            // Arrange
            var endpoint = "/api/v0/projects";
            var expectedUrl = "http://localhost:3211/api/v0/projects";

            // Act
            var actualUrl = CallBuildUrlPrivateMethod(endpoint, null);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }

        #endregion

        #region Initialization Tests

        [Test]
        public void Constructor_WithApiKey_StoresApiKey()
        {
            // Arrange & Act
            var client = new UnityHttpClient(TestBaseUrl, TestApiKey);

            // Assert - We verify by attempting to get private field
            var apiKeyField = typeof(UnityHttpClient).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(apiKeyField);
            var storedApiKey = apiKeyField?.GetValue(client) as string;
            Assert.AreEqual(TestApiKey, storedApiKey);
        }

        [Test]
        public void Constructor_WithoutApiKey_AllowsEmptyApiKey()
        {
            // Arrange & Act
            var client = new UnityHttpClient(TestBaseUrl, "");

            // Assert
            var apiKeyField = typeof(UnityHttpClient).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
            var storedApiKey = apiKeyField?.GetValue(client) as string;
            Assert.AreEqual("", storedApiKey);
        }

        [Test]
        public void Constructor_WithTimeoutParameter_StoresTimeout()
        {
            // Arrange
            var customTimeout = 30;

            // Act
            var client = new UnityHttpClient(TestBaseUrl, TestApiKey, customTimeout);

            // Assert
            var timeoutField = typeof(UnityHttpClient).GetField("_timeoutSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            var storedTimeout = (int?)timeoutField?.GetValue(client);
            Assert.AreEqual(customTimeout, storedTimeout);
        }

        [Test]
        public void Constructor_WithSkipCertificateValidation_StoresFlag()
        {
            // Arrange & Act
            var client = new UnityHttpClient(TestBaseUrl, TestApiKey, skipCertificateValidation: true);

            // Assert
            var skipCertField = typeof(UnityHttpClient).GetField("_skipCertificateValidation", BindingFlags.NonPublic | BindingFlags.Instance);
            var storedSkipCert = (bool?)skipCertField?.GetValue(client);
            Assert.True(storedSkipCert);
        }

        #endregion

        #region HttpResponse Tests

        [Test]
        public void HttpResponse_SuccessfulResponse_PropertiesSetCorrectly()
        {
            // Arrange
            var response = new HttpResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Data = "test data",
                ErrorMessage = null,
                RawContent = "test data"
            };

            // Assert
            Assert.AreEqual(200, response.StatusCode);
            Assert.True(response.IsSuccess);
            Assert.AreEqual("test data", response.Data);
            Assert.Null(response.ErrorMessage);
            Assert.AreEqual("test data", response.RawContent);
        }

        [Test]
        public void HttpResponse_ErrorResponse_PropertiesSetCorrectly()
        {
            // Arrange
            var response = new HttpResponse<string>
            {
                StatusCode = 404,
                IsSuccess = false,
                Data = null,
                ErrorMessage = "Not found",
                RawContent = "{\"error\": \"Not found\"}"
            };

            // Assert
            Assert.AreEqual(404, response.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Null(response.Data);
            Assert.AreEqual("Not found", response.ErrorMessage);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// BuildUrlプライベートメソッドを呼び出すヘルパーメソッド
        /// </summary>
        private string CallBuildUrlPrivateMethod(string endpoint, Dictionary<string, string> queryParams, UnityHttpClient client = null)
        {
            client = client ?? _httpClient;
            var buildUrlMethod = typeof(UnityHttpClient).GetMethod("BuildUrl",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Dictionary<string, string>) },
                null);

            if (buildUrlMethod == null)
            {
                Assert.Fail("BuildUrl private method not found");
            }

            return (string)buildUrlMethod.Invoke(client, new object[] { endpoint, queryParams });
        }

        #endregion
    }
}
