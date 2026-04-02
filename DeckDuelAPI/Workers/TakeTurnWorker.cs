using Azure.Messaging.ServiceBus;
using DeckDuel2.Configuration;
using DeckDuel2.Domain;
using DeckDuel2.Messaging;
using Microsoft.Extensions.Options;

namespace DeckDuel2.Workers
{
    public class TakeTurnWorker : BackgroundService
    {
        private readonly ServiceBusSessionProcessor _processor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TakeTurnWorker> _logger;

        public TakeTurnWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<ServiceBusOptions> options,
            ILogger<TakeTurnWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var opts = options.Value;
            var client = new ServiceBusClient(opts.ConnectionString);

            _processor = client.CreateSessionProcessor(
                opts.QueueName,
                new ServiceBusSessionProcessorOptions
                {
                    MaxConcurrentSessions = 5
                });

            _processor.ProcessMessageAsync += OnMessageAsync;
            _processor.ProcessErrorAsync   += OnErrorAsync;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await _processor.StartProcessingAsync(ct);
            await Task.Delay(Timeout.Infinite, ct);
            await _processor.StopProcessingAsync();
        }

        private async Task OnMessageAsync(ProcessSessionMessageEventArgs args)
        {
            var msg = args.Message.Body.ToObjectFromJson<TakeTurnMessage>();
            _logger.LogInformation("Processing turn for GameId={GameId} UserId={UserId}", msg.GameId, msg.UserId);

            using var scope = _scopeFactory.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            await gameService.TakeTurnAsync(msg.GameId, msg.UserId, msg.CategoryTypeId);

            await args.CompleteMessageAsync(args.Message);
        }

        private Task OnErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Service Bus error. Source={Source}", args.ErrorSource);
            return Task.CompletedTask;
        }
    }
}