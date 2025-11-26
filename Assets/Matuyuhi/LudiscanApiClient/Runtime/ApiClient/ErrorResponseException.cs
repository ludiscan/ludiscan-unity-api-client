using LudiscanApiClient.Runtime.ApiClient.Dto;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// Ludiscan APIからのエラーレスポンスを表す例外クラス
    /// APIエラー時にスローされ、エラーコードとメッセージを含みます
    /// </summary>
    public class ErrorResponseException : ApiException
    {
        private readonly DefaultErrorResponse _errorResponse;

        /// <summary>
        /// エラー識別子
        /// </summary>
        public string Error => _errorResponse.Error;

        /// <summary>
        /// ErrorResponseExceptionを初期化します
        /// </summary>
        /// <param name="errorResponse">エラーレスポンス</param>
        public ErrorResponseException(DefaultErrorResponse errorResponse)
            : base(decimal.ToInt32(errorResponse.Code), errorResponse.Message)
        {
            _errorResponse = errorResponse;
        }

        /// <summary>
        /// エラー情報を文字列として返します
        /// </summary>
        /// <returns>エラーコードとメッセージを含む文字列</returns>
        public override string ToString()
        {
            return $"Code: {ErrorCode}, Message: {Message}";
        }
    }
}
