using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions
{
    public class OnTransactionProcessed
    {
        private readonly ILogger<OnTransactionProcessed> _logger;
        public OnTransactionProcessed(ILogger<OnTransactionProcessed> logger) => _logger = logger;

        [Function("OnTransactionProcessed")]
        public Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("EventGrid received: {Type} {Subject}", eventGridEvent.EventType, eventGridEvent.Subject);
            return Task.CompletedTask;
        }
    }
}
