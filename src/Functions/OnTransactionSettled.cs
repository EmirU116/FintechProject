using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions;

public class OnTransactionSettled
{
    private readonly ILogger<OnTransactionSettled> _logger;
    public OnTransactionSettled(ILogger<OnTransactionSettled> logger) => _logger = logger;

    [Function("OnTransactionSettled")]
    public void Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("📬 Event received: {Type} {Subject}", cloudEvent.Type, cloudEvent.Subject);
        if (cloudEvent.Data is not null)
        {
            _logger.LogInformation("Data: {Data}", JsonSerializer.Serialize(cloudEvent.Data));
        }
    }
}
