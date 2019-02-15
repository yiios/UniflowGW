using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniFlowGW.Models;

namespace UniFlowGW.Services
{

    public class WssHostedService : BackgroundService
    {
        IServiceProvider serviceProvider;
        ILogger<WssHostedService> _logger;
        Client client;
        IConfiguration configuration;

        public WssHostedService(IServiceProvider serviceProvider,
            Client client,
            IConfiguration configuration,
            ILogger<WssHostedService> logger)
        {

            this.serviceProvider = serviceProvider;
            this.client = client;
            this.configuration = configuration;
            this._logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            client.ConnectWSS();
            client.RegisterNetWork();
            client.GetJobList();


            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
