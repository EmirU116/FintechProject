# quick-test.ps1 - Simple quick transfer test
$transfer = @{
    fromCardNumber = '4111111111111111'
    toCardNumber   = '5555555555554444'
    amount         = 500
    currency       = 'USD'
} | ConvertTo-Json

Write-Host "Sending transfer request..." -ForegroundColor Cyan
$result = Invoke-RestMethod -Uri 'http://localhost:7071/api/ProcessPayment' -Method Post -Body $transfer -ContentType 'application/json'

Write-Host "`n✓ Transaction ID: $($result.transactionId)" -ForegroundColor Green
Write-Host "✓ Trace ID: $($result.traceId)" -ForegroundColor Green
Write-Host "`nWatch function app terminal for 🟢 and 🔵 logs" -ForegroundColor Yellow