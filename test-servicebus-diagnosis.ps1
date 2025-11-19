# Test Service Bus Connection and Processing
Write-Host "`n=== TESTING SERVICE BUS ASYNC PROCESSING ===" -ForegroundColor Cyan

# Step 1: Check current queue status
Write-Host "`nStep 1: Checking Service Bus Queue Status..." -ForegroundColor Yellow
$queueStatus = az servicebus queue show --namespace-name fintech-sb-pxlpmrqgmhsls --resource-group newfintech-rg --name transactions --query '{MessageCount:messageCount}' -o json | ConvertFrom-Json
Write-Host "  Messages in queue: $($queueStatus.MessageCount)" -ForegroundColor Gray

# Step 2: Get current balances
Write-Host "`nStep 2: Getting Current Balances..." -ForegroundColor Yellow
$cards = Invoke-RestMethod -Uri 'http://localhost:7071/api/cards' -Method Get
$card1 = $cards.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-1111' }
$card2 = $cards.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-4444' }
Write-Host "  Card 1111: $($card1.balance) USD" -ForegroundColor Gray
Write-Host "  Card 4444: $($card2.balance) USD" -ForegroundColor Gray

# Step 3: Send transfer request
Write-Host "`nStep 3: Sending Transfer Request (10 USD)..." -ForegroundColor Yellow
$transfer = @{
    fromCardNumber = '4111111111111111'
    toCardNumber   = '5555555555554444'
    amount         = 10
    currency       = 'USD'
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri 'http://localhost:7071/api/ProcessPayment' -Method Post -Body $transfer -ContentType 'application/json'
    Write-Host "  ✓ $($result.message)" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Check queue again
Write-Host "`nStep 4: Checking if message was queued..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
$queueStatusAfter = az servicebus queue show --namespace-name fintech-sb-pxlpmrqgmhsls --resource-group newfintech-rg --name transactions --query '{MessageCount:messageCount}' -o json | ConvertFrom-Json
Write-Host "  Messages in queue now: $($queueStatusAfter.MessageCount)" -ForegroundColor Gray

if ($queueStatusAfter.MessageCount -gt $queueStatus.MessageCount) {
    Write-Host "  ✓ Message successfully queued!" -ForegroundColor Green
} else {
    Write-Host "  ? Message count didn't increase" -ForegroundColor Yellow
}

# Step 5: Wait and monitor
Write-Host "`nStep 5: Waiting for TransferMoney function to process..." -ForegroundColor Yellow
Write-Host "  (Check the function app terminal for 'Processing transfer from Service Bus queue' logs)" -ForegroundColor DarkGray

for ($i = 15; $i -gt 0; $i--) {
    Write-Host "  $i..." -NoNewline -ForegroundColor DarkGray
    Start-Sleep -Seconds 1
}
Write-Host ""

# Step 6: Check final balances
Write-Host "`nStep 6: Checking Final Balances..." -ForegroundColor Yellow
$cardsAfter = Invoke-RestMethod -Uri 'http://localhost:7071/api/cards' -Method Get
$card1After = $cardsAfter.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-1111' }
$card2After = $cardsAfter.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-4444' }
Write-Host "  Card 1111: $($card1After.balance) USD (was $($card1.balance))" -ForegroundColor Gray
Write-Host "  Card 4444: $($card2After.balance) USD (was $($card2.balance))" -ForegroundColor Gray

$change1 = $card1After.balance - $card1.balance
$change2 = $card2After.balance - $card2.balance

# Step 7: Check queue one more time
$queueFinal = az servicebus queue show --namespace-name fintech-sb-pxlpmrqgmhsls --resource-group newfintech-rg --name transactions --query '{MessageCount:messageCount}' -o json | ConvertFrom-Json
Write-Host "`nStep 7: Final Queue Status..." -ForegroundColor Yellow
Write-Host "  Messages remaining: $($queueFinal.MessageCount)" -ForegroundColor Gray

# Results
Write-Host "`n=== DIAGNOSIS ===" -ForegroundColor Cyan

if ($change1 -eq -10 -and $change2 -eq 10) {
    Write-Host "✓ SUCCESS! The async flow is working:" -ForegroundColor Green
    Write-Host "  HTTP → Service Bus Queue → TransferMoney → Database" -ForegroundColor Green
} elseif ($change1 -eq 0 -and $change2 -eq 0) {
    Write-Host "✗ PROBLEM: TransferMoney function is NOT processing messages" -ForegroundColor Red
    Write-Host "`nPossible causes:" -ForegroundColor Yellow
    Write-Host "  1. Service Bus connection string is invalid" -ForegroundColor Yellow
    Write-Host "  2. Function app doesn't have permission to read from queue" -ForegroundColor Yellow
    Write-Host "  3. TransferMoney function trigger is not starting" -ForegroundColor Yellow
    Write-Host "  4. Messages in queue are malformed" -ForegroundColor Yellow
    Write-Host "`nTo diagnose further:" -ForegroundColor Cyan
    Write-Host "  - Check function app terminal for any Service Bus connection errors" -ForegroundColor White
    Write-Host "  - Look for 'TransferMoney' or 'Service Bus' in the logs" -ForegroundColor White
    Write-Host "  - Verify the connection string in local.settings.json is correct" -ForegroundColor White
}

Write-Host ""
