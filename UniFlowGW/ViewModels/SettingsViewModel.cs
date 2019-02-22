using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UniFlowGW.Controllers;
using UniFlowGW.Models;
using UniFlowGW.Services;

namespace UniFlowGW.ViewModels
{
    public class SettingsViewModel
    {
        [Required]
        [Display(Name = "临时文件目录")]
        [SettingsKey(Key = SettingsKey.SystemTempFolder)]
        public string TempFileFolder { get; set; }

        [Required]
        [Display(Name = "uniFLOW 服务器地址")]
        [SettingsKey(Key = SettingsKey.UniflowServiceURL)]
        public string UniflowServiceURL { get; set; }

        [Required]
        [Display(Name = "uniFLOW REST 服务秘钥")]
        [SettingsKey(Key = SettingsKey.UniflowServiceEncryptKey)]
        public string UniflowServiceEncryptKey { get; set; }

        [Required]
        [Display(Name = "HOT 目录路径")]
        [SettingsKey(Key = SettingsKey.UniflowServiceTaskTargetPath)]
        public string UniflowServiceTaskTargetPath { get; set; }

        [Required]
        [Display(Name = "uniFLOW 数据库连接")]
        [SettingsKey(Key = SettingsKey.UniflowConnection)]
        public string UniflowConnection { get; set; }

        [Required]
        [Display(Name = "AppId")]
        [SettingsKey(Key = SettingsKey.WeChatWxAppId)]
        public string WeChatWxAppId { get; set; }

        [Required]
        [Display(Name = "Secret")]
        [SettingsKey(Key = SettingsKey.WeChatWxSecret)]
        public string WeChatWxSecret { get; set; }

        [Required]
        [Display(Name = "AppId")]
        [SettingsKey(Key = SettingsKey.WxWorkAppId)]
        public string WxWorkAppId { get; set; }

        [Required]
        [Display(Name = "Secret")]
        [SettingsKey(Key = SettingsKey.WxWorkSecret)]
        public string WxWorkSecret { get; set; }

        [Required]
        [Display(Name = "AgentId")]
        [SettingsKey(Key = SettingsKey.WxWorkAgentId)]
        public string WxWorkAgentId { get; set; }

        [Required]
        [Display(Name = "虚拟打印机 PrinterSN")]
        [SettingsKey(Key = SettingsKey.WxWorkIOTPrinterSN)]
        public string WxWorkIOTPrinterSN { get; set; }

        [Required]
        [Display(Name = "虚拟打印机 Secret")]
        [SettingsKey(Key = SettingsKey.WxWorkIOTSecret)]
        public string WxWorkIOTSecret { get; set; }


        public string StatusMessage { get; set; }

        private static readonly Dictionary<string, PropertyInfo> settingProperties;
        static SettingsViewModel()
        {
            settingProperties = (from p in typeof(SettingsViewModel).GetProperties()
                              let attrs = p.GetCustomAttributes(typeof(SettingsKeyAttribute), false)
                              where attrs.Length > 0
                              let key = (attrs[0] as SettingsKeyAttribute).Key
                              select new { p, key })
                             .ToDictionary(pk => pk.key, pk => pk.p);
        }

        public void LoadFrom(SettingService settings)
        {
            foreach(var kp in settingProperties)
            {
                var key = kp.Key;
                var pinfo = kp.Value;

                var value = settings[key];
                pinfo.SetValue(this, value);
            }
        }

        public void StoreTo(SettingService settings)
        {
            foreach (var kp in settingProperties)
            {
                var key = kp.Key;
                var pinfo = kp.Value;

                var value = pinfo.GetValue(this).ToString();
                settings[key] = value;
            }
        }
    }
}
