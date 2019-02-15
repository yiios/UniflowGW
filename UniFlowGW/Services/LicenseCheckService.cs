//using Licensing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Services
{
    public interface ILicenseCheck
    {
        //LicenseChecker Checker { get; }
        //LicenseCheckResult CheckResult { get; }

    }
    public enum LicenseStatus
    {
        OK,
        NotReady,
        NoValidLicenseKey,
        QuotaExceed,
    }
    public class LicenseCheckService
    {
        public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.NotReady;

        public LicenseCheckService()
        {

        }

    }
}
