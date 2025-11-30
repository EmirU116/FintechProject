# Test Event Grid Integration
# This script sends a test transfer request and monitors Event Grid events

param(
    [string]$FunctionUrl = "http://localhost:7071/api/ProcessPayment",
    [decimal]$Amount = 100.00,
    [string]$FromCard = "4532015112830366",
    [string]$ToCard = "5425233430109903"
)

Write-Host "🧪 Event Grid Integration Test" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Test payload
$testPayload = @{
    fromCardNumber = $FromCard
    toCardNumber = $ToCard
    amount = $Amount
    currency = "USD"
} | ConvertTo-Json

Write-Host "📤 Sending test transfer request..." -ForegroundColor Yellow
Write-Host "From: ****${FromCard.Substring($FromCard.Length - 4)}" -ForegroundColor Gray
Write-Host "To: ****${ToCard.Substring($ToCard.Length - 4)}" -ForegroundColor Gray
Write-Host "Amount: $Amount USD" -ForegroundColor Gray
Write-Host ""

try {
    # Send HTTP request
    $response = Invoke-RestMethod -Uri $FunctionUrl -Method Post -Body $testPayload -ContentType "application/json"
    
    Write-Host "✅ Request accepted!" -ForegroundColor Green
    Write-Host "Transaction ID: $($response.transactionId)" -ForegroundColor Green
    Write-Host "Trace ID: $($response.traceId)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 Expected Event Flow:" -ForegroundColor Cyan
    Write-Host "1. Transaction.Queued - Published by ProcessPayment" -ForegroundColor White
    Write-Host "2. Transaction.Settled or Transaction.Failed - Published by SettleTransaction" -ForegroundColor White
    Write-Host ""
    
    Write-Host "👀 Check your Function App logs for:" -ForegroundColor Yellow
    Write-Host "- 🟢 Transaction.Queued event published" -ForegroundColor Gray
    Write-Host "- 🔵 Transaction.Settled event published" -ForegroundColor Gray
    Write-Host "- 📬 OnTransactionSettled received event" -ForegroundColor Gray
    Write-Host "- 📧 SendTransactionNotification triggered" -ForegroundColor Gray
    Write-Host "- 🔍 FraudDetectionAnalyzer analyzed" -ForegroundColor Gray
    Write-Host "- 📝 AuditLogWriter logged event" -ForegroundColor Gray
    Write-Host "- 📊 TransactionAnalytics updated" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "💡 Monitor Event Grid in Azure Portal:" -ForegroundColor Cyan
    Write-Host "1. Go to your Event Grid Topic" -ForegroundColor Gray
    Write-Host "2. Click 'Metrics' to see published/delivered events" -ForegroundColor Gray
    Write-Host "3. Check Event Subscriptions for delivery status" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "🔎 Query Application Insights:" -ForegroundColor Cyan
    Write-Host "traces" -ForegroundColor Gray
    Write-Host "| where message contains 'Event Grid' or message contains 'CloudEvent'" -ForegroundColor Gray
    Write-Host "| order by timestamp desc" -ForegroundColor Gray
    Write-Host "| take 20" -ForegroundColor Gray
    
} catch {
    Write-Host "❌ Request failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test completed" -ForegroundColor Cyan
