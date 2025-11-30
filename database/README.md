# PostgreSQL Database Setup Guide

Quick setup guide for PostgreSQL database used by the Fintech Payment Platform.

## Prerequisites

- PostgreSQL 14+ installed locally or Azure Database for PostgreSQL
- psql command-line tool
- pgAdmin (optional, for GUI management)

## Installation

### Windows

1. Download PostgreSQL from: https://www.postgresql.org/download/windows/
2. Run the installer and follow the setup wizard
3. During installation:
   - Set a password for the `postgres` superuser (default in config: `postgres`)
   - Set port to `5432` (default)
   - Install Stack Builder components if needed

### macOS

```bash
# Using Homebrew
brew install postgresql@16
brew services start postgresql@16
```

### Linux (Ubuntu/Debian)

```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
```

## Database Setup

### Option 1: Using psql Command Line

1. Open terminal/command prompt
2. Connect to PostgreSQL:

```bash
psql -U postgres
```

3. Create the database:

```sql
CREATE DATABASE fintech_db;
```

4. Connect to the new database:

```sql
\c fintech_db
```

5. Run the setup script:

```bash
psql -U postgres -d fintech_db -f database/setup.sql
```

### Option 2: Using pgAdmin

1. Open pgAdmin
2. Connect to your PostgreSQL server
3. Right-click on "Databases" → "Create" → "Database"
4. Name it `fintech_db` and save
5. Right-click on `fintech_db` → "Query Tool"
6. Copy and paste the contents of `database/setup.sql`
7. Execute the script

## Configuration

The connection string is configured in `local.settings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=fintech_db;Username=postgres;Password=postgres"
  }
}
```

**Important:** Update the password in the connection string to match your PostgreSQL setup.

## Verify Setup

Check if the table was created:

```sql
\dt
-- or
SELECT * FROM processed_transactions;
```

## Using Entity Framework Core Migrations (Alternative)

If you prefer using EF Core migrations:

1. Install EF Core tools globally:

```bash
dotnet tool install --global dotnet-ef
```

2. Navigate to the Functions project:

```bash
cd src/Functions
```

3. Create initial migration:

```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```

4. Apply migration to database:

```bash
dotnet ef database update --context ApplicationDbContext
```

## Database Schema

The `processed_transactions` table stores all successfully processed transactions:

| Column | Type | Description |
|--------|------|-------------|
| id | SERIAL | Auto-incrementing primary key |
| transaction_id | VARCHAR(100) | Unique transaction identifier |
| card_number_masked | VARCHAR(50) | Masked card number |
| amount | DECIMAL(18,2) | Transaction amount |
| currency | VARCHAR(3) | Currency code (USD, EUR, etc.) |
| transaction_timestamp | TIMESTAMP | Original transaction time |
| processed_at | TIMESTAMP | When the transaction was processed |
| authorization_status | VARCHAR(50) | Authorization status |
| processing_message | VARCHAR(500) | Processing result message |

## Querying Data

View all processed transactions:

```sql
SELECT * FROM processed_transactions ORDER BY processed_at DESC;
```

View transactions by date:

```sql
SELECT * FROM processed_transactions 
WHERE processed_at >= CURRENT_DATE 
ORDER BY processed_at DESC;
```

Get transaction count:

```sql
SELECT COUNT(*) FROM processed_transactions;
```

## Troubleshooting

### Connection Issues

- Verify PostgreSQL is running: `pg_isready`
- Check if port 5432 is open and not in use
- Ensure firewall allows connections to PostgreSQL

### Authentication Issues

- Verify username and password in connection string
- Check `pg_hba.conf` for authentication settings
- Restart PostgreSQL after configuration changes

### Permission Issues

```sql
-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE fintech_db TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
```

## Docker Alternative (Optional)

If you prefer using Docker for PostgreSQL:

```bash
docker run --name fintech-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=fintech_db -p 5432:5432 -d postgres:16

# Run the setup script
docker exec -i fintech-postgres psql -U postgres -d fintech_db < database/setup.sql
```

## Next Steps

1. Restore NuGet packages: `dotnet restore`
2. Build the project: `dotnet build`
3. Run Azure Functions locally: `func start`
4. Send test transactions and verify they're stored in PostgreSQL
