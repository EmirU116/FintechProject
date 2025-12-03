# Fix Database Connection Issues

## Problem Detected
PostgreSQL password authentication is failing for user 'postgres'. This is why your ProcessPayment function is getting database errors.

## Solutions (Try in order)

### Option 1: Reset PostgreSQL Password
1. Open **pgAdmin 4** (should be installed with PostgreSQL)
2. Right-click on **PostgreSQL 17** → **Properties**
3. Go to **Connection** tab
4. Set password to: `Serap.1989`
5. Click **Save**

### Option 2: Update Connection String
If your actual PostgreSQL password is different, update `local.settings.json`:

```json
"PostgreSQLConnection": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"
```

### Option 3: Use Windows Authentication (Recommended for Development)
1. Open: `C:\Program Files\PostgreSQL\17\data\pg_hba.conf`
2. Find the line that says:
   ```
   host    all             all             127.0.0.1/32            scram-sha-256
   ```
3. Change to:
   ```
   host    all             all             127.0.0.1/32            trust
   ```
4. Restart PostgreSQL service:
   ```powershell
   Restart-Service postgresql-x64-17
   ```

### Option 4: Check Actual Password
Your PostgreSQL password might be different. Try these common defaults:
- Empty password (just press Enter)
- `postgres`
- `admin`
- The password you set during installation

To test manually:
```powershell
# Try with no password
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres

# Or try with password prompt
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres -W
```

## After Fixing Connection

Once you can connect, run these scripts to create the tables:

```powershell
# Set your correct password
$env:PGPASSWORD = "YOUR_PASSWORD"

# Create processed_transactions table
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres -f database/setup.sql

# Create credit_cards table
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres -f database/add_credit_cards_table.sql

# Verify tables exist
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres -c "\dt"
```

## Quick Test
Once fixed, test with:
```powershell
cd src/Functions
func start
```

Then test ProcessPayment:
```powershell
.\test-transfer.ps1
```
