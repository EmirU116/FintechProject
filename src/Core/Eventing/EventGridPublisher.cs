using System;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Source.Core.Eventing
{
    public interface IEventGridPublisher
    {
        Task PublishTransactionProcessedAsync(object data, string subject);
        Task PublishTransactionFailedAsync(object data, string subject);
    }

    public class EventGridPublisher : IEventGridPublisher
    {
        private readonly ILogger<EventGridPublisher> _logger;
        private readonly EventGridPublisherClient _client;

        public EventGridPublisher(ILogger<EventGridPublisher> logger, IConfiguration config)
        {
            _logger = logger;
            var endpointStr = config["EventGrid:TopicEndpoint"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(endpointStr))
            {
                _logger.LogWarning("Event Grid endpoint missing: set EventGrid:TopicEndpoint.");
            }
            var endpoint = new Uri(endpointStr);
            // Use Managed Identity / DefaultAzureCredential for lowest ops overhead
            _client = new EventGridPublisherClient(endpoint, new DefaultAzureCredential());
        }

        public Task PublishTransactionProcessedAsync(object data, string subject)
            => SendAsync("fintech.transactions.processed", subject, data);

        public Task PublishTransactionFailedAsync(object data, string subject)
            => SendAsync("fintech.transactions.failed", subject, data);

        private async Task SendAsync(string eventType, string subject, object data)
        {
            var evt = new EventGridEvent(
                subject: subject,
                eventType: eventType,
                dataVersion: "1.0",
                data: BinaryData.FromObjectAsJson(data));

            await _client.SendEventAsync(evt);
            _logger.LogInformation("EventGrid published: {Type} {Subject}", eventType, subject);
        }
    }
}
