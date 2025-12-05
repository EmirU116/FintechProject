# Azure Function App - Functions Not Showing Fix

## Problem
Your Azure Functions are not appearing in the Azure Portal because of missing configuration and inconsistent setup.

## Root Causes Identified

1. ❌ **Missing `WEBSITE_RUN_FROM_PACKAGE` setting** - Required for Azure to discover functions
2. ❌ **Missing `netFrameworkVersion` in site config** - Required for .NET 8 isolated worker
3. ⚠️ **Namespace inconsistencies** - Mixed `Source.Functions` and `Functions` namespaces
4. ⚠️ **Potential missing dependencies in deployment package**

## Solutions Applied

### ✅ 1. Updated Bicep Infrastructure (`infra/main.bicep`)

Added critical settings:
```bicep
{
  name: 'WEBSITE_RUN_FROM_PACKAGE'
  value: '1'
}
```

And:
```bicep
netFrameworkVersion: 'v8.0'
```

### ✅ 2. Standardized Namespaces

Changed all function files from `Source.Functions` to `Functions` namespace for consistency.

## Next Steps - ACTION REQUIRED

### Step 1: Rebuild and Test Locally

```powershell
# Clean and rebuild
dotnet clean
dotnet build FintechProject.sln --configuration Release

# Test locally (if you have local.settings.json configured)
cd src/Functions
func start
```

### Step 2: Redeploy Infrastructure

```powershell
# Commit the changes
git add .
git commit -m "fix: Add WEBSITE_RUN_FROM_PACKAGE and standardize namespaces"
git push

# Or manually redeploy Bicep
az deployment group create \
  --resource-group newfintech-rg \
  --template-file infra/main.bicep \
  --parameters functionAppName=event-payment-func
```

### Step 3: Redeploy Function App

The CI/CD pipeline will automatically deploy, OR manually:

```powershell
# Publish locally
dotnet publish src/Functions/Functions.csproj -c Release -o ./publish

# Deploy to Azure
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..

az functionapp deployment source config-zip \
  --resource-group newfintech-rg \
  --name event-payment-func \
  --src deploy.zip
```

### Step 4: Verify in Azure Portal

1. Go to Azure Portal → Your Function App
2. Wait 2-3 minutes after deployment
3. Navigate to **Functions** blade
4. You should now see:
   - ✅ ProcessPayment (HTTP Trigger)
   - ✅ SettleTransaction (Service Bus Trigger)
   - ✅ OnTransactionSettled (Event Grid Trigger)
   - ✅ GetAuditLogs, GetCreditCards, GetProcessedTransactions (HTTP Triggers)
   - ✅ And more...

### Step 5: Check Application Settings

In Azure Portal, verify these settings exist:
- ✅ `WEBSITE_RUN_FROM_PACKAGE` = `1`
- ✅ `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- ✅ `FUNCTIONS_EXTENSION_VERSION` = `~4`
- ✅ `ServiceBusConnection` (connection string)

## Troubleshooting

### If functions still don't appear:

1. **Check Function App logs:**
   ```bash
   az functionapp log tail \
     --name event-payment-func \
     --resource-group newfintech-rg
   ```

2. **Check for errors in Application Insights:**
   - Go to Application Insights in Portal
   - Look for exceptions or errors during startup

3. **Verify deployment package:**
   ```powershell
   # The publish folder should contain:
   # - Functions.dll
   # - host.json
   # - All dependencies
   ls ./output  # Or wherever you publish
   ```

4. **Restart the Function App:**
   ```bash
   az functionapp restart \
     --name event-payment-func \
     --resource-group newfintech-rg
   ```

5. **Check if .NET 8 is supported:**
   - Consumption plan should support .NET 8 isolated worker
   - Verify in Portal: Configuration → General settings → Stack settings

## Common Issues

### Issue: "No functions found"
- Ensure `host.json` is in the root of deployment package
- Verify all `[Function("FunctionName")]` attributes are present
- Check that Functions.csproj has `OutputType` set to `Exe`

### Issue: "Could not load file or assembly"
- Missing dependencies in deployment
- Run `dotnet publish` with `--self-contained false`
- Ensure all NuGet packages are restored

### Issue: Functions appear but fail to execute
- Check connection strings in Application Settings
- Verify Service Bus connection
- Check PostgreSQL connection string

## Quick Verification Commands

```powershell
# Check if function app is running
az functionapp show \
  --name event-payment-func \
  --resource-group newfintech-rg \
  --query "state"

# List function keys (should show functions if they're loaded)
az functionapp function list \
  --name event-payment-func \
  --resource-group newfintech-rg

# Get master key to test HTTP functions
az functionapp keys list \
  --name event-payment-func \
  --resource-group newfintech-rg
```

## Expected Result

After following these steps, your Functions blade in Azure Portal should display all 13+ functions with their respective triggers clearly visible.

---
**Created:** December 2025
**Status:** Changes committed, requires redeployment
