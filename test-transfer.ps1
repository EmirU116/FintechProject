# Money Transfer Testing Script
# This script helps test the money transfer functionality

Write-Host "=== Money Transfer API Testing ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:7071/api"

# Function to display test results
function Show-Result {
    param($Response, $TestName)
    Write-Host "Test: $TestName" -ForegroundColor Yellow
    $Response | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""
}

Write-Host "Step 1: Seeding database with test cards..." -ForegroundColor Green
try {
    $seedResult = Invoke-RestMethod -Uri "$baseUrl/seed-cards" -Method Post
    Show-Result -Response $seedResult -TestName "Seed Credit Cards"
} catch {
    Write-Host "Error seeding cards: $_" -ForegroundColor Red
    Write-Host "Make sure the Azure Function is running: func start" -ForegroundColor Yellow
    exit
}

Write-Host "Step 2: Getting all credit cards..." -ForegroundColor Green
try {
    $cardsResult = Invoke-RestMethod -Uri "$baseUrl/cards" -Method Get
    Show-Result -Response $cardsResult -TestName "Get All Cards"
} catch {
    Write-Host "Error getting cards: $_" -ForegroundColor Red
}

Write-Host "Step 3: Testing successful transfer..." -ForegroundColor Green
try {
    $transferBody = @{
        fromCardNumber = "4111111111111111"  # John Doe - $5,000
        toCardNumber = "5555555555554444"    # Jane Smith - $3,500
        amount = 250.00
        currency = "USD"
    } | ConvertTo-Json

    $transferResult = Invoke-RestMethod -Uri "$baseUrl/transfer" `
        -Method Post `
        -Body $transferBody `
        -ContentType "application/json"
    
    Show-Result -Response $transferResult -TestName "Transfer $250 from John to Jane"
} catch {
    Write-Host "Error during transfer: $_" -ForegroundColor Red
}

Write-Host "Step 4: Testing insufficient funds..." -ForegroundColor Green
try {
    $insufficientBody = @{
        fromCardNumber = "4000000000000010"  # David Lee - $25
        toCardNumber = "5555555555554444"    # Jane Smith
        amount = 100.00
        currency = "USD"
    } | ConvertTo-Json

    $insufficientResult = Invoke-RestMethod -Uri "$baseUrl/transfer" `
        -Method Post `
        -Body $insufficientBody `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Show-Result -Response $insufficientResult -TestName "Transfer with insufficient funds"
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Expected error caught:" -ForegroundColor Yellow
    $errorResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""
}

Write-Host "Step 5: Testing invalid card..." -ForegroundColor Green
try {
    $invalidBody = @{
        fromCardNumber = "9999999999999999"  # Non-existent card
        toCardNumber = "5555555555554444"
        amount = 50.00
        currency = "USD"
    } | ConvertTo-Json

    $invalidResult = Invoke-RestMethod -Uri "$baseUrl/transfer" `
        -Method Post `
        -Body $invalidBody `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Show-Result -Response $invalidResult -TestName "Transfer with invalid card"
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Expected error caught:" -ForegroundColor Yellow
    $errorResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""
}

Write-Host "Step 6: Testing blocked card..." -ForegroundColor Green
try {
    $blockedBody = @{
        fromCardNumber = "4242424242424242"  # Frank Miller - Blocked
        toCardNumber = "5555555555554444"
        amount = 50.00
        currency = "USD"
    } | ConvertTo-Json

    $blockedResult = Invoke-RestMethod -Uri "$baseUrl/transfer" `
        -Method Post `
        -Body $blockedBody `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Show-Result -Response $blockedResult -TestName "Transfer with blocked card"
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Expected error caught:" -ForegroundColor Yellow
    $errorResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""
}

Write-Host "Step 7: View updated card balances..." -ForegroundColor Green
try {
    $updatedCards = Invoke-RestMethod -Uri "$baseUrl/cards" -Method Get
    Write-Host "Updated Card Balances:" -ForegroundColor Yellow
    
    foreach ($card in $updatedCards.cards) {
        Write-Host "  $($card.cardHolderName) ($($card.cardNumberMasked)): $($card.balance) $($card.cardType)" -ForegroundColor White
    }
    Write-Host ""
} catch {
    Write-Host "Error getting updated cards: $_" -ForegroundColor Red
}

Write-Host "Step 8: View processed transactions..." -ForegroundColor Green
try {
    $transactions = Invoke-RestMethod -Uri "$baseUrl/processed-transactions" -Method Get
    Write-Host "Recent Transactions:" -ForegroundColor Yellow
    
    if ($transactions.transactions) {
        $transactions.transactions | Select-Object -First 5 | ForEach-Object {
            Write-Host "  [$($_.authorizationStatus)] $($_.cardNumberMasked) - $($_.amount) $($_.currency) - $($_.processingMessage)" -ForegroundColor White
        }
    }
    Write-Host ""
} catch {
    Write-Host "Error getting transactions: $_" -ForegroundColor Red
}

Write-Host "=== Testing Complete ===" -ForegroundColor Cyan
