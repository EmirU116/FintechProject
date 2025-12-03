# 📖 API Reference Documentation

Complete API documentation for the Event-Driven Fintech Payment API.

---

## 🔐 Authentication

All API endpoints require **Azure Functions authentication** via function keys.

### Authentication Methods

1. **Function Keys** (Recommended for production)
   - Obtained from Azure Portal or Azure CLI
   - Passed via `x-functions-key` header or `code` query parameter

2. **Host Keys** (Admin access)
   - Master key with access to all functions
   - Use only for administrative tasks

### How to Authenticate

**Option 1: Header (Recommended)**
```bash
curl -X POST https://your-app.azurewebsites.net/api/ProcessPayment \
  -H "x-functions-key: YOUR_FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{ ... }'
```

**Option 2: Query Parameter**
```bash
curl -X POST "https://your-app.azurewebsites.net/api/ProcessPayment?code=YOUR_FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{ ... }'
```

### Getting Function Keys

```bash
# Azure CLI
az functionapp keys list --name event-payment-func --resource-group fintech-rg

# Or from Azure Portal
# Navigate to: Function App > Functions > Function Name > Function Keys
```

---

## 📡 Base URL

- **Local Development**: `http://localhost:7071`
- **Production**: `https://your-function-app.azurewebsites.net`

---

## 💰 Payment Operations

### 1. Process Payment (Async Transfer)

Initiates an asynchronous money transfer between two credit cards. Returns immediately with HTTP 202 Accepted while the transfer is queued for processing.

**Endpoint**: `POST /api/ProcessPayment`

**Authorization**: Function Key Required

**Request Body**:
```json
{
  "fromCardNumber": "4111111111111111",
  "toCardNumber": "5555555555554444",
  "amount": 100.00,
  "currency": "USD"
}
```

**Request Schema**:
| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `fromCardNumber` | string | ✅ Yes | Source credit card number | 13-19 digits |
| `toCardNumber` | string | ✅ Yes | Destination credit card number | 13-19 digits |
| `amount` | decimal | ✅ Yes | Transfer amount | > 0 |
| `currency` | string | ⚠️ Optional | Currency code (default: USD) | USD, EUR, GBP, JPY, CAD |

**Success Response** (202 Accepted):
```json
{
  "success": true,
  "message": "Transfer request queued for processing",
  "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "traceId": "abc12345",
  "fromCard": "****-****-****-1111",
  "toCard": "****-****-****-4444",
  "amount": 100.00,
  "currency": "USD"
}
```

**Error Responses**:

| Status Code | Reason | Response Example |
|------------|--------|------------------|
| `400 Bad Request` | Empty request body | `{ "success": false, "message": "Empty request body" }` |
| `400 Bad Request` | Invalid amount | `{ "success": false, "message": "Invalid transfer data or amount must be greater than zero" }` |
| `400 Bad Request` | Missing card numbers | `{ "success": false, "message": "Source and destination card numbers are required" }` |
| `401 Unauthorized` | Missing/invalid function key | `{ "error": "Unauthorized" }` |
| `500 Internal Server Error` | System error | `{ "success": false, "message": "Error message details" }` |

**cURL Example**:
```bash
curl -X POST http://localhost:7071/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -H "x-functions-key: YOUR_FUNCTION_KEY" \
  -d '{
    "fromCardNumber": "4111111111111111",
    "toCardNumber": "5555555555554444",
    "amount": 100.00,
    "currency": "USD"
  }'
```

**PowerShell Example**:
```powershell
$body = @{
    fromCardNumber = "4111111111111111"
    toCardNumber = "5555555555554444"
    amount = 100.00
    currency = "USD"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/ProcessPayment" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -Headers @{ "x-functions-key" = "YOUR_KEY" }
```

**JavaScript/Node.js Example**:
```javascript
const response = await fetch('http://localhost:7071/api/ProcessPayment', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'x-functions-key': 'YOUR_FUNCTION_KEY'
  },
  body: JSON.stringify({
    fromCardNumber: '4111111111111111',
    toCardNumber: '5555555555554444',
    amount: 100.00,
    currency: 'USD'
  })
});

const result = await response.json();
console.log(result);
```

**Notes**:
- ⚡ This is an **asynchronous** operation
- 📨 Returns immediately with transaction ID
- 🔄 Actual processing happens via Service Bus queue
- 📊 Check transaction status via `/api/processed-transactions`
- 🎯 Transaction takes 2-5 seconds to complete

---

### 2. Get Processed Transactions

Retrieves all processed transactions with their status, amounts, and timestamps.

**Endpoint**: `GET /api/processed-transactions`

**Authorization**: Function Key Required

**Request**: No body required

**Success Response** (200 OK):
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "cardNumber": "4111111111111111",
    "cardNumberMasked": "****-****-****-1111",
    "toCardNumber": "5555555555554444",
    "toCardNumberMasked": "****-****-****-4444",
    "amount": 100.00,
    "currency": "USD",
    "status": "Success",
    "validationMessage": null,
    "processedAt": "2025-12-03T10:30:45.123Z"
  },
  {
    "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "cardNumber": "4111111111111111",
    "cardNumberMasked": "****-****-****-1111",
    "toCardNumber": "378282246310005",
    "toCardNumberMasked": "****-****-***0005",
    "amount": 50.00,
    "currency": "USD",
    "status": "Failed",
    "validationMessage": "Insufficient funds",
    "processedAt": "2025-12-03T10:25:30.456Z"
  }
]
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| `id` | guid | Unique transaction identifier |
| `cardNumber` | string | Full source card number |
| `cardNumberMasked` | string | Masked source card (last 4 digits visible) |
| `toCardNumber` | string | Full destination card number |
| `toCardNumberMasked` | string | Masked destination card |
| `amount` | decimal | Transaction amount |
| `currency` | string | Currency code |
| `status` | string | Transaction status (`Success` or `Failed`) |
| `validationMessage` | string | Error message if failed (null if successful) |
| `processedAt` | datetime | UTC timestamp of processing |

**cURL Example**:
```bash
curl -X GET http://localhost:7071/api/processed-transactions \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

**Error Responses**:
| Status Code | Reason | Response Example |
|------------|--------|------------------|
| `401 Unauthorized` | Missing/invalid function key | `{ "error": "Unauthorized" }` |
| `500 Internal Server Error` | Database error | `{ "error": "Error retrieving transactions: ..." }` |

---

### 3. Get Credit Cards

Lists all credit cards with their balances, card holder names, and status.

**Endpoint**: `GET /api/cards`

**Authorization**: Function Key Required

**Request**: No body required

**Success Response** (200 OK):
```json
{
  "success": true,
  "count": 9,
  "cards": [
    {
      "id": 1,
      "cardNumber": "4111111111111111",
      "cardNumberMasked": "****-****-****-1111",
      "cardHolderName": "John Doe",
      "balance": 4900.00,
      "cardType": "Visa",
      "expiryDate": "2027-12-31T00:00:00Z",
      "isActive": true
    },
    {
      "id": 2,
      "cardNumber": "5555555555554444",
      "cardNumberMasked": "****-****-****-4444",
      "cardHolderName": "Jane Smith",
      "balance": 3100.00,
      "cardType": "Mastercard",
      "expiryDate": "2026-06-30T00:00:00Z",
      "isActive": true
    }
  ]
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Operation success indicator |
| `count` | integer | Total number of cards |
| `cards` | array | Array of card objects |
| `cards[].id` | integer | Card database ID |
| `cards[].cardNumber` | string | Full card number |
| `cards[].cardNumberMasked` | string | Masked card number |
| `cards[].cardHolderName` | string | Card holder's name |
| `cards[].balance` | decimal | Current balance |
| `cards[].cardType` | string | Card type (Visa, Mastercard, Amex) |
| `cards[].expiryDate` | datetime | Card expiration date |
| `cards[].isActive` | boolean | Card active status |

**cURL Example**:
```bash
curl -X GET http://localhost:7071/api/cards \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

**Error Responses**:
| Status Code | Reason |
|------------|--------|
| `401 Unauthorized` | Missing/invalid function key |
| `500 Internal Server Error` | Database connection error |

---

### 4. Seed Credit Cards

Initializes the database with test credit card data. This is an idempotent operation - existing cards are skipped.

**Endpoint**: `POST /api/seed-cards`

**Authorization**: Function Key Required

**Request**: No body required

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "Database seeding completed. 9 cards added, 0 cards skipped (already exist).",
  "seededCount": 9,
  "skippedCount": 0,
  "totalCards": 9
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Operation success indicator |
| `message` | string | Human-readable result message |
| `seededCount` | integer | Number of new cards added |
| `skippedCount` | integer | Number of existing cards skipped |
| `totalCards` | integer | Total cards in test dataset |

**Test Cards Seeded**:

| Card Number | Card Holder | Balance | Type |
|------------|-------------|---------|------|
| 4111111111111111 | John Doe | $5,000 | Visa |
| 5555555555554444 | Jane Smith | $3,000 | Mastercard |
| 378282246310005 | Bob Johnson | $10,000 | American Express |
| 4000000000000002 | Alice Brown | $250 | Visa |
| 5105105105105100 | Charlie Wilson | $750 | Mastercard |
| 6011111111111117 | Diana Martinez | $1,500 | Discover |
| 4012888888881881 | Edward Lee | $100 | Visa |
| 5425233430109903 | Fiona Garcia | $50 | Mastercard |
| 4532015112830366 | George Taylor | $0 | Visa (Empty) |

**cURL Example**:
```bash
curl -X POST http://localhost:7071/api/seed-cards \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

**Error Responses**:
| Status Code | Reason | Response Example |
|------------|--------|------------------|
| `401 Unauthorized` | Missing/invalid function key | `{ "error": "Unauthorized" }` |
| `500 Internal Server Error` | Database error | `{ "success": false, "message": "Error seeding database", "error": "..." }` |

**Notes**:
- ⚠️ Only run this once during initial setup
- 🔄 Safe to run multiple times (idempotent)
- 🧪 Creates test data for development/demo purposes
- 🚫 Do NOT use in production with real user data

---

### 5. Get Test Cards

Returns the list of available test credit cards without querying the database. Useful for development and testing.

**Endpoint**: `GET /api/test-cards`

**Authorization**: Function Key Required

**Request**: No body required

**Success Response** (200 OK):
```json
[
  {
    "cardNumber": "4111111111111111",
    "cardNumberMasked": "****-****-****-1111",
    "cardHolderName": "John Doe",
    "balance": 5000.00,
    "cardType": "Visa",
    "expiryDate": "2027-12-03T10:30:00Z",
    "isActive": true
  },
  {
    "cardNumber": "5555555555554444",
    "cardNumberMasked": "****-****-****-4444",
    "cardHolderName": "Jane Smith",
    "balance": 3000.00,
    "cardType": "Mastercard",
    "expiryDate": "2026-12-03T10:30:00Z",
    "isActive": true
  }
]
```

**cURL Example**:
```bash
curl -X GET http://localhost:7071/api/test-cards \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

---

## 🔄 Transaction Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                   TRANSACTION STATES                         │
└─────────────────────────────────────────────────────────────┘

1. INITIATED (Client Request)
   ↓
   POST /api/ProcessPayment
   ↓
2. QUEUED (Service Bus)
   ↓
   Status: 202 Accepted
   ↓
3. PROCESSING (SettleTransaction)
   ↓
   Validation & Transfer
   ↓
4. COMPLETED (Final State)
   ├─→ SUCCESS (Transaction.Settled event published)
   │   └─→ Status: "Success" in database
   │
   └─→ FAILED (Transaction.Failed event published)
       └─→ Status: "Failed" + validationMessage
```

---

## 📊 Event Grid Events

The system publishes CloudEvents to Azure Event Grid for event-driven processing.

### Event Types

| Event Type | Trigger | Subscribers |
|-----------|---------|-------------|
| `Transaction.Queued` | ProcessPayment accepts request | AuditLogWriter |
| `Transaction.Settled` | Money transfer succeeds | FraudDetection, Notification, Analytics, Audit |
| `Transaction.Failed` | Transfer validation fails | Notification, Audit |
| `Fraud.AlertTriggered` | High-risk transaction detected | (Future: Security team notification) |

### Event Schema Example

```json
{
  "specversion": "1.0",
  "type": "Transaction.Settled",
  "source": "/fintech/payment-api",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "time": "2025-12-03T10:30:45.123Z",
  "subject": "transaction/a1b2c3d4",
  "data": {
    "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "fromCardNumber": "****1111",
    "toCardNumber": "****4444",
    "amount": 100.00,
    "currency": "USD",
    "processedAtUtc": "2025-12-03T10:30:45.123Z"
  }
}
```

---

## ⚠️ Error Handling

### Validation Errors

The API validates all inputs before processing. Common validation errors:

| Error | HTTP Status | Message |
|-------|-------------|---------|
| Empty request | 400 | "Empty request body" |
| Invalid amount | 400 | "Amount must be greater than zero" |
| Missing card numbers | 400 | "Source and destination card numbers are required" |
| Insufficient funds | 400 | "Insufficient funds" (via validationMessage in transaction record) |
| Expired card | 400 | "Card has expired" |
| Inactive card | 400 | "Card is inactive" |
| Self-transfer | 400 | "Cannot transfer to same card" |

### System Errors

| HTTP Status | Scenario | Action |
|-------------|----------|--------|
| 500 Internal Server Error | Database connection failure | Check PostgreSQL connectivity |
| 500 Internal Server Error | Service Bus unavailable | Check Azure Service Bus status |
| 503 Service Unavailable | Function app offline | Check function app deployment |

### Retry Strategy

- **Client Retries**: Implement exponential backoff for 500 errors
- **Service Bus**: Automatic retry with dead-letter queue after 10 attempts
- **Event Grid**: Built-in retry with exponential backoff

---

## 🚦 Rate Limits

**Current Status**: ⚠️ No rate limiting implemented

**Recommended Production Limits**:
- 100 requests per minute per API key
- 1000 requests per hour per API key
- Burst limit: 200 requests per minute

**Future Enhancement**: Implement Azure API Management or custom rate limiting middleware.

---

## 📈 Response Times

**Expected Performance** (local development):
- ProcessPayment: < 200ms (returns immediately)
- GetTransactions: < 100ms (database query)
- GetCards: < 50ms (database query)
- Settlement Processing: 2-5 seconds (async via Service Bus)

**Production Targets**:
- API Response: < 100ms (P95)
- End-to-End Settlement: < 3 seconds (P95)

---

## 🧪 Testing Endpoints

### Example Test Flow

```bash
# 1. Seed test data
curl -X POST http://localhost:7071/api/seed-cards \
  -H "x-functions-key: YOUR_KEY"

# 2. Check initial balances
curl -X GET http://localhost:7071/api/cards \
  -H "x-functions-key: YOUR_KEY"

# 3. Initiate transfer
curl -X POST http://localhost:7071/api/ProcessPayment \
  -H "Content-Type: application/json" \
  -H "x-functions-key: YOUR_KEY" \
  -d '{
    "fromCardNumber": "4111111111111111",
    "toCardNumber": "5555555555554444",
    "amount": 100.00,
    "currency": "USD"
  }'

# 4. Wait 3-5 seconds for async processing

# 5. Check transaction history
curl -X GET http://localhost:7071/api/processed-transactions \
  -H "x-functions-key: YOUR_KEY"

# 6. Verify updated balances
curl -X GET http://localhost:7071/api/cards \
  -H "x-functions-key: YOUR_KEY"
```

### PowerShell Test Script

Run the included test script:
```powershell
.\test-transfer.ps1
```

This script:
- ✅ Seeds test cards
- ✅ Checks initial balances
- ✅ Performs transfer
- ✅ Waits for processing
- ✅ Verifies final state
- ✅ Shows colored output

---

## 🔒 Security Best Practices

### Production Checklist

- [ ] Rotate function keys every 90 days
- [ ] Use separate keys for different environments
- [ ] Store keys in Azure Key Vault
- [ ] Enable HTTPS only (disable HTTP)
- [ ] Implement API rate limiting
- [ ] Add request validation middleware
- [ ] Enable Application Insights monitoring
- [ ] Set up Azure Monitor alerts
- [ ] Implement CORS policies
- [ ] Use Managed Identity for Azure resources
- [ ] Encrypt sensitive data in database
- [ ] Regular security audits
- [ ] Penetration testing

### Data Protection

- 🔐 Card numbers are masked in responses
- 🔐 Sensitive data encrypted at rest (PostgreSQL)
- 🔐 TLS 1.2+ for data in transit
- 🔐 Audit logging for compliance

---

## 📞 Support

For API support and questions:
- 📖 Review this documentation
- 🐛 Check [GitHub Issues](https://github.com/EmirU116/FintechProject/issues)
- 📧 Contact: See repository for contact information

---

## 📝 Changelog

### Version 1.0.0 (Current)
- ✅ Asynchronous payment processing
- ✅ Event-driven architecture with Event Grid
- ✅ Fraud detection and analytics
- ✅ Comprehensive audit logging
- ✅ Multi-currency support
- ✅ 51 unit tests

### Future Versions
- 🔜 JWT authentication
- 🔜 API rate limiting
- 🔜 GraphQL endpoint
- 🔜 Webhook notifications
- 🔜 OpenAPI/Swagger UI

---

<div align="center">

**[← Back to README](./README.md)** | **[View on GitHub →](https://github.com/EmirU116/FintechProject)**

</div>
