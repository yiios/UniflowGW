using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Licensing.LicensingSvc;
using log4net;

namespace Licensing
{
    public enum LicenseState
    {
        OK,
        Unregistered, // newly installed, please register
        MachineNotMatch,
        NoValidLicense,
        CheckByServicePostponed,
        CheckByServiceRequired,
    }
    public class LicenseCheckResult
    {
        public LicenseState State { get; set; }
        public int Amount { get; set; }
        public bool RequiresRegister
        {
            get
            {
                return this.State == LicenseState.NoValidLicense ||
                    this.State == LicenseState.MachineNotMatch ||
                    this.State == LicenseState.Unregistered;
            }
        }
        public bool Permitted
        {
            get
            {
                return
                    this.State == LicenseState.OK ||
                    this.State == LicenseState.CheckByServicePostponed;
            }
        }
        public string Message { get; set; }
    }
    public enum LicenseRegisterState
    {
        OK,
        AlreadyLicensed, // this machine have another valid license key
        InvalidKey, // key format error
        AlreadyUsed, // key is used by another machine
        Expired, // expired
        StateError,
        ServiceError,
    }
    public class LicenseRegisterResult
    {
        public LicenseRegisterState State { get; set; }
        public int Amount { get; set; }
        public string Message { get; set; }
    }
    public class LicenseChecker
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LicenseChecker));
        const uint MaxAllowedPostponeCount = 300;

        public byte[] AesKey { get; set; } = { 25, 57, 127, 110, 97, 48, 7, 172, 233, 83, 219, 187, 74, 99, 52, 160 };
        public byte[] AesIv { get; set; } = { 85, 60, 12, 116, 99, 189, 173, 19, 138, 183, 232, 248, 82, 232, 200, 242 };
        public string Password { get; set; }
        public string RsaPublicKey { get; set; }
        public KeyStorage KeyStorage { get; set; } = KeyStorage.File("key.dat");
        uint maxCheckPostponeCount = 50;
        public uint MaxCheckPostponeCount
        {
            get { return maxCheckPostponeCount; }
            set { maxCheckPostponeCount = Math.Min(value, MaxAllowedPostponeCount); }
        }

        public string ServiceEndpoint { get; set; }

        HardwareIdentifier hardware = new HardwareIdentifier
        {
            CpuID = HardwareInfo.GetCpuID(),
            HardDiskID = HardwareInfo.GetHardDiskID(),
            BaseBoardSN = HardwareInfo.GetBaseBoardSN(),
        };

        public LicenseCheckResult CheckLicenseState()
        {
            log.Debug("CheckLicenseState;");
            try
            {
                var result = CheckLicenseStateAsync();
                result.Wait();
                if (result.IsFaulted) throw result.Exception;
                return result.Result;
            }
            catch (AggregateException ex)
            {
                log.Error(ex.Message, ex);
                throw ex.InnerException;
            }
        }
        public async Task<LicenseCheckResult> CheckLicenseStateAsync()
        {
            log.Debug("CheckLicenseStateAsync;");
            // Get stored license info
            LicenseInfo lic = GetStoredLicenseInfo();
            if (lic == null) // newly installed, not registered
                return new LicenseCheckResult
                {
                    State = LicenseState.Unregistered,
                    Message = "未注册 License 或注册数据损坏。",
                };
            if (!lic.Hardware.Equals(hardware)) // stored license not match current machine
                return new LicenseCheckResult
                {
                    State = LicenseState.MachineNotMatch,
                    Message = "License 注册信息与设备不符。",
                };

            try
            {
                var tuple = await CheckLicensesByServiceAsync(lic);
                bool foundActiveKey = tuple.Item1;
                int amount = tuple.Item2;

                lic.Counter = this.MaxCheckPostponeCount;

                if (!foundActiveKey)
                    return new LicenseCheckResult
                    {
                        State = LicenseState.NoValidLicense,
                        Message = "未找到有效期内的 License。",
                    };

                return new LicenseCheckResult
                {
                    State = LicenseState.OK,
                    Amount = amount,
                    Message = "License 正常。",
                };
            }
            catch (EndpointNotFoundException ex)
            {
                log.Warn(ex.Message, ex);
                if (!lic.CountDown())
                    return new LicenseCheckResult
                    {
                        State = LicenseState.CheckByServiceRequired,
                        Message = "无法连接 License 服务。",
                    };

                if (lic.IsExpired(DateTime.Today))
                    return new LicenseCheckResult
                    {
                        State = LicenseState.NoValidLicense,
                        Message = "无法连接 License 服务，本机 License 已过期。",
                    };

                return new LicenseCheckResult
                {
                    State = LicenseState.CheckByServicePostponed,
                    Message = "无法连接 License 服务，本机 License 尚未过期，剩余 " + lic.Counter + " 次检查。",
                };
            }
            finally
            {
                StoreLicenseInfo(lic);

            }
        }

        private async Task<Tuple<bool, int>> CheckLicensesByServiceAsync(LicenseInfo lic)
        {
            log.Debug("CheckLicensesByServiceAsync;");
            LicensingClient client = new LicensingClient()
            {
                RsaPublicKey = RsaPublicKey,
                Password = Password,
            };

            foreach (var key in lic.License.Where(l => !l.Retired)
                .OrderByDescending(l => l.ExpireDate))
            {
                var response = await client.CheckLicenseAsync(
                    new LicenseCheckRequest { Hardware = hardware, Key = key.Key });
                if (response.State == LicenseCheckResponse.LicenseState.OK)
                {
                    key.Amount = response.Amount;
                    return Tuple.Create(true, response.TotalAmount);
                }
                else
                {
                    key.Retired = true; // retire keys expired, invalid, etc.
                    key.RetireReason = response.State.ToString() + ": " + response.Message;
                }
            }

            return Tuple.Create(false, 0);
        }

        private LicenseInfo GetStoredLicenseInfo()
        {
            log.Debug("GetStoredLicenseInfo;");
            var data = KeyStorage.Load();

            return data?.AesDecrypt<LicenseInfo>(AesKey, AesIv);
        }

        private void StoreLicenseInfo(LicenseInfo info)
        {
            log.Debug("StoreLicenseInfo;");
            byte[] data = info.AesEncrypt(AesKey, AesIv);
            KeyStorage.Store(data);
        }

        public LicenseRegisterResult RegisterLicense(string licenseKey)
        {
            log.Debug("RegisterLicense;");
            try
            {
                log.Debug("licenseKey:" + licenseKey);
                var result = RegisterLicenseAsync(licenseKey);
                result.Wait();
                if (result.IsFaulted) throw result.Exception;
                return result.Result;
            }
            catch (AggregateException ex)
            {
                log.Error(ex.Message, ex);
                throw ex.InnerException;
            }
        }
        public async Task<LicenseRegisterResult> RegisterLicenseAsync(string licenseKey)
        {
            LicensingClient client = new LicensingClient(
                LicensingClient.EndpointConfiguration.BasicHttpBinding_ILicensing,
                "https://localhost:6008/Licensing.svc")
            {
                Password = Password,
                RsaPublicKey = RsaPublicKey,
            };

            var response = await client.RegisterLicenseAsync(
                new RegisterRequest { Hardware = hardware, Key = licenseKey });

            if (response.State == RegisterResponse.RegisterState.OK ||
                response.State == RegisterResponse.RegisterState.OKExisted)
            {
                LicenseInfo lic = GetStoredLicenseInfo() ??
                    new LicenseInfo
                    {
                        Counter = this.MaxCheckPostponeCount,
                        Hardware = hardware,
                        License = new License[] { },
                    };

                var lickey = new License
                {
                    Key = licenseKey,
                    Retired = false,
                    Type = 1, // dummy
                    Amount = response.Amount,
                    IssueDate = response.IssueDate.Value,
                    ExpireDate = response.ExpireDate.Value,
                };
                log.Debug(string.Format("RegisterLicenseAsync;IssueDate:{0},ExpireDate:{1}", response.IssueDate.Value, response.ExpireDate.Value));
                lic.License =
                    new[] { lickey }.Concat(
                        lic.License.Where(k => string.Compare(k.Key, licenseKey, StringComparison.OrdinalIgnoreCase) != 0)
                        ).ToArray();

                lic.Counter = this.MaxCheckPostponeCount;

                StoreLicenseInfo(lic);
                return new LicenseRegisterResult
                {
                    State = response.State == RegisterResponse.RegisterState.OK ?
                        LicenseRegisterState.OK : LicenseRegisterState.AlreadyLicensed,
                    Amount = response.TotalAmount,
                    Message = response.Message,
                };
            }

            return new LicenseRegisterResult
            {
                State =
                    response.State == RegisterResponse.RegisterState.AlreadyUsed ? LicenseRegisterState.AlreadyUsed :
                    response.State == RegisterResponse.RegisterState.Invalid ? LicenseRegisterState.InvalidKey :
                    response.State == RegisterResponse.RegisterState.Expired ? LicenseRegisterState.Expired :
                    LicenseRegisterState.StateError,
                Message = response.Message,
            };
        }
    }
}
