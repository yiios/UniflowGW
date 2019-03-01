using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog.Extensions.Logging;

namespace UniFlowGW
{
	public class Program
	{
		public static void Main(string[] args)
		{
            var host = CreateWebHostBuilder(args).Build();

            if (Debugger.IsAttached || args.Contains("--console"))
            {
                host.Run();
            }
            else
            {
                host.RunAsService();
            }
        }

		public static IWebHostBuilder CreateWebHostBuilder(string[] args)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddCommandLine(args)
				.Build();

			return WebHost.CreateDefaultBuilder(args)
				.UseConfiguration(config)
				.UseStartup<Startup>();
		}

	}
}
