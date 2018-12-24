using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Licensing
{
    [Serializable]
    public class HardwareIdentifier
    {
        public string BaseBoardSN { get; set; }
        public string CpuID { get; set; }
        public string HardDiskID { get; set; }
        public override bool Equals(object obj)
        {
            var rhs = obj as HardwareIdentifier;
            return rhs != null && BaseBoardSN == rhs.BaseBoardSN
                && CpuID == rhs.CpuID && HardDiskID == rhs.HardDiskID;
        }
        public override int GetHashCode()
        {
            return BaseBoardSN.GetHashCode() ^ CpuID.GetHashCode() ^ HardDiskID.GetHashCode();
        }
        public override string ToString()
        {
            return "BBSN:" + BaseBoardSN + ";CPUID:" + CpuID + ";HDD:" + HardDiskID;
        }
    }
    [Serializable]
    public class License
    {
        public string Key { get; set; }
        public int Type { get; set; }
        public int Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool Retired { get; set; }
        public string RetireReason { get; set; }
        public bool IsExpired(DateTime check) { return check >= ExpireDate; }
    }
    [Serializable]
    public class LicenseInfo
    {
        public HardwareIdentifier Hardware { get; set; }
        public License[] License { get; set; }
        public uint Counter { get; set; }

        public bool IsExpired(DateTime check)
        {
            return License.Where(l => !l.Retired).All(l => l.IsExpired(check));
        }

        public bool CountDown()
        {
            if (Counter > 0)
            {
                Counter--;
                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class LicenseCheckRequest
    {
        public string Key { get; set; }
        public HardwareIdentifier Hardware { get; set; }
    }
    [Serializable]
    public class RegisterRequest
    {
        public string Key { get; set; }
        public HardwareIdentifier Hardware { get; set; }
    }
    [Serializable]
    public class LicenseCheckResponse
    {
        public enum LicenseState { OK, Invalid, Unused, HWMisMatch, Expired, StateError }
        public LicenseState State { get; set; }
        public int Amount { get; set; }
        public int TotalAmount { get; set; }
        public string Message { get; set; }
    }
    [Serializable]
    public class RegisterResponse
    {
        public enum RegisterState { OK, OKExisted, Invalid, AlreadyUsed, Expired, StateError }
        public RegisterState State { get; set; }
        public int Amount { get; set; }
        public int TotalAmount { get; set; }
        public DateTime? ExpireDate { get; set; }
        public DateTime? IssueDate { get; set; }
        public string Message { get; set; }
    }
}
