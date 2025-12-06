# Azure Functions Trigger Visibility Fix

## Problem
Azure Functions were deploying successfully via GitHub Actions, but the triggers were not showing up in the Azure Portal's Function App.

## Root Causes
1. **Missing trigger sync**: After deployment, Azure needs to explicitly sync the function triggers
2. **Missing restart**: Function app wasn't restarting after deployment to reload metadata
3. **Incomplete app settings**: Missing critical runtime configuration settings
4. **Build artifacts**: Metadata generation needed proper configuration

## Changes Made

### 1. Functions.csproj Updates
**File**: `src/Functions/Functions.csproj`

Added property to ensure function metadata is preserved:
```xml
<_FunctionsSkipCleanOutput>true</_FunctionsSkipCleanOutput>
```

### 2. Created .funcignore
**File**: `src/Functions/.funcignore`

Ensures only necessary files are deployed, excluding source files that could interfere with runtime metadata.

### 3. GitHub Actions Workflow Updates
**File**: `.github/workflows/ci-cd.yml`

#### A. Enhanced Publish Step
- Added explicit retention for artifacts
- Ensured proper output structure

#### B. Enhanced App Settings
Added critical settings:
```yaml
"FUNCTIONS_EXTENSION_VERSION=~4"
"AzureWebJobsDisableHomepage=false"
```

#### C. Enhanced Deployment Action
Added flags to ensure clean deployment:
```yaml
respect-funcignore: true
scm-do-build-during-deployment: false
enable-oryx-build: false
```

#### D. Added Function App Restart Step
**New step** that restarts the function app after deployment:
- Forces the runtime to reload all function metadata
- Waits 30 seconds for restart to complete

#### E. Added Trigger Sync Step
**New step** that explicitly syncs function triggers:
- Uses Azure REST API to sync triggers
- Ensures triggers are registered with Azure management plane
- Makes them visible in the Azure Portal

#### F. Enhanced Verification Step
- Lists deployed functions with their trigger types
- Displays function app URL
- Provides visibility into deployment success

## How It Works

### Deployment Flow
```
1. Build → Publish → Upload Artifact
2. Download Artifact
3. Configure App Settings (with proper runtime config)
4. Deploy to Azure Functions
5. **Restart Function App** ← NEW
6. **Sync Function Triggers** ← NEW
7. Verify & List Functions
```

### The Trigger Sync Process
When you deploy Azure Functions, the following metadata needs to be synchronized:
1. **Function.json files** - Define bindings and triggers
2. **Host.json** - Runtime configuration
3. **Trigger registration** - Azure management plane awareness

The `syncfunctiontriggers` API call ensures:
- Portal sees all function triggers
- Event Grid/Service Bus subscriptions are aware
- External systems can discover function endpoints

## Verification Steps

After your next deployment, verify:

### 1. Check GitHub Actions
The workflow should show:
- ✅ Restart completed
- ✅ Trigger sync completed
- 📋 List of deployed functions with trigger types

### 2. Check Azure Portal
Navigate to: **Function App → Functions**

You should now see:
- All function names listed
- Trigger type for each function (HTTP, EventGrid, ServiceBus, etc.)
- Function status (Enabled)

### 3. Check Trigger Details
For each function:
- Click on function name
- Go to "Integration" tab
- Verify triggers and bindings are visible

### 4. Test Functions
Test one function to ensure it's working:
```bash
# Example: Test HTTP trigger
curl -X POST https://event-payment-func.azurewebsites.net/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -d '{"fromCardNumber":"1234567890123456","toCardNumber":"9876543210987654","amount":100.00}'
```

## Common Issues & Solutions

### Issue: Functions still not showing
**Solution**: Manually sync triggers via Azure CLI:
```bash
az functionapp restart --name event-payment-func --resource-group newfintech-rg
sleep 30
az rest --method POST --uri "/subscriptions/<SUB_ID>/resourceGroups/newfintech-rg/providers/Microsoft.Web/sites/event-payment-func/syncfunctiontriggers?api-version=2023-01-01"
```

### Issue: Deployment succeeds but functions don't work
**Checklist**:
- [ ] Check Application Insights for errors
- [ ] Verify all required app settings are configured
- [ ] Ensure connection strings are correct
- [ ] Check function logs in Azure Portal

### Issue: Triggers show but don't fire
**Check**:
- Event Grid/Service Bus topic subscriptions
- Connection string configuration
- Function authorization level

## Additional Notes

### For .NET Isolated Functions (v4)
The isolated worker model requires:
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
- Proper SDK version (2.0.0+)
- Host.json version 2.0

### Best Practices
1. Always restart after deployment
2. Always sync triggers after restart
3. Use deployment slots for production (if budget allows)
4. Monitor Application Insights for cold start issues
5. Verify triggers in Portal after each deployment

## Next Steps
1. Commit and push these changes
2. Let GitHub Actions run the deployment
3. Verify triggers appear in Azure Portal
4. Test your functions

## Reference
- [Azure Functions deployment best practices](https://learn.microsoft.com/azure/azure-functions/functions-best-practices)
- [Sync triggers API](https://learn.microsoft.com/rest/api/appservice/web-apps/sync-function-triggers)
