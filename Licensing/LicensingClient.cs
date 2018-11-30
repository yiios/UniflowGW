using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Svc = Licensing.LicensingSvc;
using log4net;

namespace Licensing.LicensingSvc
{
    public interface IReqBase
    {
        Request Request { get; set; }
    }
    public interface IRespBase
    {
        Response Response { get; set; }
    }
    partial class CheckLicenseRequest : IReqBase
    {
        public Request Request {
            get => data;
            set { data = value; }
        }
    }
    partial class CheckLicenseResponse : IRespBase
    {
        public Response Response
        {
            get => CheckLicenseResult;
            set { CheckLicenseResult = value; }
        }
    }
    partial class RegisterLicenseRequest : IReqBase
    {
        public Request Request
        {
            get => data;
            set { data = value; }
        }
    }
    partial class RegisterLicenseResponse : IRespBase
    {
        public Response Response
        {
            get => RegisterLicenseResult;
            set { RegisterLicenseResult = value; }
        }
    }
    partial class LicensingClient
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LicensingClient));
        public static readonly int MessageVersion = 1;

        public string RsaPublicKey { get; set; }
        public string Password { get; set; }

        public async Task<LicenseCheckResponse> CheckLicenseAsync(LicenseCheckRequest request)
        {
            return await CryptAsync<LicenseCheckRequest, LicenseCheckResponse,
                CheckLicenseRequest, CheckLicenseResponse>(
                request, base.Channel.CheckLicenseAsync);
        }

        public async Task<RegisterResponse> RegisterLicenseAsync(RegisterRequest request)
        {
            return await CryptAsync<RegisterRequest, RegisterResponse,
                RegisterLicenseRequest, RegisterLicenseResponse>(
                request, base.Channel.RegisterLicenseAsync);
        }

        public async Task<TResp> CryptAsync<TReq, TResp, TReqWrap, TRespWrap>
            (TReq request, Func<TReqWrap, Task<TRespWrap>> workAsync)
            where TReqWrap : IReqBase, new()
            where TRespWrap : IRespBase, new()
        {
            try
            {
                var aeskey = Password.ToHashedKey();
                var iv = CryptHelper.MakeRandomKey();

                var data = request.AesEncrypt(aeskey, iv);
                var token = iv.RsaEncrypt(RsaPublicKey);
                var response = (await workAsync(
                    new TReqWrap
                    {
                        Request = new Svc.Request
                        {
                            Version = MessageVersion,
                            Data = data,
                            Token = token
                        }
                    })).Response;

                System.Diagnostics.Debug.Assert(response.Version == MessageVersion);

                if (!response.Data.VerifyRSASignature(response.Signature, RsaPublicKey))
                    throw new CryptographicException("消息签名验证失败。");

                return response.Data.AesDecrypt<TResp>(aeskey, iv);
            }
            catch (CryptographicException ex)
            {
                log.Warn(ex.Message, ex);
                throw new InvalidOperationException(
                    "加密解密失败，请检查密码和秘钥配置。", ex);
            }
            catch (FaultException ex)
            {
                log.Error(ex.Message, ex);
                throw new InvalidOperationException(
                    "服务端故障：" + ex.Message, ex);
            }
        }
    }
}
