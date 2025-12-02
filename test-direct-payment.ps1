# Direct test of ProcessPayment HTTP endpoint only (no Service Bus)
Write-Host "=== Testing ProcessPayment HTTP Endpoint ===" -ForegroundColor Cyan

$body = @{
    FromCardNumber = "4111111111111111"
    ToCardNumber = "5555555555554444"
    Amount = 500.00
    Currency = "USD"
} | ConvertTo-Json

Write-Host "`nSending POST request to http://localhost:7071/api/ProcessPayment" -ForegroundColor Yellow
Write-Host "Payload:" -ForegroundColor Yellow
Write-Host $body -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "http://localhost:7071/api/ProcessPayment" `
        -Method POST `
        -Body $body `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Write-Host "`n✅ SUCCESS!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
}
catch {
    Write-Host "`n❌ FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n=== Note: Service Bus trigger won't work without valid credentials ===" -ForegroundColor Yellow
Write-Host "To test full flow, update Service Bus connection string in local.settings.json" -ForegroundColor Yellow
