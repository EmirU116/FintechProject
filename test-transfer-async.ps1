# Test Async Transfer with Service Bus Queue
# This tests the full architecture: HTTP → Service Bus → Database

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
    amount         = 25
    currency       = 'USD'
} | ConvertTo-Json

Write-Host "`n=== SENDING TRANSFER REQUEST: 25 USD from 1111 to 4444 ===" -ForegroundColor Cyan

try {
    $result = Invoke-RestMethod -Uri 'http://localhost:7071/api/ProcessPayment' -Method Post -Body $transfer -ContentType 'application/json'
    Write-Host "✓ $($result.message)" -ForegroundColor Green
    Write-Host "  Amount: $($result.amount) $($result.currency)" -ForegroundColor Gray
    Write-Host "  From: $($result.fromCard)" -ForegroundColor Gray
    Write-Host "  To: $($result.toCard)" -ForegroundColor Gray
    
} catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    exit 1
}

# Wait for Service Bus processing
Write-Host "`n=== WAITING FOR SERVICE BUS PROCESSING ===" -ForegroundColor Cyan
Write-Host "The TransferMoney function should trigger from the queue..." -ForegroundColor Gray

for ($i = 10; $i -gt 0; $i--) {
    Write-Host "  $i seconds remaining..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 1
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

Write-Host "`n=== RESULT ===" -ForegroundColor Cyan

if ($change1 -eq -25 -and $change2 -eq 25) {
    Write-Host "✓ ASYNC TRANSFER SUCCESSFUL!" -ForegroundColor Green
    Write-Host "  The full flow worked: HTTP → Service Bus Queue → TransferMoney Function → Database" -ForegroundColor Green
} elseif ($change1 -eq 0 -and $change2 -eq 0) {
    Write-Host "✗ No balance change detected" -ForegroundColor Red
    Write-Host "  The message was queued but TransferMoney function may not have processed it yet." -ForegroundColor Yellow
    Write-Host "  Check the function app terminal for TransferMoney logs." -ForegroundColor Yellow
} else {
    Write-Host "? Unexpected balance changes: Card1=$change1, Card2=$change2" -ForegroundColor Magenta
}

Write-Host ""
