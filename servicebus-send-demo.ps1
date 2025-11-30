# Service Bus Critical Payment Demo Script
# This script sends a critical payment transaction to the Service Bus queue

param(
    [string]$ConnectionString,
    [decimal]$Amount = 5000.00,
    [string]$CardNumber = "4532015112830366",
    [string]$ToCardNumber = "5425233430109903",
    [string]$Currency = "USD"
)

Write-Host "🔴 ═══ SERVICE BUS CRITICAL PAYMENT DEMO ═══" -ForegroundColor Red

# If no connection string provided, try to read from local.settings.json
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $settingsPath = Join-Path $PSScriptRoot "local.settings.json"
    if (Test-Path $settingsPath) {
        Write-Host "📖 Reading connection string from local.settings.json..." -ForegroundColor Cyan
        $settings = Get-Content $settingsPath | ConvertFrom-Json
        $ConnectionString = $settings.Values.ServiceBusConnectionString
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "❌ ERROR: ServiceBusConnectionString not found!" -ForegroundColor Red
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\servicebus-send-demo.ps1 -ConnectionString 'Endpoint=sb://...'" -ForegroundColor Yellow
    Write-Host "  OR add ServiceBusConnectionString to local.settings.json" -ForegroundColor Yellow
    exit 1
}

# Install Azure.Messaging.ServiceBus if not present
Write-Host "📦 Checking for Azure.Messaging.ServiceBus module..." -ForegroundColor Cyan
if (-not (Get-Module -ListAvailable -Name "Az.ServiceBus")) {
    Write-Host "⚠️  Az.ServiceBus module not found. Using .NET client instead..." -ForegroundColor Yellow
}

# Generate transaction payload
$transactionId = [Guid]::NewGuid().ToString()
$transaction = @{
    Id = $transactionId
    CardNumber = $CardNumber
    CardNumberMasked = "****-****-****-$($CardNumber.Substring($CardNumber.Length - 4))"
    ToCardNumber = $ToCardNumber
    ToCardNumberMasked = "****-****-****-$($ToCardNumber.Substring($ToCardNumber.Length - 4))"
    Amount = $Amount
    Currency = $Currency
    Timestamp = (Get-Date).ToUniversalTime().ToString("o")
}

$messageBody = $transaction | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "📋 Transaction Details:" -ForegroundColor Green
Write-Host "  Transaction ID: $transactionId" -ForegroundColor White
Write-Host "  From Card:      $($transaction.CardNumberMasked)" -ForegroundColor White
Write-Host "  To Card:        $($transaction.ToCardNumberMasked)" -ForegroundColor White
Write-Host "  Amount:         $Amount $Currency" -ForegroundColor White
Write-Host ""

# Create inline C# to send message
$csharpCode = @"
using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

public class ServiceBusSender
{
    public static async Task SendMessage(string connectionString, string queueName, string messageBody, string messageId)
    {
        await using var client = new ServiceBusClient(connectionString);
        await using var sender = client.CreateSender(queueName);
        
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = messageId,
            ContentType = "application/json",
            Subject = "CriticalPayment"
        };
        
        await sender.SendMessageAsync(message);
    }
}
"@

try {
    Write-Host "📤 Sending critical payment to Service Bus queue 'critical-payments'..." -ForegroundColor Cyan
    
    # Add the Azure.Messaging.ServiceBus type
    Add-Type -Path "$env:USERPROFILE\.nuget\packages\azure.messaging.servicebus\7.17.5\lib\net6.0\Azure.Messaging.ServiceBus.dll" -ErrorAction Stop
    Add-Type -Path "$env:USERPROFILE\.nuget\packages\azure.core\1.35.0\lib\net6.0\Azure.Core.dll" -ErrorAction Stop
    Add-Type -ReferencedAssemblies @(
        "$env:USERPROFILE\.nuget\packages\azure.messaging.servicebus\7.17.5\lib\net6.0\Azure.Messaging.ServiceBus.dll",
        "$env:USERPROFILE\.nuget\packages\azure.core\1.35.0\lib\net6.0\Azure.Core.dll"
    ) -TypeDefinition $csharpCode
    
    [ServiceBusSender]::SendMessage($ConnectionString, "critical-payments", $messageBody, $transactionId).Wait()
    
    Write-Host ""
    Write-Host "✅ Critical payment successfully queued to Service Bus!" -ForegroundColor Green
    Write-Host "   Queue: critical-payments" -ForegroundColor White
    Write-Host "   Transaction ID: $transactionId" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 Next Steps:" -ForegroundColor Yellow
    Write-Host "   1. Check Function App logs to see processing (ProcessCriticalPayment function)" -ForegroundColor White
    Write-Host "   2. Query ProcessedTransactions table in PostgreSQL" -ForegroundColor White
    Write-Host "   3. Monitor Event Grid events (OnTransactionProcessed function)" -ForegroundColor White
    Write-Host "   4. Check Service Bus Dead Letter Queue if processing fails after 10 retries" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Key Features:" -ForegroundColor Cyan
    Write-Host "   ✓ Guaranteed delivery (Service Bus Standard)" -ForegroundColor White
    Write-Host "   ✓ Duplicate detection (10-minute window)" -ForegroundColor White
    Write-Host "   ✓ Dead Letter Queue (DLQ) after 10 retries" -ForegroundColor White
    Write-Host "   ✓ 5-minute message lock duration" -ForegroundColor White
    Write-Host "   ✓ Event Grid domain events on success/failure" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR: Failed to send message to Service Bus" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Verify Service Bus connection string is correct" -ForegroundColor White
    Write-Host "   2. Ensure 'critical-payments' queue exists" -ForegroundColor White
    Write-Host "   3. Check that you have Send permissions on the queue" -ForegroundColor White
    Write-Host "   4. Run: Install-Package Azure.Messaging.ServiceBus" -ForegroundColor White
    exit 1
}
