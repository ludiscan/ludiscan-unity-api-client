using Matuyuhi.LudiscanApi.Client.Client;
using Matuyuhi.LudiscanApi.Client.Model;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// Ludiscan APIからのエラーレスポンスを表す例外クラス
    /// APIエラー時にスローされ、エラーコードとメッセージを含みます
    /// </summary>
    public class ErrorResponseException: ApiException
    {
        private DefaultErrorResponse e;

        /// <summary>
        /// エラー識別子
        /// </summary>
        public string Error => e.Error;

        /// <summary>
        /// ErrorResponseExceptionを初期化します
        /// </summary>
        /// <param name="e">エラーレスポンス</param>
        public ErrorResponseException(DefaultErrorResponse e): base((int)e.Code, e.Message)
        {

        }

        /// <summary>
        /// エラー情報を文字列として返します
        /// </summary>
        /// <returns>エラーコードとメッセージを含む文字列</returns>
        public override string ToString() {
            return $"Code: {ErrorCode}, Message: {Message}";
        }
    }
}