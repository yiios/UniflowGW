using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Management;
using log4net;

namespace Licensing
{
    public static class HardwareInfo
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HardwareInfo));
        //取机器名
        public static string GetHostName()
        {
            return System.Net.Dns.GetHostName();
        }
        //取CPU编号
        public static String GetCpuID()
        {
            try
            {
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                String strCpuID = null;
                foreach (ManagementObject mo in moc)
                {
                    strCpuID = mo.Properties["ProcessorId"].Value.ToString();
                    break;
                }
                log.Debug("GetCpuID;CPUID:" + strCpuID);
                return strCpuID;
            }
            catch(Exception ex)
            {
                log.Warn(ex.Message, ex);
                return "";
            }
        }//end method
         //取第一块硬盘编号
        public static String GetHardDiskID()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
                String strHardDiskID = null;
                foreach (ManagementObject mo in searcher.Get())
                {
                    strHardDiskID = mo["SerialNumber"].ToString().Trim();
                    break;
                }
                log.Debug("GetHardDiskID;HardDiskID:" + strHardDiskID);
                return strHardDiskID;
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message, ex);
                return "";
            }
        }//end
         //取MAC Address
        public static String GetMacAddress()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration");
                String strMacAddress = null;
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo["MacAddress"] != null)
                    {
                        strMacAddress = mo["MacAddress"].ToString().Replace(":", "");
                        break;
                    }
                }
                log.Debug("GetMacAddress;MACID:" + strMacAddress);
                return strMacAddress;
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message, ex);
                return "";
            }
        }//end
         //BaseBoard SerialNumber
        public static String GetBaseBoardSN()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                String strSN = null;
                foreach (ManagementObject mo in searcher.Get())
                {
                    strSN = mo["SerialNumber"].ToString().Trim();
                    break;
                }
                log.Debug("GetBaseBoardSN;SN:" + strSN);
                return strSN;
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message, ex);
                return "";
            }
        }//end
    }
}
