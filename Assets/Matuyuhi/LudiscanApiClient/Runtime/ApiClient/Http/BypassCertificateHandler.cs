using UnityEngine.Networking;

namespace LudiscanApiClient.Runtime.ApiClient.Http
{
    /// <summary>
    /// 証明書検証をスキップするCertificateHandler
    /// 開発環境や自己署名証明書を使用する環境で使用します
    /// 注意: 本番環境では使用しないでください
    /// </summary>
    public class BypassCertificateHandler : CertificateHandler
    {
        /// <summary>
        /// 証明書の検証を行います（常にtrueを返してスキップ）
        /// </summary>
        /// <param name="certificateData">証明書データ</param>
        /// <returns>常にtrue</returns>
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // 全ての証明書を許可（開発用）
            return true;
        }
    }
}
