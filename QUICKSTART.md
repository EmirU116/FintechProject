# Quick Start - Testing the Fintech Platform

**5-minute guide to get the project running locally**

## 🚀 Prerequisites

Install these first:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (optional, for Azure deployment)

## ⚡ Quick Start (Local)

### 1. Clone & Restore
```powershell
git clone https://github.com/EmirU116/FintechProject.git
cd FintechProject
dotnet restore
```

### 2. Setup Database
```powershell
# Create database and tables
psql -U postgres -f database/setup.sql

# Verify
psql -U postgres -d fintech -c "\dt"
```

### 3. Configure Settings
Create `src/Functions/local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:PostgreSqlConnection": "Host=localhost;Database=fintech;Username=postgres;Password=postgres",
    "EventGrid:TopicEndpoint": "https://localhost:7071/api/events"
  }
}
```

### 4. Run Functions
```powershell
cd src/Functions
func start
```

### 5. Test Transaction Flow

**Terminal 1** (Functions running):
```
[2025-11-30T10:00:00] Azure Functions Core Tools
[2025-11-30T10:00:00] Functions: ProcessPayment, SettleTransaction, ...
```

**Terminal 2** (Send test transaction):
```powershell
# Seed test cards
Invoke-RestMethod -Method Post -Uri "http://localhost:7071/api/SeedCreditCards"

# Send transaction
.\queue-send-demo.ps1 -Amount 50.00

# Check results
Invoke-RestMethod -Method Get -Uri "http://localhost:7071/api/GetProcessedTransactions" | ConvertTo-Json
```

**Expected logs in Terminal 1:**
```
[2025-11-30T10:00:01] 🟩 ═══ STORAGE QUEUE TRIGGER FIRED ═══
[2025-11-30T10:00:01] 🟩 Processing transaction from Storage Queue
[2025-11-30T10:00:02] 🟩 ✓ Transfer completed successfully
[2025-11-30T10:00:02] EventGrid published: fintech.transactions.processed
```

---

## ✅ Verify Everything Works

### 1. Run Unit Tests
```powershell
cd test/FintechProject.Tests
dotnet test
```
**Expected:** `Test Run Successful. Total: 51, Passed: 51`

### 2. Check Database
```powershell
psql -U postgres -d fintech -c "SELECT * FROM processed_transactions;"
```

### 3. Check Balances
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:7071/api/GetCreditCards" | ConvertTo-Json
```

---

## 🐛 Troubleshooting

### Functions won't start
```powershell
# Check versions
func --version  # Should be 4.x
dotnet --version  # Should be 8.0.x

# Rebuild
cd src/Functions
dotnet clean && dotnet build
```

### Database connection failed
```powershell
# Test connection
psql -U postgres -c "SELECT version();"

# Check your password in local.settings.json matches
```

### Queue not triggering
```powershell
# Check storage emulator (Windows)
# Download: https://go.microsoft.com/fwlink/?linkid=717179
AzureStorageEmulator.exe start

# Or use real Azure Storage (update local.settings.json)
```

---

## 🌐 Deploy to Azure (Optional)

### 1. Deploy Infrastructure
```powershell
az login
az group create --name fintech-rg --location eastus
cd infra
az deployment group create --resource-group fintech-rg --template-file main.bicep --parameters functionAppName=fintech-func-prod
```

### 2. Deploy Functions
```powershell
cd ../src/Functions
func azure functionapp publish fintech-func-prod
```

### 3. Configure Database
```powershell
# Create Azure PostgreSQL
$DB_CONNECTION = "Host=fintech-db-prod.postgres.database.azure.com;Database=fintech;Username=admin;Password=YourPassword;SSL Mode=Require"

az functionapp config connection-string set `
  --name fintech-func-prod `
  --resource-group fintech-rg `
  --connection-string-type PostgreSQL `
  --settings PostgreSqlConnection="$DB_CONNECTION"

# Run migrations
psql "$DB_CONNECTION" -f ../../database/setup.sql
```

### 4. Test in Azure
```powershell
$FUNCTION_KEY = az functionapp keys list --name fintech-func-prod --resource-group fintech-rg --query "functionKeys.default" -o tsv
$BASE_URL = "https://fintech-func-prod.azurewebsites.net/api"

# Seed data
Invoke-RestMethod -Method Post -Uri "$BASE_URL/SeedCreditCards?code=$FUNCTION_KEY"

# Test transaction
$transaction = @{
    fromCardNumber = "4532015112830366"
    toCardNumber = "5425233430109903"
    amount = 100.00
    currency = "USD"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BASE_URL/ProcessPayment?code=$FUNCTION_KEY" -Body $transaction -ContentType "application/json"
```

---

## 📚 Next Steps

- Read [TESTING_GUIDE.md](TESTING_GUIDE.md) for comprehensive testing scenarios
- Read [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for production deployment
- Read [README.md](README.md) for architecture and features

---

## 🎯 What You Just Built

✅ **Asynchronous Payment Platform** with:
- HTTP ingestion (ProcessPayment)
- Storage Queue for reliability
- Background processing (SettleTransaction)
- Event Grid for domain events (MSI auth)
- PostgreSQL for persistence
- Application Insights monitoring (sampled at 5 items/sec)

✅ **Cost-optimized**:
- Consumption plan Functions (pay-per-execution)
- Storage Queues (near-zero idle cost)
- App Insights sampling (low ingestion)
- **Total idle cost: ~$13/month**

✅ **Production-ready**:
- 51 unit tests
- Managed Identity (no keys)
- HTTPS only
- Input validation
- Structured logging
- IaC with Bicep

**Perfect for a portfolio!** 🎉
