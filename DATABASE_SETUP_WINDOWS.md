# Database Setup Guide - Windows

Since `psql` is not recognized in your PATH, here are alternative ways to set up the credit_cards table:

## Option 1: Using pgAdmin (Recommended for Windows)

1. Open **pgAdmin** (installed with PostgreSQL)
2. Connect to your PostgreSQL server
3. Navigate to your database
4. Right-click and select **Query Tool**
5. Copy and paste the SQL from `database/add_credit_cards_table.sql`
6. Click the **Execute** button (▶️ icon) or press F5

## Option 2: Add psql to PATH

If you want to use `psql` from PowerShell:

1. Find your PostgreSQL installation directory (typically):
   - `C:\Program Files\PostgreSQL\15\bin`
   - `C:\Program Files\PostgreSQL\14\bin`
   - `C:\Program Files\PostgreSQL\16\bin`

2. Add to PATH temporarily (current session):
   ```powershell
   $env:PATH += ";C:\Program Files\PostgreSQL\15\bin"
   ```

3. Or add permanently:
   ```powershell
   # Run as Administrator
   [Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Program Files\PostgreSQL\15\bin", "Machine")
   ```

4. Verify:
   ```powershell
   psql --version
   ```

5. Then run:
   ```powershell
   psql -U your_username -d your_database -f database/add_credit_cards_table.sql
   ```

## Option 3: Using Azure Data Studio

1. Install **Azure Data Studio** (if not installed)
2. Connect to your PostgreSQL server
3. Create a new query
4. Paste the SQL from `database/add_credit_cards_table.sql`
5. Execute the query

## Option 4: Use PowerShell with Npgsql

Create and run this PowerShell script:

```powershell
# Install Npgsql package (run once)
Install-Package Npgsql -Force -Scope CurrentUser

# Connection details
$server = "localhost"
$port = "5432"
$database = "your_database"
$username = "your_username"
$password = "your_password"

$connectionString = "Host=$server;Port=$port;Database=$database;Username=$username;Password=$password"

# Read SQL file
$sqlScript = Get-Content "database/add_credit_cards_table.sql" -Raw

# Execute
Add-Type -Path "$env:USERPROFILE\.nuget\packages\npgsql\*\lib\net8.0\Npgsql.dll"
$conn = New-Object Npgsql.NpgsqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlScript
$cmd.ExecuteNonQuery()
$conn.Close()

Write-Host "Table created successfully!" -ForegroundColor Green
```

## Option 5: Use the Azure Functions Approach

Since you're using Entity Framework Core, you can create the table programmatically:

1. Start your Azure Functions app
2. The table should be auto-created if you have migrations enabled
3. Call the seed endpoint to populate data:
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:7071/api/seed-cards" -Method Post
   ```

## Quick Setup - Copy/Paste SQL

If you prefer, here's the complete SQL to run in any PostgreSQL client:

```sql
-- Create credit_cards table
CREATE TABLE IF NOT EXISTS credit_cards (
    id SERIAL PRIMARY KEY,
    card_number VARCHAR(20) NOT NULL,
    card_holder_name VARCHAR(100) NOT NULL,
    balance DECIMAL(18,2) NOT NULL,
    card_type VARCHAR(50) NOT NULL,
    expiry_date TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT idx_card_number UNIQUE (card_number)
);

CREATE INDEX IF NOT EXISTS idx_card_number ON credit_cards(card_number);

-- Insert test data
INSERT INTO credit_cards (card_number, card_holder_name, balance, card_type, expiry_date, is_active)
VALUES 
    ('4111111111111111', 'John Doe', 5000.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('5555555555554444', 'Jane Smith', 3500.00, 'Mastercard', NOW() + INTERVAL '3 years', TRUE),
    ('378282246310005', 'Bob Johnson', 10000.00, 'American Express', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000002', 'Alice Brown', 250.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('5105105105105100', 'Charlie Wilson', 750.00, 'Mastercard', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000010', 'David Lee', 25.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('4000000000000051', 'Emma Davis', 10.50, 'Visa', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000069', 'Grace Taylor', 500.00, 'Visa', NOW() - INTERVAL '6 months', TRUE),
    ('4242424242424242', 'Frank Miller', 1000.00, 'Visa', NOW() + INTERVAL '2 years', FALSE)
ON CONFLICT (card_number) DO NOTHING;
```

## Recommended Next Steps

1. **Use pgAdmin** (easiest for Windows) - it's likely already installed with PostgreSQL
2. Copy the SQL from above
3. Execute it in the Query Tool
4. Start your Azure Functions: `func start`
5. Run the test script: `.\test-transfer.ps1`

The test script will verify the setup and show you the money transfer in action!
