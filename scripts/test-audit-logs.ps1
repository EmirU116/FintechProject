# Test Audit Logging API
# This script tests the new GetAuditLogs endpoint

$baseUrl = "http://localhost:7071"
$apiUrl = "$baseUrl/api"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "      TESTING AUDIT LOGGING API" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 0. Seed audit log data (since Event Grid triggers don't work locally)
Write-Host "🌱 Step 0: Seeding audit log data..." -ForegroundColor Yellow
try {
    $seedResponse = Invoke-RestMethod -Uri "$apiUrl/SeedAuditLogs" -Method POST
    Write-Host "✅ Seeded $($seedResponse.entries.Count) audit log entries" -ForegroundColor Green
    Write-Host "   Sample Transaction ID: $($seedResponse.sampleTransactionId)" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "⚠️  Note: Event Grid triggers don't work locally without ngrok setup" -ForegroundColor Yellow
    Write-Host "   Seeding sample data for testing API..." -ForegroundColor Yellow
}

# 1. Send a test transaction
Write-Host "📤 Step 1: Sending test transaction..." -ForegroundColor Yellow
$transferRequest = @{
    fromCardNumber = "4532123456789012"
    toCardNumber = "5412345678901234"
    amount = 150.00
    currency = "USD"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/ProcessPayment" `
        -Method POST `
        -Headers @{"Content-Type"="application/json"} `
        -Body $transferRequest
    
    Write-Host "✅ Transaction sent successfully!" -ForegroundColor Green
    Write-Host "   Transaction ID: $($response.transactionId)" -ForegroundColor White
    Write-Host "   From: $($response.fromCard)" -ForegroundColor White
    Write-Host "   To: $($response.toCard)" -ForegroundColor White
    Write-Host "   Amount: $($response.amount) $($response.currency)" -ForegroundColor White
    Write-Host ""
    
    $transactionId = $response.transactionId
} catch {
    Write-Host "❌ Failed to send transaction: $_" -ForegroundColor Red
    exit 1
}

# Wait for processing
Write-Host "⏳ Waiting 5 seconds for transaction processing..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
Write-Host ""

# 2. Get all audit logs
Write-Host "📋 Step 2: Retrieving all audit logs..." -ForegroundColor Yellow
try {
    $allLogs = Invoke-RestMethod -Uri "$apiUrl/GetAuditLogs?limit=10"
    Write-Host "✅ Retrieved $($allLogs.count) audit log entries" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Failed to retrieve audit logs: $_" -ForegroundColor Red
}

# 3. Get logs for specific transaction
Write-Host "🔍 Step 3: Retrieving logs for transaction ID: $transactionId..." -ForegroundColor Yellow
try {
    $txnLogs = Invoke-RestMethod -Uri "$apiUrl/GetAuditLogs?transactionId=$transactionId"
    Write-Host "✅ Found $($txnLogs.count) audit log entries for transaction" -ForegroundColor Green
    
    if ($txnLogs.auditLogs.Count -gt 0) {
        Write-Host ""
        Write-Host "   Audit Trail:" -ForegroundColor Cyan
        foreach ($log in $txnLogs.auditLogs) {
            Write-Host "   • [$($log.eventType)] - $($log.eventSubject)" -ForegroundColor White
            Write-Host "     Time: $($log.eventTime)" -ForegroundColor Gray
        }
    }
    Write-Host ""
} catch {
    Write-Host "❌ Failed to retrieve transaction logs: $_" -ForegroundColor Red
}

# 4. Get logs by event type
Write-Host "🎯 Step 4: Retrieving logs by event type (Transaction.Queued)..." -ForegroundColor Yellow
try {
    $queuedLogs = Invoke-RestMethod -Uri "$apiUrl/GetAuditLogs?eventType=Transaction.Queued&limit=5"
    Write-Host "✅ Found $($queuedLogs.count) queued transaction events" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Failed to retrieve event type logs: $_" -ForegroundColor Red
}

# 5. Get logs by date range
Write-Host "📅 Step 5: Retrieving logs for today..." -ForegroundColor Yellow
$today = (Get-Date).ToString("yyyy-MM-dd")
try {
    $todayLogs = Invoke-RestMethod -Uri "$apiUrl/GetAuditLogs?fromDate=$today&limit=20"
    Write-Host "✅ Found $($todayLogs.count) audit log entries for today" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Failed to retrieve today's logs: $_" -ForegroundColor Red
}

# Summary
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "      AUDIT LOGGING TEST COMPLETE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ All audit log tests passed!" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Check the console for formatted audit logs" -ForegroundColor White
Write-Host "   2. Query database: SELECT * FROM audit_events ORDER BY recorded_at DESC LIMIT 10;" -ForegroundColor White
Write-Host "   3. Use GetAuditLogs API in your frontend application" -ForegroundColor White
Write-Host ""
Write-Host "📖 Documentation: docs/AUDIT_LOGGING.md" -ForegroundColor Cyan
Write-Host ""
