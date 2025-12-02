# setup-database.ps1
# Complete database setup for Fintech project

$ErrorActionPreference = "Stop"

Write-Host "=== Setting up PostgreSQL Database ===" -ForegroundColor Cyan

# Configuration
$pgHost = "localhost"
$pgPort = "5432"
$pgUser = "postgres"
$pgPassword = "Serap.1989"
$pgDatabase = "postgres"

# Try to find psql.exe
$psqlPath = $null
$possiblePaths = @(
    "D:\Database\PostSQL\bin\psql.exe",
    "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $psqlPath = $path
        Write-Host "Found psql at: $psqlPath" -ForegroundColor Gray
        break
    }
}

if (-not $psqlPath) {
    Write-Host "✗ Could not find psql.exe. Please install PostgreSQL or update the path." -ForegroundColor Red
    exit 1
}

# Set password environment variable
$env:PGPASSWORD = $pgPassword

Write-Host "`n1. Testing PostgreSQL connection..." -ForegroundColor Yellow
try {
    & $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c "SELECT version();" | Out-Null
    Write-Host "   ✓ Connection successful!" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Failed to connect to PostgreSQL" -ForegroundColor Red
    Write-Host "   Make sure PostgreSQL is running and credentials are correct" -ForegroundColor Red
    exit 1
}

Write-Host "`n2. Creating credit_cards table..." -ForegroundColor Yellow
$creditCardsSql = @"
CREATE TABLE IF NOT EXISTS credit_cards (
    id SERIAL PRIMARY KEY,
    card_number VARCHAR(20) NOT NULL UNIQUE,
    card_holder_name VARCHAR(100) NOT NULL,
    balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    card_type VARCHAR(50) NOT NULL,
    expiry_date TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT chk_balance CHECK (balance >= 0)
);

CREATE INDEX IF NOT EXISTS idx_card_number ON credit_cards(card_number);
"@

& $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c $creditCardsSql
Write-Host "   ✓ credit_cards table created" -ForegroundColor Green

Write-Host "`n3. Creating processed_transactions table..." -ForegroundColor Yellow
& $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -f "database\setup.sql"
Write-Host "   ✓ processed_transactions table created" -ForegroundColor Green

Write-Host "`n4. Creating audit_events, fraud_alerts, and transaction_metrics tables..." -ForegroundColor Yellow
& $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -f "database\add_new_tables.sql"
Write-Host "   ✓ New tables created" -ForegroundColor Green

Write-Host "`n5. Seeding test credit cards..." -ForegroundColor Yellow
$seedSql = @"
INSERT INTO credit_cards (card_number, card_holder_name, balance, card_type, expiry_date, is_active) VALUES
('4111-1111-1111-1111', 'John Doe', 10000.00, 'Visa', '2026-12-31', true),
('5555-5555-5555-4444', 'Jane Smith', 5000.00, 'Mastercard', '2026-12-31', true),
('3782-822463-10005', 'Bob Johnson', 7500.00, 'Amex', '2026-12-31', true),
('6011-1111-1111-1117', 'Alice Williams', 3000.00, 'Discover', '2026-12-31', true)
ON CONFLICT (card_number) DO NOTHING;
"@

& $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c $seedSql
Write-Host "   ✓ Test cards seeded" -ForegroundColor Green

Write-Host "`n6. Verifying tables..." -ForegroundColor Yellow
$tables = & $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c "\dt" -t
Write-Host "$tables"

Write-Host "`n7. Checking credit cards data..." -ForegroundColor Yellow
$cards = & $psqlPath -h $pgHost -p $pgPort -U $pgUser -d $pgDatabase -c "SELECT card_number, card_holder_name, balance FROM credit_cards;" -t
Write-Host "$cards"

Write-Host "`n=== Database Setup Complete! ===" -ForegroundColor Green
Write-Host "`nYou can now run:" -ForegroundColor Cyan
Write-Host "  cd src\Functions" -ForegroundColor White
Write-Host "  func start" -ForegroundColor White
Write-Host "`nThen test with:" -ForegroundColor Cyan
Write-Host "  .\test-transfer.ps1" -ForegroundColor White

# Clear password from environment
Remove-Item Env:\PGPASSWORD
