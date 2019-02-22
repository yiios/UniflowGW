//using Licensing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniFlowGW.Models;

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
        //PrinterCountNotReady,
    }
    public class LicenseCheckService
    {
        private ILogger<LicenseCheckService> logger;
        private SettingService settings;

        public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.NotReady;

        public int? TotalPrinterQuota { get; set; }
        public int? PrinterCount { get; set; }
        public LicenseKeyModel[] LicenseKeys { get; set; }

        public LicenseCheckService(
            ILogger<LicenseCheckService> logger,
            SettingService settings
            )
        {
            this.logger = logger;
            this.settings = settings;
        }

        public Task CheckLicenseKeyAsync()
        {
            logger.LogDebug("Check license keys");
            return Task.CompletedTask;
        }

        public Task CheckDeviceQuotaAsync()
        {
            logger.LogDebug("Check device quota");
            return Task.CompletedTask;
        }
    }

    public class LicenseCheckHostedService : BackgroundService
    {
        ILogger<LicenseCheckHostedService> _logger;
        private LicenseCheckService licenseCheckService;

        public LicenseCheckHostedService(
            ILogger<LicenseCheckHostedService> logger,
            LicenseCheckService licenseCheckService)
        {

            this._logger = logger;
            this.licenseCheckService = licenseCheckService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Running Startup License Check.");

            await licenseCheckService.CheckLicenseKeyAsync();
            await licenseCheckService.CheckDeviceQuotaAsync();
        }
    }

    public class LicenseKeyCheckTask : IScheduledTask
    {
        private ILogger<LicenseKeyCheckTask> logger;
        private SettingService settings;
        private LicenseCheckService licenseCheckService;

        public string Schedule { get; }

        public LicenseKeyCheckTask(
            ILogger<LicenseKeyCheckTask> logger,
            SettingService settings,
            LicenseCheckService licenseCheckService)
        {
            this.logger = logger;
            this.settings = settings;
            this.licenseCheckService = licenseCheckService;

            Schedule = settings.GetOrDefault("Licensing:Schedules:LicenseKeyCheck", "0 6 * * *");
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Run license key check");
            await this.licenseCheckService.CheckLicenseKeyAsync();
        }
    }

    public class DeviceQuotaCheckTask : IScheduledTask
    {
        private ILogger<DeviceQuotaCheckTask> logger;
        private SettingService settings;
        private LicenseCheckService licenseCheckService;

        public string Schedule { get; }

        public DeviceQuotaCheckTask(
            ILogger<DeviceQuotaCheckTask> logger,
            SettingService settings,
            LicenseCheckService licenseCheckService)
        {
            this.logger = logger;
            this.settings = settings;
            this.licenseCheckService = licenseCheckService;

            Schedule = settings.GetOrDefault("Licensing:Schedules:DeviceQuotaCheck", "20 6 * * *");
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Run device quota check");
            await this.licenseCheckService.CheckDeviceQuotaAsync();
        }
    }
}
