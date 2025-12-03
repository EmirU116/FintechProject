# Complete Deployment Guide for Event Grid Integration

## Step-by-Step Deployment

### 1. Deploy Infrastructure (Without Event Grid Subscription)
```powershell
cd .\infra
az deployment group create --resource-group newfintech-rg --template-file main.bicep --parameters functionAppName=event-payment-func
```

This creates:
- Storage Account
- Application Insights
- Function App (empty)
- Service Bus namespace and queue
- Event Grid topic
- Blob container for dead-lettering

### 2. Deploy Function App Code
```powershell
cd ..\src\Functions
func azure functionapp publish event-payment-func
```

This deploys all your functions including:
- ProcessPayment
- SettleTransaction
- OnTransactionSettled
- SendTransactionNotification
- FraudDetectionAnalyzer
- AuditLogWriter
- TransactionAnalytics

### 3. Create Event Grid Subscription
```powershell
cd ..\..
.\setup-event-grid-subscription.ps1
```

This creates the Event Grid subscription that routes events to `OnTransactionSettled`.

### 4. Configure App Settings
```powershell
# Get Event Grid details
$topicEndpoint = az deployment group show -g newfintech-rg -n main --query properties.outputs.eventGridTopicEndpoint.value -o tsv
$topicKey = az eventgrid topic key list -g newfintech-rg -n (az eventgrid topic list -g newfintech-rg --query "[0].name" -o tsv) --query key1 -o tsv

# Get Service Bus connection
$sbNs = az servicebus namespace list -g newfintech-rg --query "[0].name" -o tsv
$sbConn = az servicebus namespace authorization-rule keys list -g newfintech-rg --namespace-name $sbNs --name RootManageSharedAccessKey --query primaryConnectionString -o tsv

# Configure Function App
az webapp config appsettings set -g newfintech-rg -n event-payment-func --settings `
  "EventGrid:TopicEndpoint=$topicEndpoint" `
  "EventGrid:TopicKey=$topicKey" `
  "ServiceBusConnection=$sbConn"
```

### 5. Create Database Tables
```powershell
psql -h localhost -U postgres -d postgres -f .\database\event_grid_tables.sql
```

### 6. Test the Integration
```powershell
.\test-event-grid.ps1
```

## Why This Order?

**Problem:** Event Grid validates the function endpoint during subscription creation. If the function doesn't exist yet, deployment fails with:
```
Destination endpoint not found. Resource details: resourceId: .../functions/OnTransactionSettled
```

**Solution:** Deploy in this order:
1. Infrastructure (creates empty Function App and Event Grid topic)
2. Function code (makes OnTransactionSettled available)
3. Event Grid subscription (can now validate the function endpoint)

## Troubleshooting

### "Destination endpoint not found"
- Ensure you've published your function code (step 2)
- Verify the function exists:
  ```powershell
  az functionapp function list -g newfintech-rg -n event-payment-func
  ```

### "Deadletter endpoint validation failed"
- The blob container should be created by the Bicep template
- Verify it exists:
  ```powershell
  az storage container list --account-name <storage-account-name> --query "[?name=='event-grid-deadletter']"
  ```

### Event Grid subscription already exists
- Run the setup script again and choose to delete and recreate
- Or delete manually:
  ```powershell
  az eventgrid event-subscription delete --name fintech-transaction-settled-sub --source-resource-id <topic-resource-id>
  ```

## CI/CD Pipeline

For automated deployments, use this order in your pipeline:

```yaml
- task: AzureResourceManagerTemplateDeployment
  displayName: 'Deploy Infrastructure'
  inputs:
    templateFile: 'infra/main.bicep'

- task: AzureFunctionApp
  displayName: 'Deploy Function App'
  inputs:
    appName: 'event-payment-func'

- task: AzurePowerShell
  displayName: 'Create Event Grid Subscription'
  inputs:
    scriptPath: 'setup-event-grid-subscription.ps1'
```
