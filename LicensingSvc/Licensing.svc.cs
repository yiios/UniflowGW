using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Data.SQLite;
using System.Security.Cryptography;
using log4net;

namespace Licensing
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Licensing : ILicensing
    {
        public static readonly int MessageVersion = 1;

        private static readonly ILog log = LogManager.GetLogger(typeof(Licensing));

        public Response CheckLicense(Request msg)
        {
            return Crypt<LicenseCheckRequest, LicenseCheckResponse>(msg, DoCheckLicense);
        }

        public Response RegisterLicense(Request msg)
        {
            return Crypt<RegisterRequest, RegisterResponse>(msg, DoRegisterLicense);
        }

        Response Crypt<TReq, TResp>(Request msg, Func<TReq, TResp> work)
        {
            log.Info("Crypt:");
            if (msg.Version != MessageVersion)
            {
                string message = "不支持的客户消息版本";
                log.Warn(string.Format(message, msg.Version));
                throw new FaultException(message);
            }

            var rsakey = Properties.Settings.Default.RsaKey;
            var aeskey = Properties.Settings.Default.Password.ToHashedKey();

            try
            {
                var iv = msg.Token.RsaDecrypt(rsakey);

                var request = msg.Data.AesDecrypt<TReq>(aeskey, iv);
                var response = work(request);

                var data = response.AesEncrypt(aeskey, iv);
                var signature = data.GetRSASignature(rsakey);

                return new Response
                {
                    Version = msg.Version,
                    Data = data,
                    Signature = signature,
                };
            }
            catch (CryptographicException ex)
            {
                log.Warn(ex.Message, ex);
                throw new FaultException("加密解密错误。");
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new FaultException("服务器故障。");
            }
        }

        LicenseCheckResponse DoCheckLicense(LicenseCheckRequest request)
        {
            log.Info("LicenseCheckRequest;License Key:" + request.Key);
            using (var db = new LicensingDb())
            {
                var list = db.SelectByKeyAndProduct(request.Key, request.Product);
                if (list.Count == 0)
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.Invalid,
                        Message = "无效 License Key。",
                    };

                if (list.Count > 1)
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.StateError,
                        Message = "License Key 被重复使用。",
                    };

                var license = list[0];
                if (string.IsNullOrEmpty(license.HardwareInfo))
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.Unused,
                        Message = "License Key 尚未被注册使用。",
                    };

                if (string.Compare(license.HardwareInfo, request.Hardware.ToString(),
                    StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.HWMisMatch,
                        Message = "不符合已注册设备。",
                    };
                }

                if (license.ExpireDate == null)
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.StateError,
                        Message = "注册数据损坏。",
                    };

                if (license.ExpireDate.Value < DateTime.Today)
                    return new LicenseCheckResponse
                    {
                        State = LicenseCheckResponse.LicenseState.Expired,
                        Message = "License 过期。",
                    };

                int total = db.SumCountOfHWByProduct(license.HardwareInfo, request.Product);

                return new LicenseCheckResponse
                {
                    State = LicenseCheckResponse.LicenseState.OK,
                    Amount = license.Count,
                    TotalAmount = total,
                    Message = "License 正常。",
                };
            }
        }

        RegisterResponse DoRegisterLicense(RegisterRequest request)
        {
            log.Info("DoRegisterLicense;key:" + request.Key);
            using (var db = new LicensingDb(true))
            {
                var list = db.SelectByKey(request.Key);
                if (list.Count == 0)
                    return new RegisterResponse
                    {
                        State = RegisterResponse.RegisterState.Invalid,
                        Message = "无效 License Key。",
                    };

                if (list.Count > 1)
                    return new RegisterResponse
                    {
                        State = RegisterResponse.RegisterState.StateError,
                        Message = "License Key 被重复使用。",
                    };

                var license = list[0];
                if (string.IsNullOrEmpty(license.HardwareInfo))
                {
                    if (license.IssueDate == null) license.IssueDate = DateTime.Today;
                    if (license.ExpireDate == null) license.ExpireDate = DateTime.Today.AddYears(1);
                    license.HardwareInfo = request.Hardware.ToString();
                    log.Info(string.Format("DoRegisterLicense;IssueDate:{0},ExpireDate:{1},Hardware:{2}", license.IssueDate, license.ExpireDate, license.HardwareInfo));
                    try
                    {
                        db.Update(license);
                        db.Commit();

                        int totalcount = db.SumCountOfHW(license.HardwareInfo);

                        return new RegisterResponse
                        {
                            State = RegisterResponse.RegisterState.OK,
                            Amount = license.Count,
                            TotalAmount = totalcount,
                            IssueDate = license.IssueDate.Value,
                            ExpireDate = license.ExpireDate.Value,
                            Message = "注册成功。",
                        };
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message, ex);
                        return new RegisterResponse
                        {
                            State = RegisterResponse.RegisterState.StateError,
                            Message = "内部错误。",
                        };
                    }
                }

                if (string.Compare(license.HardwareInfo, request.Hardware.ToString(),
                    StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return new RegisterResponse
                    {
                        State = RegisterResponse.RegisterState.AlreadyUsed,
                        Message = "License Key 已被其他设备注册使用。",
                    };
                }

                if (license.ExpireDate == null || license.IssueDate == null)
                    return new RegisterResponse
                    {
                        State = RegisterResponse.RegisterState.StateError,
                        Message = "注册数据损坏。",
                    };

                if (license.ExpireDate.Value < DateTime.Today)
                    return new RegisterResponse
                    {
                        State = RegisterResponse.RegisterState.Expired,
                        Message = "License 过期。",
                    };

                int total = db.SumCountOfHW(license.HardwareInfo);

                return new RegisterResponse
                {
                    State = RegisterResponse.RegisterState.OKExisted,
                    Amount = license.Count,
                    TotalAmount = total,
                    IssueDate = license.IssueDate.Value,
                    ExpireDate = license.ExpireDate.Value,
                    Message = "License 正常。",
                };
            }
        }
    }
}
