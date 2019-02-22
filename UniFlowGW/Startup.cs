﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniFlowGW.Models;
using Microsoft.EntityFrameworkCore;
using UniFlowGW.Services;
using Microsoft.Extensions.Logging;
using UniFlowGW.Controllers;
using UniFlowGW.Util;

namespace UniFlowGW
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => false;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			services.AddDistributedMemoryCache().AddSession();

			services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
			services.AddHostedService<QueuedHostedService>();
			if (bool.TryParse(Configuration["WeChat:WxWorkIOT:Switch"], out bool wxWorkIOTSwitch) && wxWorkIOTSwitch)
			{
				services.AddTransient<Client>();
				services.AddHostedService<WssHostedService>();
			}

            services.AddSingleton<SettingService>();
            services.AddSingleton<UncHelper>();

            services.AddDbContext<DatabaseContext>(options =>
				options.UseSqlite(
					Configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton<LicenseCheckService>();

            services.AddHostedService<LicenseCheckHostedService>();

            // Add scheduled tasks & scheduler
            services.AddSingleton<IScheduledTask, LicenseKeyCheckTask>();
            services.AddSingleton<IScheduledTask, DeviceQuotaCheckTask>();
            services.AddScheduler((sender, args) =>
			{
				Console.Write(args.Exception.Message);
				args.SetObserved();
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
			services.AddSession(opt =>
			{
				opt.Cookie.Name = "uniFLOW.Gateway.Session";
			});
			services.AddTransient<UniflowController>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
            app.UseDeveloperExceptionPage();
   //         if (env.IsDevelopment())
			//{
			//	app.UseDeveloperExceptionPage();
			//}
			//else
			//{
			//	app.UseExceptionHandler("/Home/Error");
			//	app.UseHsts();
			//}

			//app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCookiePolicy();
			app.UseSession();
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});

			using (var ctx = serviceProvider.GetService<DatabaseContext>())
				ctx.Database.Migrate();

			loggerFactory.AddFile(Configuration.GetSection("Logging"));
			//app.UseWebSockets();

		}
	}
}
