# Deployment Guide

This guide walks through deploying the Fintech Payment Platform to Azure for production use.

## Prerequisites

- Azure subscription with Contributor access
- Azure CLI installed and authenticated
- .NET 8.0 SDK
- Azure Functions Core Tools v4

## Step 1: Provision Azure Resources

### 1.1 Create Resource Group

```powershell
az login
az account set --subscription "<your-subscription-id>"

$RESOURCE_GROUP = "fintech-prod-rg"
$LOCATION = "eastus"

az group create --name $RESOURCE_GROUP --location $LOCATION
```

### 1.2 Deploy Infrastructure with Bicep

```powershell
cd infra

az deployment group create `
  --resource-group $RESOURCE_GROUP `
  --template-file main.bicep `
  --parameters functionAppName=fintech-func-prod
```

This provisions:
- Storage Account (with lifecycle policies)
- Application Insights (with sampling)
- Function App (Consumption plan)
- Storage Queue (`transactions`)
- **Service Bus Namespace (Standard tier)**
- **Service Bus Queue (`critical-payments` with DLQ, duplicate detection)**
- Event Grid Custom Topic
- Role assignment (Function → Event Grid Publisher)

**Expected duration**: 3-5 minutes

### 1.3 Capture Deployment Outputs

```powershell
$FUNCTION_APP_NAME = "fintech-func-prod"
$STORAGE_ACCOUNT = az storage account list `
  --resource-group $RESOURCE_GROUP `
  --query "[0].name" -o tsv

$EVENT_GRID_TOPIC = az eventgrid topic list `
  --resource-group $RESOURCE_GROUP `
  --query "[0].name" -o tsv

Write-Host "Function App: $FUNCTION_APP_NAME"
Write-Host "Storage Account: $STORAGE_ACCOUNT"
Write-Host "Event Grid Topic: $EVENT_GRID_TOPIC"
```

## Step 2: Set Up PostgreSQL Database

### 2.1 Create Azure Database for PostgreSQL

```powershell
$DB_SERVER = "fintech-db-prod"
$DB_ADMIN_USER = "fintechadmin"
$DB_ADMIN_PASSWORD = "<generate-strong-password>"

az postgres flexible-server create `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER `
  --location $LOCATION `
  --admin-user $DB_ADMIN_USER `
  --admin-password $DB_ADMIN_PASSWORD `
  --sku-name Standard_B1ms `
  --tier Burstable `
  --version 14 `
  --storage-size 32 `
  --public-access 0.0.0.0 `
  --yes
```

### 2.2 Configure Firewall (Allow Azure Services)

```powershell
az postgres flexible-server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER `
  --rule-name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

### 2.3 Create Database and Schema

```powershell
# Connect via psql or Azure Cloud Shell
$DB_CONNECTION = "host=$DB_SERVER.postgres.database.azure.com port=5432 dbname=postgres user=$DB_ADMIN_USER password=$DB_ADMIN_PASSWORD sslmode=require"

psql "$DB_CONNECTION" -f ../database/setup.sql
```

Or use Azure CLI:
```powershell
az postgres flexible-server execute `
  --name $DB_SERVER `
  --admin-user $DB_ADMIN_USER `
  --admin-password $DB_ADMIN_PASSWORD `
  --database-name postgres `
  --file-path ../database/setup.sql
```

### 2.4 Build Connection String

```powershell
$DB_CONNECTION_STRING = "Host=$DB_SERVER.postgres.database.azure.com;Database=fintech;Username=$DB_ADMIN_USER;Password=$DB_ADMIN_PASSWORD;SSL Mode=Require"
```

## Step 3: Configure Function App Settings

### 3.1 Set Database Connection String

```powershell
az functionapp config connection-string set `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --connection-string-type PostgreSQL `
  --settings PostgreSqlConnection="$DB_CONNECTION_STRING"
```

### 3.2 Verify Event Grid Endpoint (Set by Bicep)

```powershell
az functionapp config appsettings list `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --query "[?name=='EventGrid:TopicEndpoint'].value" -o tsv
```

### 3.3 Enable System-Assigned Managed Identity (Already enabled by Bicep)

```powershell
az functionapp identity show `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP
```

## Step 4: Deploy Function App Code

### 4.1 Build and Package

```powershell
cd ../src/Functions

dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### 4.2 Deploy via Azure Functions Core Tools

```powershell
func azure functionapp publish $FUNCTION_APP_NAME
```

**Expected duration**: 1-2 minutes

### 4.3 Verify Deployment

```powershell
az functionapp function list `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --query "[].name" -o table
```

Expected functions:
- `ProcessPayment` (HTTP → Storage Queue)
- `SendCriticalPayment` (HTTP → Service Bus)
- `SettleTransaction` (Storage Queue trigger)
- `ProcessCriticalPayment` (Service Bus trigger)
- `GetProcessedTransactions`
- `GetCreditCards`
- `SeedCreditCards`
- `OnTransactionProcessed` (Event Grid trigger)

## Step 5: Initialize Database with Test Data

### 5.1 Call Seed Function

```powershell
$FUNCTION_KEY = az functionapp keys list `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --query "functionKeys.default" -o tsv

$SEED_URL = "https://$FUNCTION_APP_NAME.azurewebsites.net/api/SeedCreditCards?code=$FUNCTION_KEY"

Invoke-RestMethod -Method Post -Uri $SEED_URL
```

### 5.2 Verify Test Cards

```powershell
$GET_CARDS_URL = "https://$FUNCTION_APP_NAME.azurewebsites.net/api/GetCreditCards?code=$FUNCTION_KEY"

Invoke-RestMethod -Method Get -Uri $GET_CARDS_URL | ConvertTo-Json
```

## Step 6: Test End-to-End Flow

### 6.1 Send a Test Transaction

```powershell
$PROCESS_PAYMENT_URL = "https://$FUNCTION_APP_NAME.azurewebsites.net/api/ProcessPayment?code=$FUNCTION_KEY"

$transaction = @{
    fromCardNumber = "4532015112830366"
    toCardNumber = "5425233430109903"
    amount = 50.00
    currency = "USD"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri $PROCESS_PAYMENT_URL -Body $transaction -ContentType "application/json"
```

### 6.2 Check Application Insights Logs

```powershell
# View live metrics in Azure Portal
az monitor app-insights component show `
  --resource-group $RESOURCE_GROUP `
  --query "appId" -o tsv
```

Or query logs:
```kusto
traces
| where timestamp > ago(10m)
| where message contains "STORAGE QUEUE TRIGGER FIRED"
| project timestamp, message, severityLevel
| order by timestamp desc
```

### 6.3 Verify Transaction in Database

```powershell
psql "$DB_CONNECTION" -c "SELECT * FROM processed_transactions ORDER BY processed_at DESC LIMIT 5;"
```

### 6.4 Check Event Grid Metrics

```powershell
az monitor metrics list `
  --resource "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC" `
  --metric PublishSuccessCount `
  --start-time (Get-Date).AddMinutes(-10) `
  --end-time (Get-Date)
```

## Step 7: Set Up Event Grid Subscriptions (Optional)

### 7.1 Create Webhook Subscription

```powershell
$WEBHOOK_ENDPOINT = "https://your-downstream-service.com/api/events"

az eventgrid event-subscription create `
  --name fintech-webhook-sub `
  --source-resource-id "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC" `
  --endpoint $WEBHOOK_ENDPOINT `
  --included-event-types fintech.transactions.processed fintech.transactions.failed
```

### 7.2 Create Storage Queue Subscription (for analytics)

```powershell
az eventgrid event-subscription create `
  --name fintech-analytics-queue `
  --source-resource-id "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC" `
  --endpoint-type storagequeue `
  --endpoint "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT/queueservices/default/queues/analytics" `
  --included-event-types fintech.transactions.processed
```

## Step 8: Production Hardening

### 8.1 Enable Virtual Network Integration

```powershell
# Create VNet and subnet
az network vnet create `
  --resource-group $RESOURCE_GROUP `
  --name fintech-vnet `
  --address-prefix 10.0.0.0/16 `
  --subnet-name functions-subnet `
  --subnet-prefix 10.0.1.0/24

# Integrate Function App
az functionapp vnet-integration add `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --vnet fintech-vnet `
  --subnet functions-subnet
```

### 8.2 Restrict PostgreSQL to VNet

```powershell
az postgres flexible-server update `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER `
  --public-network-access Disabled
```

### 8.3 Enable Diagnostic Logs

```powershell
$LOG_ANALYTICS_WORKSPACE = az monitor log-analytics workspace create `
  --resource-group $RESOURCE_GROUP `
  --workspace-name fintech-logs `
  --query id -o tsv

az monitor diagnostic-settings create `
  --name fintech-diagnostics `
  --resource "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$FUNCTION_APP_NAME" `
  --workspace $LOG_ANALYTICS_WORKSPACE `
  --logs '[{"category": "FunctionAppLogs", "enabled": true}]' `
  --metrics '[{"category": "AllMetrics", "enabled": true}]'
```

### 8.4 Set Up Alerts

```powershell
# Alert on failed transactions
az monitor metrics alert create `
  --name "High Transaction Failure Rate" `
  --resource-group $RESOURCE_GROUP `
  --scopes "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC" `
  --condition "count PublishFailCount > 10" `
  --window-size 5m `
  --evaluation-frequency 1m
```

## Step 9: CI/CD Setup (Azure DevOps or GitHub Actions)

### GitHub Actions Example

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      
      - name: Build
        run: |
          cd src/Functions
          dotnet build -c Release
          dotnet publish -c Release -o ./publish
      
      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: fintech-func-prod
          package: src/Functions/publish
          publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## Step 10: Cost Management

### 10.1 Set Spending Alerts

```powershell
az consumption budget create `
  --budget-name fintech-monthly-budget `
  --amount 50 `
  --time-grain Monthly `
  --resource-group $RESOURCE_GROUP
```

### 10.2 Enable Auto-Shutdown for Dev Environments

Use Azure Automation or Functions to scale down/delete resources outside business hours.

## Rollback Procedure

If issues arise post-deployment:

```powershell
# Revert to previous deployment slot (if using slots)
az functionapp deployment slot swap `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --slot staging `
  --target-slot production

# Or redeploy previous version
func azure functionapp publish $FUNCTION_APP_NAME --slot staging
```

## Monitoring Checklist

- [ ] Application Insights live metrics showing traffic
- [ ] Event Grid publish success count > 0
- [ ] Storage Queue depth returning to 0 after processing
- [ ] PostgreSQL connections healthy (no timeouts)
- [ ] No 5xx errors in Function App logs
- [ ] Latency p99 < 500ms

## Troubleshooting

### Issue: Function not triggering from queue

**Check**:
```powershell
az storage queue list --account-name $STORAGE_ACCOUNT
az storage message peek --queue-name transactions --account-name $STORAGE_ACCOUNT
```

### Issue: Event Grid publish failing

**Check MSI permissions**:
```powershell
az role assignment list `
  --assignee $(az functionapp identity show --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP --query principalId -o tsv) `
  --scope "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC"
```

### Issue: Database connection timeout

**Check firewall**:
```powershell
az postgres flexible-server firewall-rule list `
  --resource-group $RESOURCE_GROUP `
  --name $DB_SERVER
```

---

**Deployment Complete!** 🎉

Your fintech platform is now live at:
- **API**: `https://{functionAppName}.azurewebsites.net/api/`
- **Logs**: Azure Portal → Application Insights
- **Metrics**: Azure Portal → Event Grid Topic → Metrics
