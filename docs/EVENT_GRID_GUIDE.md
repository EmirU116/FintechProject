# Event Grid Integration Guide

This guide explains how to use Event Grid in the Fintech project for event-driven architecture.

## Overview

The project publishes transaction lifecycle events to Azure Event Grid using CloudEvents 1.0 schema. Multiple subscriber functions react to these events for notifications, fraud detection, audit logging, and analytics.

## Event Catalog

### Published Events

| Event Type | Published By | Description | Data Schema |
|------------|--------------|-------------|-------------|
| `Transaction.Queued` | `ProcessPayment` | Transfer request accepted and queued | `TransactionQueuedEventData` |
| `Transaction.Settled` | `SettleTransaction` | Transfer completed successfully | `TransactionEventData` |
| `Transaction.Failed` | `SettleTransaction` | Transfer failed (insufficient funds, invalid card, etc.) | `TransactionEventData` |

### Event Data Schemas

**TransactionQueuedEventData**
```json
{
  "transactionId": "guid",
  "amount": 100.00,
  "currency": "USD",
  "fromCardMasked": "****-****-****-0366",
  "toCardMasked": "****-****-****-9903",
  "queuedAtUtc": "2025-11-30T12:00:00Z"
}
```

**TransactionEventData**
```json
{
  "transactionId": "guid",
  "amount": 100.00,
  "currency": "USD",
  "fromCardMasked": "****-****-****-0366",
  "toCardMasked": "****-****-****-9903",
  "processedAtUtc": "2025-11-30T12:00:01Z",
  "reason": "Insufficient funds" // Only for Failed events
}
```

## Subscriber Functions

### 1. OnTransactionSettled
- **Trigger**: All Event Grid events
- **Purpose**: Sample subscriber that logs all events
- **Use Case**: Development/debugging, event monitoring

### 2. SendTransactionNotification
- **Trigger**: `Transaction.Settled`, `Transaction.Failed`
- **Purpose**: Send email/SMS notifications to card holders
- **TODO**: Integrate SendGrid, Twilio, or Azure Communication Services

### 3. FraudDetectionAnalyzer
- **Trigger**: All transaction events
- **Purpose**: Real-time fraud pattern detection
- **Rules**:
  - Large amounts (>$10,000): +30 risk score
  - Round numbers (e.g., $1,000, $5,000): +20 risk score
  - Very small amounts (<$1): +15 risk score
- **TODO**: Add velocity checks, historical patterns, geo-location

### 4. AuditLogWriter
- **Trigger**: All Event Grid events
- **Purpose**: Compliance audit trail
- **Storage**: `audit_events` table (PostgreSQL)
- **Features**: Immutable event log with full CloudEvent payload

### 5. TransactionAnalytics
- **Trigger**: `Transaction.Settled`
- **Purpose**: Real-time metrics and reporting
- **TODO**: Update `transaction_metrics` table, push to Application Insights

## Infrastructure

### Event Grid Topic
```bicep
resource eventGridTopic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: eventGridTopicName
  location: location
  properties: {
    inputSchema: 'CloudEventSchemaV1_0'
    publicNetworkAccess: 'Enabled'
  }
}
```

### Event Grid Subscription
```bicep
resource eventSubscription 'Microsoft.EventGrid/eventSubscriptions@2022-06-15' = {
  name: 'fintech-transaction-settled-sub'
  scope: eventGridTopic
  properties: {
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        resourceId: '${functionApp.id}/functions/OnTransactionSettled'
      }
    }
    filter: {
      includedEventTypes: ['Transaction.Settled', 'Transaction.Failed']
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
    deadLetterDestination: {
      endpointType: 'StorageBlob'
      properties: {
        resourceId: storage.id
        blobContainerName: 'event-grid-deadletter'
      }
    }
  }
}
```

## Database Tables

Run `database/event_grid_tables.sql` to create:

- `audit_events` - Immutable audit log
- `notification_logs` - Notification delivery tracking
- `fraud_alerts` - Fraud detection results
- `transaction_metrics` - Aggregated analytics

### Views for Monitoring
- `recent_audit_events` - Last 24 hours of events
- `pending_fraud_alerts` - Alerts awaiting review
- `daily_transaction_summary` - Daily metrics for last 30 days

## Configuration

### Local Development (`local.settings.json`)
```json
{
  "Values": {
    "EventGrid:TopicEndpoint": "https://fintech-transactions-xxx.eventgrid.azure.net/api/events",
    "EventGrid:TopicKey": "<paste-key-from-azure-portal>",
    "ServiceBusConnection": "<service-bus-connection-string>",
    "ConnectionStrings:PostgreSqlConnection": "<postgres-connection-string>"
  }
}
```

### Azure Function App Settings
```bash
# Get Event Grid details
$topicName = (az eventgrid topic list -g newfintech-rg --query "[0].name" -o tsv)
$topicEndpoint = az eventgrid topic show -g newfintech-rg -n $topicName --query endpoint -o tsv
$topicKey = az eventgrid topic key list -g newfintech-rg -n $topicName --query key1 -o tsv

# Configure Function App
az webapp config appsettings set -g newfintech-rg -n fintech-func-demo --settings \
  "EventGrid:TopicEndpoint=$topicEndpoint" \
  "EventGrid:TopicKey=$topicKey"
```

## Deployment

### 1. Deploy Infrastructure
```powershell
cd .\infra
az deployment group create --resource-group newfintech-rg --template-file main.bicep --parameters functionAppName=fintech-func-demo
```

### 2. Create Database Tables
```powershell
psql -h localhost -U postgres -d postgres -f .\database\event_grid_tables.sql
```

### 3. Deploy Function App
```powershell
cd .\src\Functions
func azure functionapp publish fintech-func-demo
```

### 4. Verify Event Grid Subscription
```powershell
az eventgrid event-subscription list --source-resource-id (az eventgrid topic show -g newfintech-rg -n $topicName --query id -o tsv)
```

## Testing

### Local Testing
```powershell
# Start Functions locally
cd .\src\Functions
func start

# In another terminal, send test request
.\test-event-grid.ps1
```

### Monitor Events
```powershell
# Azure Portal: Event Grid Topic → Metrics
# - Published Events
# - Matched Events
# - Delivery Success Rate

# Application Insights Query
traces
| where message contains "Event Grid" or message contains "CloudEvent"
| order by timestamp desc
| take 20
```

### Query Audit Logs
```sql
-- Recent events
SELECT * FROM recent_audit_events LIMIT 20;

-- Pending fraud alerts
SELECT * FROM pending_fraud_alerts;

-- Daily summary
SELECT * FROM daily_transaction_summary WHERE metric_date > CURRENT_DATE - 7;
```

## Event Flow Diagram

```
HTTP Request
    ↓
ProcessPayment (Function)
    ↓
    ├→ [Event Grid] Transaction.Queued
    └→ [Service Bus Queue] transactions
         ↓
    SettleTransaction (Function)
         ↓
    MoneyTransferService
         ↓
    PostgreSQL (Update balances)
         ↓
    [Event Grid] Transaction.Settled OR Transaction.Failed
         ↓
    ┌────────────────────────────────────┐
    │                                    │
    ↓                                    ↓
OnTransactionSettled        SendTransactionNotification
    ↓                                    ↓
FraudDetectionAnalyzer      AuditLogWriter
    ↓                                    ↓
TransactionAnalytics
```

## Monitoring and Alerts

### Key Metrics
- Event publish success rate: Target >99.9%
- Subscriber execution time: Target <2s
- Dead-letter events: Alert if >0
- Fraud alerts: Monitor pending count

### Application Insights Queries

**Event Publishing Success Rate**
```kusto
traces
| where message contains "Event Grid event published"
| summarize SuccessCount = count() by bin(timestamp, 5m)
```

**Subscriber Execution Time**
```kusto
requests
| where name startswith "OnTransaction" or name startswith "Send" or name startswith "Fraud"
| summarize avg(duration), percentile(duration, 95) by name
```

**Fraud Alerts**
```kusto
traces
| where message contains "FRAUD ALERT"
| project timestamp, message
| order by timestamp desc
```

## Best Practices

1. **Idempotency**: Store processed event IDs in database to skip duplicates
2. **Error Handling**: Let exceptions bubble up to trigger Event Grid retry
3. **Dead-Letter**: Monitor dead-letter storage for failed deliveries
4. **Secrets**: Use Azure Key Vault for Event Grid keys in production
5. **Cost**: Basic tier Event Grid is cost-effective for <1M events/month

## Troubleshooting

### Event Not Published
- Check `EventGrid:TopicEndpoint` and `EventGrid:TopicKey` configuration
- Verify Function App has network access to Event Grid topic
- Review Application Insights for publish errors

### Subscriber Not Triggered
- Verify Event Grid subscription exists and is enabled
- Check event type filter matches published events
- Review dead-letter storage for failed deliveries
- Ensure Function App is running and healthy

### High Latency
- Check Event Grid metrics for delivery latency
- Review subscriber function execution time
- Verify PostgreSQL connection pool settings
- Consider increasing Function App scale-out limits

## Next Steps

- [ ] Integrate SendGrid/Twilio for notifications
- [ ] Add partner webhook management API
- [ ] Implement Managed Identity instead of access keys
- [ ] Add more sophisticated fraud detection rules
- [ ] Create Power BI dashboard from `transaction_metrics`
- [ ] Set up Azure Monitor alerts for fraud and failures
