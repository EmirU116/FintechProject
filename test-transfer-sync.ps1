# Quick Transfer Test Script
# Tests money transfer and shows before/after balances

Write-Host "`n=== BEFORE TRANSFER ===" -ForegroundColor Cyan

# Get current balances
$cards = Invoke-RestMethod -Uri 'http://localhost:7071/api/cards' -Method Get
$card1 = $cards.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-1111' }
$card2 = $cards.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-4444' }

Write-Host "Card 1111 (John Doe):  $($card1.balance) USD" -ForegroundColor Yellow
Write-Host "Card 4444 (Jane Smith): $($card2.balance) USD" -ForegroundColor Yellow

# Prepare transfer request
$transfer = @{
    fromCardNumber = '4111111111111111'
    toCardNumber   = '5555555555554444'
    amount         = 50
    currency       = 'USD'
} | ConvertTo-Json

Write-Host "`n=== PROCESSING TRANSFER: 50 USD from 1111 to 4444 ===" -ForegroundColor Cyan

try {
    $result = Invoke-RestMethod -Uri 'http://localhost:7071/api/ProcessPayment' -Method Post -Body $transfer -ContentType 'application/json'
    Write-Host "Response: $($result.message)" -ForegroundColor Green
    
    # Wait for async processing
    Write-Host "`nWaiting 5 seconds for Service Bus processing..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== AFTER TRANSFER ===" -ForegroundColor Cyan

# Get updated balances
$cardsAfter = Invoke-RestMethod -Uri 'http://localhost:7071/api/cards' -Method Get
$card1After = $cardsAfter.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-1111' }
$card2After = $cardsAfter.cards | Where-Object { $_.cardNumberMasked -eq '****-****-****-4444' }

Write-Host "Card 1111 (John Doe):  $($card1After.balance) USD (was $($card1.balance))" -ForegroundColor Yellow
Write-Host "Card 4444 (Jane Smith): $($card2After.balance) USD (was $($card2.balance))" -ForegroundColor Yellow

# Calculate changes
$change1 = $card1After.balance - $card1.balance
$change2 = $card2After.balance - $card2.balance

if ($change1 -eq -50 -and $change2 -eq 50) {
    Write-Host "`n✓ Transfer successful!" -ForegroundColor Green
} elseif ($change1 -eq 0 -and $change2 -eq 0) {
    Write-Host "`n✗ No balance change detected. Service Bus may not be processing messages." -ForegroundColor Red
    Write-Host "  Possible issues:" -ForegroundColor Yellow
    Write-Host "  - Azure Service Bus queue 'transactions' doesn't exist" -ForegroundColor Yellow
    Write-Host "  - Service Bus connection permissions issue" -ForegroundColor Yellow
    Write-Host "  - TransferMoney function not triggering" -ForegroundColor Yellow
} else {
    Write-Host "`n? Unexpected balance changes: Card1=$change1, Card2=$change2" -ForegroundColor Magenta
}

Write-Host ""
