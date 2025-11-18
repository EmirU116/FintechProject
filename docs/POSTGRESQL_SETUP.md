# Quick Start Guide - PostgreSQL Integration

## Overview

Your Fintech project now has PostgreSQL database integration to store successful Service Bus Queue transactions. The setup is modular and easy to configure for local development.

## Architecture

```
ProcessPayment (HTTP) 
    ↓
Service Bus Queue
    ↓
SettleTransaction (Trigger)
    ↓
PostgreSQL Database (Local)
```

## What Was Added

### 1. **Database Models**
- `ProcessedTransaction.cs` - Entity model for storing transaction data in PostgreSQL

### 2. **Database Context**
- `ApplicationDbContext.cs` - EF Core DbContext with proper table mappings and indexes

### 3. **Repository Pattern**
- `ITransactionRepository.cs` - Interface for database operations
- `TransactionRepository.cs` - Implementation with async methods

### 4. **Azure Functions**
- `SettleTransaction.cs` - Updated to save successful transactions to database
- `GetProcessedTransactions.cs` - NEW: HTTP endpoint to retrieve all processed transactions

### 5. **Configuration**
- `Program.cs` - DI setup for DbContext and Repository
- `local.settings.json` - PostgreSQL connection string
- `Functions.csproj` - Added Npgsql.EntityFrameworkCore.PostgreSQL package

## Setup Steps

### 1. Install PostgreSQL Locally

**Windows:**
- Download from: https://www.postgresql.org/download/windows/
- Use default port: 5432
- Set password: `postgres` (or update in `local.settings.json`)

**Docker (Recommended for quick setup):**
```bash
docker run --name fintech-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=fintech_db -p 5432:5432 -d postgres:16
```

### 2. Create Database and Table

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE fintech_db;

# Exit and run setup script
psql -U postgres -d fintech_db -f database/setup.sql
```

### 3. Update Connection String (if needed)

Edit `src/Functions/local.settings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=fintech_db;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 4. Build and Run

```bash
# Navigate to Functions directory
cd src/Functions

# Restore packages
dotnet restore

# Build project
dotnet build

# Run Azure Functions
func start
```

## Testing the Integration

### 1. Send a Payment Request

```bash
curl -X POST http://localhost:7071/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber": "4532015112830366",
    "amount": 99.99,
    "currency": "USD"
  }'
```

### 2. Check Database

```sql
-- View all processed transactions
SELECT * FROM processed_transactions ORDER BY processed_at DESC;

-- Count transactions
SELECT COUNT(*) FROM processed_transactions;

-- View today's transactions
SELECT * FROM processed_transactions 
WHERE processed_at >= CURRENT_DATE 
ORDER BY processed_at DESC;
```

### 3. Query via HTTP Endpoint

```bash
curl http://localhost:7071/api/GetProcessedTransactions
```

## Database Schema

Table: `processed_transactions`

| Column | Type | Description |
|--------|------|-------------|
| id | SERIAL | Auto-increment primary key |
| transaction_id | VARCHAR(100) | Unique transaction identifier |
| card_number_masked | VARCHAR(50) | Masked card number (****-****-****-1234) |
| amount | DECIMAL(18,2) | Transaction amount |
| currency | VARCHAR(3) | Currency code (USD, EUR, etc.) |
| transaction_timestamp | TIMESTAMP | Original transaction time |
| processed_at | TIMESTAMP | When saved to database |
| authorization_status | VARCHAR(50) | Authorization result |
| processing_message | VARCHAR(500) | Processing details |

**Indexes:**
- `idx_transaction_id` - For fast transaction lookups
- `idx_processed_at` - For date-range queries

## Code Structure

```
src/
├── Core/
│   ├── Transaction.cs              # Transaction model
│   ├── ProcessedTransaction.cs     # Database entity
│   └── Database/
│       ├── ApplicationDbContext.cs      # EF Core context
│       ├── ITransactionRepository.cs    # Repository interface
│       └── TransactionRepository.cs     # Repository implementation
└── Functions/
    ├── ProcessPayment.cs                # HTTP trigger
    ├── SettleTransaction.cs             # Service Bus trigger (saves to DB)
    ├── GetProcessedTransactions.cs      # HTTP endpoint to query DB
    └── Program.cs                       # DI configuration
```

## Key Features

✅ **Modular Design** - Repository pattern separates data access from business logic  
✅ **Dependency Injection** - Services registered in `Program.cs`  
✅ **Async/Await** - All database operations are asynchronous  
✅ **Error Handling** - Comprehensive logging and exception handling  
✅ **Indexed Tables** - Optimized for query performance  
✅ **Type Safety** - Strong typing with Entity Framework Core  
✅ **Local Development** - No cloud resources needed  

## Useful Commands

```bash
# Check PostgreSQL status
pg_isready

# Connect to database
psql -U postgres -d fintech_db

# View tables
\dt

# Describe table structure
\d processed_transactions

# Count records
SELECT COUNT(*) FROM processed_transactions;

# Drop table (if needed to recreate)
DROP TABLE processed_transactions;
```

## Troubleshooting

### Connection Failed
- Verify PostgreSQL is running: `pg_isready`
- Check port 5432 is not blocked
- Verify credentials in `local.settings.json`

### Table Not Found
- Run `database/setup.sql` script
- Or use EF migrations: `dotnet ef database update`

### Build Errors
- Run `dotnet restore` in Functions directory
- Check all NuGet packages are installed

## Next Steps

1. ✅ PostgreSQL setup complete
2. ⏭️ Add more query endpoints (by date, by transaction ID)
3. ⏭️ Add database seeding for test data
4. ⏭️ Implement soft deletes or archiving
5. ⏭️ Add reporting/analytics queries

## Additional Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql Provider](https://www.npgsql.org/efcore/)
