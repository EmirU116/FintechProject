# Money Transfer Feature

This feature enables transferring money between credit card accounts stored in a PostgreSQL database.

## Overview

The money transfer system consists of:
- **MoneyTransferService**: Core business logic for handling transfers
- **CreditCardRepository**: Database operations for credit card accounts
- **TransferMoney Function**: Azure Function HTTP endpoint for transfer requests
- **SeedCreditCards Function**: Initialize database with test credit cards
- **GetCreditCards Function**: View all credit cards and their balances

## Database Setup

### 1. Create the Credit Cards Table

Run the SQL migration script to create the `credit_cards` table:

```bash
psql -U your_username -d your_database -f database/add_credit_cards_table.sql
```

Or manually execute:

```sql
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
```

### 2. Seed Test Data

Use the `SeedCreditCards` function to populate the database with test cards:

```bash
curl -X POST http://localhost:7071/api/seed-cards
```

## API Endpoints

### 1. Transfer Money

**Endpoint**: `POST /api/transfer`

**Request Body**:
```json
{
  "fromCardNumber": "4111111111111111",
  "toCardNumber": "5555555555554444",
  "amount": 100.50,
  "currency": "USD"
}
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "Successfully transferred $100.50 from ****-1111 to ****-4444",
  "transactionId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "fromAccountNewBalance": 4899.50,
  "toAccountNewBalance": 3600.50,
  "transferTimestamp": "2025-11-18T10:30:00Z"
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Insufficient funds. Available balance: $50.00",
  "transactionId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "transferTimestamp": "2025-11-18T10:30:00Z"
}
```

### 2. Get All Credit Cards

**Endpoint**: `GET /api/cards`

**Response** (200 OK):
```json
{
  "success": true,
  "count": 9,
  "cards": [
    {
      "cardNumberMasked": "****-****-****-1111",
      "cardHolderName": "John Doe",
      "balance": 5000.00,
      "cardType": "Visa",
      "expiryDate": "2027-11-18T00:00:00Z",
      "isActive": true
    }
  ]
}
```

### 3. Seed Credit Cards

**Endpoint**: `POST /api/seed-cards`

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Database seeding completed. 9 cards added, 0 cards skipped.",
  "seededCount": 9,
  "skippedCount": 0,
  "totalCards": 9
}
```

## Test Credit Cards

### High Balance Cards (Successful Transfers)
- **4111111111111111** - John Doe - $5,000.00 (Visa)
- **5555555555554444** - Jane Smith - $3,500.00 (Mastercard)
- **378282246310005** - Bob Johnson - $10,000.00 (Amex)

### Medium Balance Cards
- **4000000000000002** - Alice Brown - $250.00 (Visa)
- **5105105105105100** - Charlie Wilson - $750.00 (Mastercard)

### Low Balance Cards (Insufficient Funds)
- **4000000000000010** - David Lee - $25.00 (Visa)
- **4000000000000051** - Emma Davis - $10.50 (Visa)

### Declined Cards
- **4242424242424242** - Frank Miller - $1,000.00 (Blocked)
- **4000000000000069** - Grace Taylor - $500.00 (Expired)

## Transfer Validation Rules

The system validates:
1. ✅ Amount must be greater than zero
2. ✅ Source card must exist in database
3. ✅ Source card must be active
4. ✅ Source card must not be expired
5. ✅ Source card must have sufficient balance
6. ✅ Destination card must exist in database
7. ✅ Destination card must be active
8. ✅ Cannot transfer to the same card

## Transaction Logging

All transfer attempts (successful and failed) are logged to the `processed_transactions` table with:
- Transaction ID (unique identifier)
- Masked card numbers
- Amount and currency
- Timestamp
- Authorization status
- Processing message

## Example Usage

### Using cURL

```bash
# Transfer $100 from John Doe to Jane Smith
curl -X POST http://localhost:7071/api/transfer \
  -H "Content-Type: application/json" \
  -d '{
    "fromCardNumber": "4111111111111111",
    "toCardNumber": "5555555555554444",
    "amount": 100.00,
    "currency": "USD"
  }'

# View all cards and balances
curl http://localhost:7071/api/cards

# Seed the database with test cards
curl -X POST http://localhost:7071/api/seed-cards
```

### Using PowerShell

```powershell
# Transfer money
$body = @{
    fromCardNumber = "4111111111111111"
    toCardNumber = "5555555555554444"
    amount = 100.00
    currency = "USD"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/transfer" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"

# Get all cards
Invoke-RestMethod -Uri "http://localhost:7071/api/cards" -Method Get

# Seed database
Invoke-RestMethod -Uri "http://localhost:7071/api/seed-cards" -Method Post
```

## Architecture

```
TransferMoney Function (HTTP Trigger)
    ↓
MoneyTransferService
    ↓
┌───────────────────┴────────────────────┐
↓                                        ↓
CreditCardRepository              TransactionRepository
    ↓                                        ↓
┌───┴────┐                           ┌──────┴────────┐
│ Cards  │                           │ Transactions  │
│ Table  │                           │ Table         │
└────────┘                           └───────────────┘
```

## Error Handling

All errors are:
- Logged with detailed information
- Returned as JSON responses with appropriate HTTP status codes
- Recorded in the database as failed transactions
- Include transaction IDs for tracking

## Security Considerations

For production use, consider:
- Add authentication/authorization to endpoints
- Implement rate limiting
- Use encrypted connections to database
- Mask full card numbers in responses
- Add fraud detection mechanisms
- Implement transaction reversals
- Add audit logging
