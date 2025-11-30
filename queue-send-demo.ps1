#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enqueue a test transaction message to Azure Storage Queue for demo.

.DESCRIPTION
    Creates a sample transaction JSON and sends it to the 'transactions' queue.
    Uses Azure Storage connection string from environment or local.settings.json.

.PARAMETER ConnectionString
    Azure Storage connection string. If not provided, reads from local.settings.json.

.PARAMETER Amount
    Transaction amount. Default: 100.00

.PARAMETER FromCard
    Source card number. Default: test card.

.PARAMETER ToCard
    Destination card number. Default: test card.

.EXAMPLE
    .\queue-send-demo.ps1 -Amount 50.00
    .\queue-send-demo.ps1 -ConnectionString "DefaultEndpointsProtocol=https;AccountName=..."
#>

param(
    [string]$ConnectionString = "",
    [decimal]$Amount = 100.00,
    [string]$FromCard = "4532015112830366",
    [string]$ToCard = "5425233430109903"
)

# Helper: read connection string from local.settings.json if not provided
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $localSettingsPath = Join-Path $PSScriptRoot "src/Functions/local.settings.json"
    if (Test-Path $localSettingsPath) {
        $settings = Get-Content $localSettingsPath | ConvertFrom-Json
        $ConnectionString = $settings.Values.AzureWebJobsStorage
        if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
            Write-Error "AzureWebJobsStorage not found in local.settings.json. Provide -ConnectionString explicitly."
            exit 1
        }
    } else {
        Write-Error "local.settings.json not found and no -ConnectionString provided."
        exit 1
    }
}

# Build transaction message
$transaction = @{
    Id = [guid]::NewGuid().ToString()
    CardNumber = $FromCard
    CardNumberMasked = "****-****-****-$($FromCard.Substring($FromCard.Length - 4))"
    Amount = $Amount
    Currency = "USD"
    Timestamp = (Get-Date).ToUniversalTime().ToString("o")
    ToCardNumber = $ToCard
    ToCardNumberMasked = "****-****-****-$($ToCard.Substring($ToCard.Length - 4))"
}

$messageJson = $transaction | ConvertTo-Json -Compress

Write-Host "📤 Enqueueing transaction message:" -ForegroundColor Cyan
Write-Host $messageJson -ForegroundColor Yellow

# Use Azure CLI to send message to Storage Queue
try {
    $result = az storage message put `
        --queue-name "transactions" `
        --content $messageJson `
        --connection-string $ConnectionString `
        2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Message enqueued successfully to 'transactions' queue." -ForegroundColor Green
    } else {
        Write-Error "Failed to enqueue message: $result"
        exit 1
    }
} catch {
    Write-Error "Error enqueueing message: $_"
    exit 1
}
