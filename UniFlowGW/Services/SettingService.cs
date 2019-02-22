using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniFlowGW.Models;

namespace UniFlowGW.Services
{
    public class SettingService
    {
        IConfiguration configuration;
        IServiceScopeFactory scopeFactory;
        public SettingService(IConfiguration configuration,
            IServiceScopeFactory scopeFactory
            )
        {
            this.configuration = configuration;
            this.scopeFactory = scopeFactory;
        }

        public string this[string key]
        {
            get
            {
                using (var scope = scopeFactory.CreateScope())
                using (var ctx = scope.ServiceProvider.GetService<DatabaseContext>())
                {
                    var setting = ctx.Settings.FirstOrDefault(s => s.Key == key);
                    if (setting != null) return setting.Value;
                }
                return configuration[key];
            }
            set
            {
                using (var scope = scopeFactory.CreateScope())
                using (var ctx = scope.ServiceProvider.GetService<DatabaseContext>())
                {
                    var setting = ctx.Settings.FirstOrDefault(s => s.Key == key);
                    if (setting == null)
                    {
                        ctx.Settings.Add(new Models.Setting
                        {
                            Key = key,
                            Value = value
                        });
                    }
                    else
                    {
                        setting.Value = value;
                    }
                    ctx.SaveChanges();
                }
            }
        }

        public string GetOrDefault(string key, string defaultValue)
        {
            using (var scope = scopeFactory.CreateScope())
            using (var ctx = scope.ServiceProvider.GetService<DatabaseContext>())
            {
                var setting = ctx.Settings.FirstOrDefault(s => s.Key == key);
                if (setting != null) return setting.Value;
            }
            return configuration[key] ?? defaultValue;
        }
    }
}
