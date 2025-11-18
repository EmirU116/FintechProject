# Asynchronous Money Transfer Flow

## Architecture Overview

The money transfer system now uses an **event-driven asynchronous architecture** with Azure Service Bus:

```
┌──────────┐      ┌─────────────────┐      ┌──────────────┐      ┌─────────────────┐      ┌──────────┐
│  Client  │─────▶│ ProcessPayment  │─────▶│ Service Bus  │─────▶│ TransferMoney   │─────▶│ Database │
│          │ POST │   (HTTP)        │ Send │    Queue     │Trigger│  (Background)   │Update│ (PostgreSQL)│
└──────────┘      └─────────────────┘      └──────────────┘      └─────────────────┘      └──────────┘
                         │                                                │
                         │ Returns 202 Accepted                          │
                         ▼                                                ▼
                    (Immediate)                                    (Async Processing)
```

## Flow Steps

### 1. Client Sends Request
Client sends a POST request to ProcessPayment endpoint with transfer details:
```json
{
  "fromCardNumber": "4111111111111111",
  "toCardNumber": "5555555555554444",
  "amount": 100.00,
  "currency": "USD"
}
```

### 2. ProcessPayment Function (HTTP Trigger)
- **Validates** the request (required fields, amount > 0)
- **Returns immediately** with HTTP 202 Accepted
- **Sends** transfer request to Service Bus queue
- Does NOT process the transfer or update database

### 3. Service Bus Queue
- Stores the transfer request message
- Provides reliability, retry logic, and dead-letter handling
- Decouples HTTP request from processing

### 4. TransferMoney Function (Service Bus Trigger)
- **Automatically triggered** when message arrives in queue
- Deserializes the transfer request
- **Executes the money transfer** via MoneyTransferService
- **Updates database**:
  - Deducts from source card balance
  - Adds to destination card balance
  - Logs transaction to processed_transactions table
- Logs success or failure

### 5. Database Update
- Credit card balances updated atomically
- Transaction audit log created
- All within a transaction for data integrity

## Benefits of This Architecture

✅ **Fast Response** - Client gets immediate acknowledgment (202 Accepted)  
✅ **Scalability** - Queue can handle high volume of requests  
✅ **Reliability** - Service Bus provides retry logic and dead-letter queue  
✅ **Decoupling** - HTTP endpoint separated from processing logic  
✅ **Monitoring** - Can track queue depth and processing rates  
✅ **Fault Tolerance** - Failed transfers can be retried automatically  

## API Usage

### Request Transfer
```bash
curl -X POST http://localhost:7071/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -d '{
    "fromCardNumber": "4111111111111111",
    "toCardNumber": "5555555555554444",
    "amount": 100.00,
    "currency": "USD"
  }'
```

### Response (202 Accepted)
```json
{
  "message": "Transfer request queued for processing",
  "fromCard": "****1111",
  "toCard": "****4444",
  "amount": 100.00,
  "currency": "USD"
}
```

## Configuration Required

### local.settings.json
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "Endpoint=sb://your-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "ConnectionStrings:PostgreSqlConnection": "Host=localhost;Database=fintech;Username=postgres;Password=yourpassword"
  }
}
```

### Service Bus Setup
1. Create Azure Service Bus namespace
2. Create queue named `transactions`
3. Copy connection string to `ServiceBusConnection` in local.settings.json

## Error Handling

### Validation Errors (400 Bad Request)
- Empty request body
- Invalid transfer data
- Missing card numbers
- Amount <= 0

### Processing Errors
- Logged by TransferMoney function
- Message stays in queue for retry
- After max retries, moved to dead-letter queue
- Can be monitored in Azure Portal

## Monitoring

### Check Queue Status
Use Azure Portal or CLI to monitor:
- Active messages in queue
- Dead-letter messages
- Processing rate
- Average processing time

### Check Logs
```bash
# View function logs
func start

# Check for transfer completions
# Look for: "Transfer completed successfully" or "Transfer failed"
```

### Check Database
```sql
-- View recent transactions
SELECT * FROM processed_transactions 
ORDER BY processed_at DESC 
LIMIT 10;

-- View current card balances
SELECT card_holder_name, balance, card_number_masked 
FROM credit_cards;
```

## Testing Locally

### Prerequisites
1. **Azure Storage Emulator** (Azurite) running
2. **PostgreSQL** database running with tables created
3. **Azure Service Bus** (can use real Azure Service Bus, no local emulator)

### Test Steps
```bash
# 1. Start functions
cd src/Functions
func start

# 2. Seed database
curl -X POST http://localhost:7071/api/seed-cards

# 3. Send transfer request
curl -X POST http://localhost:7071/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -d '{"fromCardNumber":"4111111111111111","toCardNumber":"5555555555554444","amount":100.00,"currency":"USD"}'

# 4. Check logs - should see:
# - "Transfer request validated"
# - "Processing transfer from Service Bus queue"
# - "Transfer completed successfully"

# 5. Verify balances updated
curl http://localhost:7071/api/cards
```

## Production Deployment

For production, ensure:
- Use managed identity for Service Bus authentication
- Enable Application Insights for monitoring
- Set up alerts for dead-letter queue messages
- Configure auto-scaling based on queue depth
- Implement idempotency checks for duplicate messages
- Set appropriate message TTL and max delivery count
