# Azure Credentials Setup Script for GitHub Actions
# This script creates a service principal and outputs the credentials needed for GitHub Secrets

param(
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [string]$ServicePrincipalName = "github-actions-fintech",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "fintech-rg"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Azure Service Principal Setup for GitHub Actions" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "✗ Azure CLI is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Login to Azure
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json

if (-not $account) {
    Write-Host "Not logged in to Azure. Initiating login..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "  Subscription: $($account.name) ($($account.id))" -ForegroundColor Gray

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host ""
    Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
    $account = az account show | ConvertFrom-Json
    Write-Host "✓ Subscription set: $($account.name)" -ForegroundColor Green
}

$subscriptionId = $account.id

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Creating Service Principal..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Name: $ServicePrincipalName" -ForegroundColor Gray
Write-Host "Scope: Subscription $subscriptionId" -ForegroundColor Gray
Write-Host ""

# Create service principal
try {
    $sp = az ad sp create-for-rbac `
        --name $ServicePrincipalName `
        --role contributor `
        --scopes "/subscriptions/$subscriptionId" `
        --sdk-auth | ConvertFrom-Json
    
    Write-Host "✓ Service Principal created successfully!" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to create service principal" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "GitHub Secrets Configuration" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Convert to JSON string for GitHub secret
$credentials = @{
    clientId = $sp.clientId
    clientSecret = $sp.clientSecret
    subscriptionId = $sp.subscriptionId
    tenantId = $sp.tenantId
} | ConvertTo-Json -Compress

Write-Host "Add the following secrets to your GitHub repository:" -ForegroundColor Yellow
Write-Host "(Settings → Secrets and variables → Actions → New repository secret)" -ForegroundColor Gray
Write-Host ""

Write-Host "1. AZURE_CREDENTIALS" -ForegroundColor Cyan
Write-Host "   Copy the entire JSON below:" -ForegroundColor Gray
Write-Host "   ----------------------------------------"
Write-Host $credentials -ForegroundColor White
Write-Host "   ----------------------------------------"
Write-Host ""

Write-Host "2. POSTGRES_CONNECTION_STRING (OPTIONAL - for Azure PostgreSQL)" -ForegroundColor Cyan
Write-Host "   ⚠️  SKIP THIS if using local PostgreSQL database" -ForegroundColor Yellow
Write-Host "   Format: postgresql://username:password@hostname:5432/database" -ForegroundColor Gray
Write-Host "   Example: postgresql://fintech_user:mypassword@myserver.postgres.database.azure.com:5432/fintech_db" -ForegroundColor Gray
Write-Host ""

Write-Host "3. POSTGRES_PASSWORD (OPTIONAL - for Azure PostgreSQL)" -ForegroundColor Cyan
Write-Host "   ⚠️  SKIP THIS if using local PostgreSQL database" -ForegroundColor Yellow
Write-Host "   Your PostgreSQL database password (only needed for Azure PostgreSQL)" -ForegroundColor Gray
Write-Host ""

Write-Host "4. STORAGE_ACCOUNT_NAME (Optional)" -ForegroundColor Cyan
Write-Host "   Your Azure Storage Account name (if different from auto-generated)" -ForegroundColor Gray
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Service Principal Details (Save these!)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Principal Name: $ServicePrincipalName" -ForegroundColor White
Write-Host "Application (Client) ID: $($sp.clientId)" -ForegroundColor White
Write-Host "Tenant ID: $($sp.tenantId)" -ForegroundColor White
Write-Host "Subscription ID: $($sp.subscriptionId)" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Keep the clientSecret secure! It will not be shown again." -ForegroundColor Yellow
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Test Service Principal Login" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Test login
Write-Host "Testing service principal login..." -ForegroundColor Yellow
try {
    az login --service-principal `
        --username $sp.clientId `
        --password $sp.clientSecret `
        --tenant $sp.tenantId `
        --output none
    
    Write-Host "✓ Service principal login successful!" -ForegroundColor Green
    
    # Switch back to user account
    az login --output none 2>$null
    az account set --subscription $subscriptionId --output none
    
} catch {
    Write-Host "✗ Service principal login failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Copy the AZURE_CREDENTIALS JSON above" -ForegroundColor White
Write-Host "2. Go to your GitHub repository → Settings → Secrets and variables → Actions" -ForegroundColor White
Write-Host "3. Click 'New repository secret'" -ForegroundColor White
Write-Host "4. Name: AZURE_CREDENTIALS" -ForegroundColor White
Write-Host "5. Value: Paste the JSON" -ForegroundColor White
Write-Host "6. Add other required secrets (PostgreSQL connection, etc.)" -ForegroundColor White
Write-Host "7. Push to main branch to trigger deployment!" -ForegroundColor White
Write-Host ""
Write-Host "For detailed setup instructions, see: CICD_SETUP.md" -ForegroundColor Yellow
Write-Host ""
Write-Host "✓ Setup complete!" -ForegroundColor Green
Write-Host ""

# Save to file for reference
$outputFile = "azure-credentials-output.txt"
$output = @"
================================================
Azure Service Principal for GitHub Actions
Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
================================================

Service Principal Name: $ServicePrincipalName
Application (Client) ID: $($sp.clientId)
Tenant ID: $($sp.tenantId)
Subscription ID: $($sp.subscriptionId)

================================================
GitHub Secret: AZURE_CREDENTIALS
================================================
$credentials

================================================
IMPORTANT
================================================
- Keep this information secure
- The client secret cannot be retrieved again
- Add this as AZURE_CREDENTIALS secret in GitHub
- Also add PostgreSQL connection string and other secrets
- Delete this file after copying to GitHub: $outputFile
"@

$output | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "⚠️  Credentials saved to: $outputFile" -ForegroundColor Yellow
Write-Host "   Please delete this file after copying to GitHub!" -ForegroundColor Yellow
Write-Host ""
