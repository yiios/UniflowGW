//using Licensing;
using Licensing;
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
        private readonly ILogger<LicenseCheckService> logger;
        private readonly SettingService settings;
        private readonly UniflowDbAccessService uniflowDb;
        private readonly LicenseChecker licenseChecker;

        public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.NotReady;

        public int? TotalPrinterQuota { get; set; }
        public int? PrinterCount { get; set; }
        public LicenseKeyModel[] LicenseKeys { get; set; } = { };

        public LicenseCheckService(
            ILogger<LicenseCheckService> logger,
            SettingService settings,
            LicenseChecker licenseChecker,
            UniflowDbAccessService uniflowDb
            )
        {
            this.logger = logger;
            this.settings = settings;
            this.licenseChecker = licenseChecker;
            this.uniflowDb = uniflowDb;
        }

        readonly object locker = new object();
        public async Task CheckLicenseKeyAsync()
        {
            logger.LogDebug("Checking license keys");

            LicenseStatus = LicenseStatus.NotReady;

            var res = await licenseChecker.CheckLicenseStateAsync();
            logger.LogInformation($"Check Result: {res.State} - {res.Message}");

            var info = licenseChecker.GetStoredLicenseInfo();
            if (info != null)
                LicenseKeys = (from key in info.License
                               orderby key.Retired, key.IssueDate descending
                               select new LicenseKeyModel
                               {
                                   Key = key.Key,
                                   Count = key.Amount,
                                   IsActive = !key.Retired,
                                   IssueTime = key.IssueDate,
                                   ExpireTime = key.ExpireDate,
                               }).ToArray();

            lock(locker)
            {
                if (res.Permitted)
                {
                    if (res.State == LicenseState.OK)
                        TotalPrinterQuota = res.Amount;
                    else
                        TotalPrinterQuota = LicenseKeys.Where(k => k.IsActive).Sum(k => k.Count);
                    LicenseStatus = TotalPrinterQuota >= PrinterCount ?
                        LicenseStatus.OK : LicenseStatus.QuotaExceed;
                }
                else
                {
                    TotalPrinterQuota = 0;
                    if (res.RequiresRegister)
                        LicenseStatus = LicenseStatus.NoValidLicenseKey;
                    else
                        LicenseStatus = LicenseStatus.NotReady;
                }
            }
        }

        public async Task CheckDeviceQuotaAsync()
        {
            logger.LogDebug("Check device quota");

            var count = await uniflowDb.QueryPrinterCountAsync();
            lock(locker)
            {
                PrinterCount = count;
                if (LicenseStatus == LicenseStatus.OK ||
                    LicenseStatus == LicenseStatus.QuotaExceed)
                {
                    LicenseStatus = TotalPrinterQuota >= PrinterCount ?
                        LicenseStatus.OK : LicenseStatus.QuotaExceed;
                }
            }
        }

        public async Task<LicenseRegisterResult> RegisterLicenseKeyAsync(string newKey)
        {
            logger.LogDebug("Registering license key");

            var res = await licenseChecker.RegisterLicenseAsync(newKey);
            logger.LogInformation($"Register Result: {res.State} - {res.Message}");

            var info = licenseChecker.GetStoredLicenseInfo();
            if (info != null)
                LicenseKeys = (from key in info.License
                               orderby key.Retired, key.IssueDate descending
                               select new LicenseKeyModel
                               {
                                   Key = key.Key,
                                   Count = key.Amount,
                                   IsActive = !key.Retired,
                                   IssueTime = key.IssueDate,
                                   ExpireTime = key.ExpireDate,
                               }).ToArray();
            else LicenseKeys = new LicenseKeyModel[] { };

            lock (locker)
            {
                if (res.State == LicenseRegisterState.OK ||
                    res.State == LicenseRegisterState.AlreadyLicensed)
                {
                    TotalPrinterQuota = res.Amount;
                    LicenseStatus = TotalPrinterQuota >= PrinterCount ?
                        LicenseStatus.OK : LicenseStatus.QuotaExceed;
                }
            }

            return res;
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
