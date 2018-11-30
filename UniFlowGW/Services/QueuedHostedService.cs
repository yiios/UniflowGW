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
    public delegate Task ConvertTaskFunc(DatabaseContext ctx, CancellationToken token);
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(ConvertTaskFunc workItem);

        Task<ConvertTaskFunc> DequeueAsync(
            CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<ConvertTaskFunc> _workItems =
            new ConcurrentQueue<ConvertTaskFunc>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public BackgroundTaskQueue()
        {

        }

        public void QueueBackgroundWorkItem(
            ConvertTaskFunc workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<ConvertTaskFunc> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
    public class QueuedHostedService : BackgroundService
    {
        IServiceProvider serviceProvider;
        IServiceScopeFactory scopeFactory;
        public IBackgroundTaskQueue TaskQueue { get; }
        ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(IServiceProvider serviceProvider,
            IServiceScopeFactory scopeFactory,
            IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger)
        {
            this.TaskQueue = taskQueue;
            this.serviceProvider = serviceProvider;
            this.scopeFactory = scopeFactory;
            this._logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    using (var ctx = scope.ServiceProvider.GetService<DatabaseContext>())
                        await workItem(ctx, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                       $"Error occurred executing {nameof(workItem)}.");
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
