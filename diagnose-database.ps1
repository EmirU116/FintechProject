# Database Diagnostic Script for Fintech Project
Write-Host "=== Fintech Database Diagnostics ===" -ForegroundColor Cyan

# 1. Check PostgreSQL Service
Write-Host "`n1. Checking PostgreSQL Service..." -ForegroundColor Yellow
try {
    $pgService = Get-Service -Name "*postgres*" -ErrorAction Stop
    Write-Host "   ✓ PostgreSQL Service: $($pgService.Status)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ PostgreSQL Service: NOT FOUND" -ForegroundColor Red
    Write-Host "   Please install PostgreSQL first!" -ForegroundColor Red
    exit 1
}

# 2. Check Connection String
Write-Host "`n2. Checking Connection String..." -ForegroundColor Yellow
$settingsPath = "src\Functions\local.settings.json"
if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath | ConvertFrom-Json
    $connString = $settings.Values.PostgreSQLConnection
    Write-Host "   ✓ Connection String: $connString" -ForegroundColor Green
    
    # Parse connection string
    $connParts = @{}
    $connString -split ';' | ForEach-Object {
        $parts = $_ -split '=', 2
        if ($parts.Count -eq 2) {
            $connParts[$parts[0]] = $parts[1]
        }
    }
    Write-Host "   Host: $($connParts['Host'])" -ForegroundColor Cyan
    Write-Host "   Port: $($connParts['Port'])" -ForegroundColor Cyan
    Write-Host "   Database: $($connParts['Database'])" -ForegroundColor Cyan
    Write-Host "   Username: $($connParts['Username'])" -ForegroundColor Cyan
} else {
    Write-Host "   ✗ local.settings.json not found!" -ForegroundColor Red
}

# 3. Test Database Connection with psql (if available)
Write-Host "`n3. Testing Database Connection..." -ForegroundColor Yellow
$pgPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
if (Test-Path $pgPath) {
    Write-Host "   ✓ Found psql at: $pgPath" -ForegroundColor Green
    
    # Set password environment variable temporarily
    $env:PGPASSWORD = "Serap.1989"
    
    Write-Host "   Testing connection..." -ForegroundColor Cyan
    $testQuery = "SELECT version();"
    & $pgPath -h localhost -U postgres -d postgres -c $testQuery 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Connection successful!" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Connection failed! Check username/password" -ForegroundColor Red
    }
    
    # Check if tables exist
    Write-Host "`n4. Checking Database Tables..." -ForegroundColor Yellow
    $checkTables = @"
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('processed_transactions', 'credit_cards');
"@
    
    $result = & $pgPath -h localhost -U postgres -d postgres -t -c $checkTables 2>&1
    
    if ($result -match "processed_transactions") {
        Write-Host "   ✓ Table 'processed_transactions' exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Table 'processed_transactions' NOT FOUND" -ForegroundColor Red
        Write-Host "   Run: database\setup.sql to create it" -ForegroundColor Yellow
    }
    
    if ($result -match "credit_cards") {
        Write-Host "   ✓ Table 'credit_cards' exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Table 'credit_cards' NOT FOUND" -ForegroundColor Red
        Write-Host "   Run: database\add_credit_cards_table.sql to create it" -ForegroundColor Yellow
    }
    
    # Clear password
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    
} else {
    Write-Host "   ⚠ psql not found at default location" -ForegroundColor Yellow
    Write-Host "   Trying to find psql in PATH..." -ForegroundColor Cyan
    $psqlCmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($psqlCmd) {
        Write-Host "   ✓ Found psql in PATH: $($psqlCmd.Source)" -ForegroundColor Green
    } else {
        Write-Host "   ✗ psql not found in PATH" -ForegroundColor Red
        Write-Host "   Add PostgreSQL to PATH: C:\Program Files\PostgreSQL\17\bin" -ForegroundColor Yellow
    }
}

# 5. Check Azure Functions Core Tools
Write-Host "`n5. Checking Azure Functions Core Tools..." -ForegroundColor Yellow
$funcCmd = Get-Command func -ErrorAction SilentlyContinue
if ($funcCmd) {
    Write-Host "   ✓ Azure Functions Core Tools installed" -ForegroundColor Green
    $funcVersion = & func --version
    Write-Host "   Version: $funcVersion" -ForegroundColor Cyan
} else {
    Write-Host "   ✗ Azure Functions Core Tools NOT FOUND" -ForegroundColor Red
    Write-Host "   Install: npm install -g azure-functions-core-tools@4" -ForegroundColor Yellow
}

Write-Host "`n=== Diagnostics Complete ===" -ForegroundColor Cyan
Write-Host ""
