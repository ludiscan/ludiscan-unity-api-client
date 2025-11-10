using Matuyuhi.LudiscanApi.Client.Client;
using Matuyuhi.LudiscanApi.Client.Model;

namespace LudiscanApiClient.Runtime.ApiClient
{
    public class ErrorResponseException: ApiException
    {
        private DefaultErrorResponse e;
        public string Error => e.Error;
        public ErrorResponseException(DefaultErrorResponse e): base((int)e.Code, e.Message)
        {

        }

        public override string ToString() {
            return $"Code: {ErrorCode}, Message: {Message}";
        }
    }
}