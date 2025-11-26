using System;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// API呼び出しで発生した例外を表すクラス
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// HTTPステータスコード
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// ApiExceptionを初期化します
        /// </summary>
        /// <param name="errorCode">HTTPステータスコード</param>
        /// <param name="message">エラーメッセージ</param>
        public ApiException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// ApiExceptionを初期化します
        /// </summary>
        /// <param name="errorCode">HTTPステータスコード</param>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="innerException">内部例外</param>
        public ApiException(int errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// エラー情報を文字列として返します
        /// </summary>
        public override string ToString()
        {
            return $"ApiException: Code={ErrorCode}, Message={Message}";
        }
    }
}
