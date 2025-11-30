# Testing Guide

Complete guide for testing the Fintech Payment Platform locally and in Azure.

## 🧪 Testing Overview

### Test Levels
1. **Unit Tests** - Test core business logic (51 tests)
2. **Local Integration** - Test Functions locally with queue + database
3. **Azure Integration** - Test deployed Functions end-to-end

---

## 1️⃣ Unit Testing

### Run All Tests
```powershell
cd test/FintechProject.Tests
dotnet test
```

### Run with Detailed Output
```powershell
dotnet test --logger "console;verbosity=detailed"
```

### Run with Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage
- **51 tests** covering validators, processors, and transfer logic
- Tests located in `test/FintechProject.Tests/`
- Test files:
  - `TransactionValidatorTests.cs`
  - `TransactionProcessorTests.cs`
  - `MoneyTransferServiceTests.cs`

---

## 2️⃣ Local Testing (Development)

### Prerequisites
1. PostgreSQL running locally
2. Azure Storage Emulator or Azure Storage Account
3. Azure Functions Core Tools installed

### Step 1: Set Up Database
```powershell
# Run database setup
psql -U postgres -f database/setup.sql

# Verify tables created
psql -U postgres -d fintech -c "\dt"
```

### Step 2: Configure Local Settings

Edit `src/Functions/local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:PostgreSqlConnection": "Host=localhost;Database=fintech;Username=postgres;Password=yourpassword",
    "EventGrid:TopicEndpoint": "https://localhost:7071/api/events"
  }
}
```

**For Azure Storage (not emulator):**
```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net"
}
```

### Step 3: Start Functions Locally
```powershell
cd src/Functions
func start
```

You should see:
```
Azure Functions Core Tools
Core Tools Version: 4.x
Function Runtime Version: 4.x

Functions:
  ProcessPayment: [POST] http://localhost:7071/api/ProcessPayment
  GetCreditCards: [GET] http://localhost:7071/api/GetCreditCards
  GetProcessedTransactions: [GET] http://localhost:7071/api/GetProcessedTransactions
  SettleTransaction: queueTrigger
  OnTransactionProcessed: eventGridTrigger
```

### Step 4: Test Endpoints

#### A. Seed Test Credit Cards
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:7071/api/SeedCreditCards"
```

#### B. Check Cards and Balances
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:7071/api/GetCreditCards" | ConvertTo-Json
```

Expected output:
```json
[
  {
    "cardNumber": "****-****-****-0366",
    "cardHolderName": "John Doe",
    "balance": 1000.00,
    "currency": "USD"
  },
  {
    "cardNumber": "****-****-****-9903",
    "cardHolderName": "Jane Smith",
    "balance": 1000.00,
    "currency": "USD"
  }
]
```

#### C. Send Test Transaction (Manual)
```powershell
$transaction = @{
    fromCardNumber = "4532015112830366"
    toCardNumber = "5425233430109903"
    amount = 50.00
    currency = "USD"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:7071/api/ProcessPayment" -Body $transaction -ContentType "application/json"
```

Expected response:
```json
{
  "status": "Accepted",
  "transactionId": "abc-123-def",
  "message": "Transaction queued for processing"
}
```

#### D. Use Demo Scripts (Automated)

**Standard Transfer (Storage Queue):**
```powershell
# From project root
.\queue-send-demo.ps1 -Amount 100.00
```

**Parameters:**
- `-Amount` - Transaction amount (default: 100.00)
- `-FromCard` - Source card (default: test card)
- `-ToCard` - Destination card (default: test card)
- `-ConnectionString` - Storage connection (reads from local.settings.json if omitted)

---

**Critical Payment (Service Bus):**
```powershell
# From project root
.\servicebus-send-demo.ps1 -Amount 5000.00
```

**Parameters:**
- `-Amount` - Transaction amount (default: 5000.00)
- `-CardNumber` - Source card (default: test card)
- `-ToCardNumber` - Destination card (default: test card)
- `-ConnectionString` - Service Bus connection (reads from local.settings.json if omitted)
- `-Currency` - Currency code (default: USD)

**Critical Payment Features:**
- ✅ Guaranteed delivery via Service Bus Standard
- ✅ Dead Letter Queue (DLQ) after 10 retries
- ✅ Duplicate detection (10-minute window)
- ✅ 5-minute message lock duration

### Step 5: Verify Transaction Processing

#### Watch Logs in Real-Time
In the Functions terminal, you'll see:
```
[2025-11-30T10:00:00.123] 🟩 ═══ STORAGE QUEUE TRIGGER FIRED ═══
[2025-11-30T10:00:00.234] 🟩 Processing transaction from Storage Queue
[2025-11-30T10:00:00.345] 🟩 Transaction ID: abc-123-def
[2025-11-30T10:00:00.456] 🟩 ✓ Transfer completed successfully
[2025-11-30T10:00:00.567] EventGrid published: fintech.transactions.processed /transactions/abc-123
```

#### Check Updated Balances
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:7071/api/GetCreditCards" | ConvertTo-Json
```

#### Query Transaction History
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:7071/api/GetProcessedTransactions" | ConvertTo-Json
```

#### Check Database Directly
```powershell
psql -U postgres -d fintech -c "SELECT * FROM processed_transactions ORDER BY processed_at DESC LIMIT 5;"
```

---

## 3️⃣ Azure Testing (Production)

### Prerequisites
1. Azure resources deployed via Bicep
2. Function App published
3. Database configured

### Step 1: Get Function URLs

```powershell
$FUNCTION_APP_NAME = "fintech-func-prod"
$RESOURCE_GROUP = "fintech-prod-rg"

# Get function key
$FUNCTION_KEY = az functionapp keys list `
  --name $FUNCTION_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --query "functionKeys.default" -o tsv

# Build URLs
$BASE_URL = "https://$FUNCTION_APP_NAME.azurewebsites.net/api"
Write-Host "Base URL: $BASE_URL"
Write-Host "Function Key: $FUNCTION_KEY"
```

### Step 2: Seed Data
```powershell
$seedUrl = "$BASE_URL/SeedCreditCards?code=$FUNCTION_KEY"
Invoke-RestMethod -Method Post -Uri $seedUrl
```

### Step 3: Test Transaction Flows

#### Standard Transfer (Storage Queue)
```powershell
$processUrl = "$BASE_URL/ProcessPayment?code=$FUNCTION_KEY"

$transaction = @{
    fromCardNumber = "4532015112830366"
    toCardNumber = "5425233430109903"
    amount = 75.00
    currency = "USD"
} | ConvertTo-Json

$response = Invoke-RestMethod -Method Post -Uri $processUrl -Body $transaction -ContentType "application/json"
$response | ConvertTo-Json

# Wait for processing (Storage Queue → DB)
Start-Sleep -Seconds 5

# Check results
$historyUrl = "$BASE_URL/GetProcessedTransactions?code=$FUNCTION_KEY"
Invoke-RestMethod -Method Get -Uri $historyUrl | ConvertTo-Json
```

---

#### Critical Payment (Service Bus)
```powershell
$criticalUrl = "$BASE_URL/critical-payment?code=$FUNCTION_KEY"

$criticalPayment = @{
    cardNumber = "4532015112830366"
    toCardNumber = "5425233430109903"
    amount = 5000.00
    currency = "USD"
} | ConvertTo-Json

$response = Invoke-RestMethod -Method Post -Uri $criticalUrl -Body $criticalPayment -ContentType "application/json"
$response | ConvertTo-Json

# Wait for processing (Service Bus → DB with retries/DLQ)
Start-Sleep -Seconds 8

# Check results
Invoke-RestMethod -Method Get -Uri $historyUrl | ConvertTo-Json
```

**Monitor Critical Payment Processing:**
```powershell
# Check Service Bus queue depth
az servicebus queue show `
  --resource-group $RESOURCE_GROUP `
  --namespace-name fintech-sb-<suffix> `
  --name critical-payments `
  --query "countDetails.activeMessageCount"

# Check Dead Letter Queue (if retries exhausted)
az servicebus queue show `
  --resource-group $RESOURCE_GROUP `
  --namespace-name fintech-sb-<suffix> `
  --name critical-payments `
  --query "countDetails.deadLetterMessageCount"
```

---

### Step 4: Monitor with Application Insights

#### View Live Metrics
```powershell
az portal open --resource-group $RESOURCE_GROUP --name "$FUNCTION_APP_NAME-insights"
```

#### Query Logs (Kusto)
```kusto
traces
| where timestamp > ago(10m)
| where message contains "STORAGE QUEUE TRIGGER"
| project timestamp, message, severityLevel
| order by timestamp desc
```

#### Check Event Grid Events
```kusto
customEvents
| where name contains "EventGrid"
| where timestamp > ago(10m)
| project timestamp, name, customDimensions
```

### Step 5: Test Event Grid Integration

#### Create Test Webhook Subscription
```powershell
$EVENT_GRID_TOPIC = az eventgrid topic list `
  --resource-group $RESOURCE_GROUP `
  --query "[0].name" -o tsv

az eventgrid event-subscription create `
  --name test-webhook `
  --source-resource-id "/subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.EventGrid/topics/$EVENT_GRID_TOPIC" `
  --endpoint https://webhook.site/your-unique-url `
  --included-event-types fintech.transactions.processed fintech.transactions.failed
```

Send a transaction and check webhook.site for event delivery.

---

## 4️⃣ Load Testing (Optional)

### Simple Load Test with PowerShell
```powershell
# Test 100 concurrent transactions
$jobs = 1..100 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $key)
        $transaction = @{
            fromCardNumber = "4532015112830366"
            toCardNumber = "5425233430109903"
            amount = 1.00
            currency = "USD"
        } | ConvertTo-Json
        
        Invoke-RestMethod -Method Post -Uri "$url/ProcessPayment?code=$key" `
            -Body $transaction -ContentType "application/json"
    } -ArgumentList $BASE_URL, $FUNCTION_KEY
}

$jobs | Wait-Job | Receive-Job
```

### Using Azure Load Testing (Advanced)
1. Create Azure Load Testing resource
2. Upload JMeter test script
3. Configure endpoints and parameters
4. Run load test and review metrics

---

## 5️⃣ Troubleshooting

### Issue: Functions not starting locally
**Check:**
```powershell
# Verify Azure Functions Core Tools
func --version

# Check .NET version
dotnet --version

# Rebuild Functions
cd src/Functions
dotnet clean
dotnet build
```

### Issue: Queue not triggering
**Check Storage Queue:**
```powershell
# Local emulator
az storage queue list --connection-string "UseDevelopmentStorage=true"

# Azure Storage
az storage queue list --account-name <your-account>
az storage message peek --queue-name transactions --account-name <your-account>
```

### Issue: Database connection failed
**Check PostgreSQL:**
```powershell
# Test connection
psql -U postgres -d fintech -c "SELECT version();"

# Check tables exist
psql -U postgres -d fintech -c "\dt"

# Verify connection string in local.settings.json
```

### Issue: Event Grid not publishing
**Check MSI permissions:**
```powershell
az role assignment list `
  --assignee <function-app-principal-id> `
  --scope "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.EventGrid/topics/<topic-name>"
```

Should show: `EventGrid EventGrid Contributor` or `Event Publisher`

### Issue: High App Insights costs
**Verify sampling:**
```powershell
# Check host.json
cat src/Functions/host.json | grep -A5 samplingSettings
```

Should show:
```json
"samplingSettings": {
  "isEnabled": true,
  "maxTelemetryItemsPerSecond": 5
}
```

---

## 6️⃣ Test Checklist

### Local Development
- [ ] Unit tests pass (51/51)
- [ ] Database setup successful
- [ ] Functions start without errors
- [ ] Seed cards created
- [ ] Transaction queues successfully
- [ ] Queue trigger fires
- [ ] Database updated correctly
- [ ] Event Grid event logged

### Azure Deployment
- [ ] Infrastructure deployed
- [ ] Function App published
- [ ] Database migrations run
- [ ] Seed data created
- [ ] End-to-end transaction works
- [ ] Application Insights shows logs
- [ ] Event Grid events published
- [ ] No errors in logs

---

## 📊 Expected Performance

### Local
- HTTP response: < 100ms (202 Accepted)
- Queue → DB: < 2 seconds
- End-to-end: < 3 seconds

### Azure (Consumption Plan)
- Cold start: 2-5 seconds (first request)
- Warm: < 200ms HTTP response
- Queue → DB: < 3 seconds
- Event Grid publish: < 100ms

---

## 🔗 Additional Resources

- [Azure Functions Local Development](https://docs.microsoft.com/azure/azure-functions/functions-develop-local)
- [Storage Queue Testing](https://docs.microsoft.com/azure/storage/queues/storage-quickstart-queues-portal)
- [Event Grid Testing](https://docs.microsoft.com/azure/event-grid/custom-event-quickstart)
- [Application Insights Queries](https://docs.microsoft.com/azure/azure-monitor/logs/get-started-queries)
