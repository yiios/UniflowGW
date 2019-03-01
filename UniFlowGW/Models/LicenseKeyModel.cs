using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Models
{
    public class LicenseKeyModel
    {
        string key;
        public string Key
        {
            get => key;
            set
            {
                key = value;
                for (int i = 0; i < 5; i++)
                    KeyParts[i] = key.Substring(i * 5, 5);
            }
        }
        public string[] KeyParts { get; } = new string[5];

        [Required]
        [Display(Name = "授权码")]
        public string KeyString => string.Join('-', KeyParts);

        [Display(Name = "授权数量")]
        public int Count { get; set; }
        [Display(Name = "注册时间")]
        public DateTime IssueTime { get; set; }
        [Display(Name = "过期时间")]
        public DateTime ExpireTime { get; set; }
        [Display(Name = "有效")]
        public bool IsActive { get; set; }
    }
}
