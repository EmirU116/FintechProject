# Setup Event Grid Subscription for OnTransactionSettled Function
# Run this AFTER deploying your Function App code

param(
    [string]$ResourceGroup = "newfintech-rg",
    [string]$FunctionAppName = "event-payment-func",
    [string]$SubscriptionName = "fintech-transaction-settled-sub"
)

Write-Host "🔵 Setting up Event Grid Subscription" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get the Event Grid topic
Write-Host "📋 Getting Event Grid topic..." -ForegroundColor Yellow
$topicName = az eventgrid topic list -g $ResourceGroup --query "[0].name" -o tsv

if ([string]::IsNullOrEmpty($topicName)) {
    Write-Host "❌ Event Grid topic not found. Deploy infrastructure first!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found Event Grid topic: $topicName" -ForegroundColor Green

# Get the Function App resource ID
Write-Host "📋 Getting Function App details..." -ForegroundColor Yellow
$functionAppId = az functionapp show -g $ResourceGroup -n $FunctionAppName --query id -o tsv

if ([string]::IsNullOrEmpty($functionAppId)) {
    Write-Host "❌ Function App not found: $FunctionAppName" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found Function App: $FunctionAppName" -ForegroundColor Green

# Check if OnTransactionSettled function exists
Write-Host "📋 Checking if OnTransactionSettled function exists..." -ForegroundColor Yellow
$functions = az functionapp function list -g $ResourceGroup -n $FunctionAppName --query "[].name" -o tsv

$functionExists = $false
foreach ($func in $functions) {
    if ($func -like "*OnTransactionSettled*") {
        $functionExists = $true
        break
    }
}

if (-not $functionExists) {
    Write-Host "❌ OnTransactionSettled function not found!" -ForegroundColor Red
    Write-Host "   Please deploy your function code first:" -ForegroundColor Yellow
    Write-Host "   cd .\src\Functions" -ForegroundColor Gray
    Write-Host "   func azure functionapp publish $FunctionAppName" -ForegroundColor Gray
    exit 1
}

Write-Host "✅ OnTransactionSettled function exists" -ForegroundColor Green

# Get storage account for dead-letter
Write-Host "📋 Getting storage account for dead-letter..." -ForegroundColor Yellow
$storageAccount = az storage account list -g $ResourceGroup --query "[?starts_with(name, 'payapi')].{name:name,id:id}" -o json | ConvertFrom-Json
$storageId = $storageAccount.id
$storageName = $storageAccount.name

Write-Host "✅ Found storage account: $storageName" -ForegroundColor Green

# Check if event subscription already exists
Write-Host "📋 Checking for existing subscription..." -ForegroundColor Yellow
$existingSub = az eventgrid event-subscription show `
    --name $SubscriptionName `
    --source-resource-id "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$ResourceGroup/providers/Microsoft.EventGrid/topics/$topicName" `
    2>$null

if ($existingSub) {
    Write-Host "⚠️  Event subscription already exists: $SubscriptionName" -ForegroundColor Yellow
    $confirm = Read-Host "Delete and recreate? (y/n)"
    if ($confirm -eq 'y') {
        Write-Host "🗑️  Deleting existing subscription..." -ForegroundColor Yellow
        az eventgrid event-subscription delete `
            --name $SubscriptionName `
            --source-resource-id "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$ResourceGroup/providers/Microsoft.EventGrid/topics/$topicName"
        Write-Host "✅ Deleted" -ForegroundColor Green
    } else {
        Write-Host "❌ Cancelled" -ForegroundColor Red
        exit 0
    }
}

# Create the Event Grid subscription
Write-Host ""
Write-Host "🚀 Creating Event Grid subscription..." -ForegroundColor Cyan
Write-Host "   Subscription: $SubscriptionName" -ForegroundColor Gray
Write-Host "   Topic: $topicName" -ForegroundColor Gray
Write-Host "   Function: OnTransactionSettled" -ForegroundColor Gray
Write-Host ""

$subscriptionId = az account show --query id -o tsv
$topicResourceId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.EventGrid/topics/$topicName"
$functionResourceId = "$functionAppId/functions/OnTransactionSettled"

az eventgrid event-subscription create `
    --name $SubscriptionName `
    --source-resource-id $topicResourceId `
    --endpoint-type azurefunction `
    --endpoint $functionResourceId `
    --event-delivery-schema cloudeventschemav1_0 `
    --included-event-types "Transaction.Settled" "Transaction.Failed" `
    --max-delivery-attempts 30 `
    --event-ttl 1440 `
    --deadletter-endpoint "$storageId/blobServices/default/containers/event-grid-deadletter"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Event Grid subscription created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Verify in Azure Portal:" -ForegroundColor Cyan
    Write-Host "   1. Go to Event Grid Topic: $topicName" -ForegroundColor Gray
    Write-Host "   2. Click 'Event Subscriptions'" -ForegroundColor Gray
    Write-Host "   3. You should see: $SubscriptionName" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🧪 Test the integration:" -ForegroundColor Cyan
    Write-Host "   .\test-event-grid.ps1" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "❌ Failed to create Event Grid subscription" -ForegroundColor Red
    Write-Host "   Check the error message above for details" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
