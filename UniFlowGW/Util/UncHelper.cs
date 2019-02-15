using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNC = UNCAccessWithCredentials.UNCAccessWithCredentials;
using Microsoft.Extensions.Logging;

namespace UniFlowGW.Util
{
    public class UncHelper
    {
        UNC unc;
        public UncHelper(IConfiguration configuration,
            ILogger<UncHelper> logger)
        {
            logger.LogInformation("[UncHelper] NetUseWithCredentials");
            var targetPath = configuration["UniflowService:TaskTargetPath"];
            if (targetPath.StartsWith(@"\\"))
            {
                logger.LogInformation("Unc initialize: " + targetPath);
                unc = new UNC();
                var user = configuration["UniflowService:UncUser"];
                var domain = configuration["UniflowService:UncDomain"];
                var pwd = configuration["UniflowService:UncPassword"];
                var result = unc.NetUseWithCredentials(targetPath, user, domain, pwd);
                logger.LogInformation("[UncHelper] NetUseWithCredentials Result:" + result);
            }

        }
    }
}
