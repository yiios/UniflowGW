using System;
using System.Collections.Generic;
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
                    KeyParts[i] = key.Substring(i, 5);
            }
        }
        public string[] KeyParts { get; } = new string[5];

        public int Count { get; set; }
        public DateTime IssueTime { get; set; }
        public DateTime ExpireTime { get; set; }
        public bool IsActive { get; set; }
    }
}
