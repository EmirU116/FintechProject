# Audit Logging Implementation

## Overview

The fintech project now includes comprehensive audit logging throughout the transaction lifecycle. Audit logs are captured in **real-time to the console/terminal** and stored in the **PostgreSQL database** for compliance and future retrieval via API.

## Architecture Flow

```
HTTP Request (ProcessPayment)
    ↓ [Audit: Request Received]
Validation
    ↓ [Audit: Validation Passed/Failed]
Service Bus Queue
    ↓ [Audit: Queued to Service Bus]
Event Grid (Transaction.Queued)
    ↓
Service Bus Trigger (SettleTransaction)
    ↓ [Audit: Service Bus Triggered]
Process Transfer
    ↓ [Audit: Processing Transfer]
Database Update
    ↓ [Audit: Database Updated]
Event Grid (Transaction.Settled/Failed)
    ↓
AuditLogWriter Function
    ↓ [Audit: Event Logged to DB & Console]
```

## Features Implemented

### 1. **GetAuditLogs API Endpoint** ✅
- **Endpoint**: `GET /api/GetAuditLogs`
- **Description**: Retrieves audit logs with flexible filtering
- **Query Parameters**:
  - `eventType` - Filter by event type (e.g., "Transaction.Queued", "Transaction.Settled")
  - `transactionId` - Filter by specific transaction ID
  - `fromDate` - Filter from date (ISO 8601 format)
  - `toDate` - Filter to date (ISO 8601 format)
  - `limit` - Number of records to return (default: 100, max: 1000)

**Example Usage**:
```bash
# Get all audit logs (last 100)
GET http://localhost:7071/api/GetAuditLogs

# Get logs for specific transaction
GET http://localhost:7071/api/GetAuditLogs?transactionId=abc123

# Get logs by event type
GET http://localhost:7071/api/GetAuditLogs?eventType=Transaction.Settled

# Get logs in date range
GET http://localhost:7071/api/GetAuditLogs?fromDate=2025-12-01&toDate=2025-12-04&limit=50
```

**Response Format**:
```json
{
  "success": true,
  "count": 10,
  "limit": 100,
  "filters": {
    "eventType": "Transaction.Settled",
    "transactionId": "none",
    "fromDate": "none",
    "toDate": "none"
  },
  "auditLogs": [
    {
      "id": "uuid",
      "eventId": "event-id",
      "eventType": "Transaction.Settled",
      "eventSource": "urn:fintech:transactions",
      "eventSubject": "transactions/abc123",
      "eventData": { ... },
      "eventTime": "2025-12-04T10:30:00Z",
      "recordedAt": "2025-12-04T10:30:01Z"
    }
  ]
}
```

### 2. **Console/Terminal Audit Output** ✅

All audit events are logged to the console in **formatted boxes** for easy visibility during development and debugging.

**Example Console Output**:
```
╔══════════════════════════════════════════════════════════════════════════╗
║ AUDIT LOG: HTTP REQUEST RECEIVED                                         ║
╠══════════════════════════════════════════════════════════════════════════╣
║ Transaction ID: abc12345                                                 ║
║ Timestamp:      2025-12-04 10:30:00.123 UTC                              ║
╠══════════════════════════════════════════════════════════════════════════╣
║ Endpoint       : ProcessPayment                                          ║
║ Method         : POST                                                    ║
║ Timestamp      : 2025-12-04 10:30:00.123 UTC                             ║
╚══════════════════════════════════════════════════════════════════════════╝
```

### 3. **AuditLogger Utility** ✅

A centralized `AuditLogger` class in `src/Core/AuditLogger.cs` provides consistent logging methods:

```csharp
// Log generic audit information
AuditLogger.LogAuditToConsole(stage, transactionId, details);

// Log success events
AuditLogger.LogAuditSuccess(operation, transactionId, message);

// Log failures
AuditLogger.LogAuditFailure(operation, transactionId, reason);

// Log warnings
AuditLogger.LogAuditWarning(operation, transactionId, warning);
```

### 4. **ProcessPayment Audit Points** ✅

The `ProcessPayment` function now logs:
- ✅ HTTP request received
- ✅ Request validation passed
- ✅ Message queued to Service Bus

### 5. **SettleTransaction Audit Points** ✅

The `SettleTransaction` function now logs:
- ✅ Service Bus trigger fired
- ✅ Transaction message received
- ✅ Transfer processing started
- ✅ Transfer success/failure
- ✅ Database update status

### 6. **AuditLogWriter Enhanced** ✅

The `AuditLogWriter` function (Event Grid triggered) now:
- ✅ Stores audit events to PostgreSQL
- ✅ Outputs formatted audit logs to console
- ✅ Pretty-prints JSON event data

## Database Schema

The `audit_events` table stores all Event Grid events:

```sql
CREATE TABLE audit_events (
    id UUID PRIMARY KEY,
    event_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    event_source VARCHAR(500),
    event_subject VARCHAR(500),
    event_data TEXT,
    event_time TIMESTAMP NOT NULL,
    recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_audit_events_type ON audit_events(event_type);
CREATE INDEX idx_audit_events_time ON audit_events(event_time);
```

## Testing the Audit Logging

### ⚠️ Local Development Note

**Event Grid triggers don't work locally** without additional setup (ngrok tunneling). When running locally:
- ✅ Console audit logs work perfectly (you see them in terminal)
- ✅ ProcessPayment and SettleTransaction audit logs work
- ❌ AuditLogWriter (Event Grid triggered) won't fire automatically
- ❌ Database won't have audit_events entries from Event Grid

**Solutions:**
1. **For local testing**: Use the `SeedAuditLogs` endpoint to populate test data
2. **For full testing**: Deploy to Azure where Event Grid works natively
3. **For local Event Grid**: Set up ngrok (advanced, see below)

### 1. Start the Functions Locally

```powershell
cd src/Functions
func start
```

### 2. Seed Test Audit Data (Local Only)

```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/SeedAuditLogs" -Method POST
```

This will create sample audit log entries in the database so you can test the GetAuditLogs API.

### 3. Send a Test Transaction

```powershell
# Use the test script
.\scripts\test-transfer.ps1

# Or manually
Invoke-RestMethod -Uri "http://localhost:7071/api/ProcessPayment" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    fromCardNumber = "4532123456789012"
    toCardNumber = "5412345678901234"
    amount = 50.00
    currency = "USD"
  } | ConvertTo-Json)
```

### 3. Watch Console Output

You'll see audit logs appear in real-time:
- HTTP request received
- Validation passed
- Queued to Service Bus
- Service Bus triggered
- Processing transfer
- Database updated
- Event Grid events logged

### 4. Retrieve Audit Logs via API

```powershell
# Get all recent audit logs
Invoke-RestMethod -Uri "http://localhost:7071/api/GetAuditLogs"

# Get logs for specific transaction (use the transactionId from step 2)
Invoke-RestMethod -Uri "http://localhost:7071/api/GetAuditLogs?transactionId=YOUR_TRANSACTION_ID"

# Get only settled transactions
Invoke-RestMethod -Uri "http://localhost:7071/api/GetAuditLogs?eventType=Transaction.Settled"
```

## Frontend Integration

To integrate audit logs into your frontend application:

### Option 1: Real-time Polling
```javascript
// Poll for audit logs every 5 seconds
setInterval(async () => {
  const response = await fetch('https://your-app.azurewebsites.net/api/GetAuditLogs?limit=10');
  const data = await response.json();
  
  if (data.success) {
    updateAuditLogUI(data.auditLogs);
  }
}, 5000);
```

### Option 2: Transaction-Specific Logs
```javascript
// After initiating a transaction, fetch its audit trail
async function getTransactionAuditTrail(transactionId) {
  const response = await fetch(
    `https://your-app.azurewebsites.net/api/GetAuditLogs?transactionId=${transactionId}`
  );
  const data = await response.json();
  return data.auditLogs;
}
```

### Option 3: Event Type Filtering
```javascript
// Show only settled transactions
const response = await fetch(
  'https://your-app.azurewebsites.net/api/GetAuditLogs?eventType=Transaction.Settled&limit=50'
);
```

## Event Types

The system logs the following event types to Event Grid:

| Event Type | Description | When Triggered |
|------------|-------------|----------------|
| `Transaction.Queued` | Transaction queued to Service Bus | After validation in ProcessPayment |
| `Transaction.Settled` | Transaction successfully completed | After successful transfer in SettleTransaction |
| `Transaction.Failed` | Transaction failed | After failed transfer in SettleTransaction |

## Security Considerations

1. **Authorization**: The GetAuditLogs endpoint uses `AuthorizationLevel.Function` which requires a function key
2. **Data Privacy**: Card numbers are masked in audit logs
3. **Retention**: Consider implementing data retention policies for audit logs
4. **Access Control**: Restrict who can view audit logs in production

## Production Deployment

### Why Deploy to Azure?

**In Azure, Event Grid works natively:**
- ✅ AuditLogWriter function is automatically triggered by Event Grid events
- ✅ All Transaction.Queued, Transaction.Settled, and Transaction.Failed events are logged to database
- ✅ Complete audit trail without manual intervention
- ✅ No need for SeedAuditLogs endpoint

### Deploy to Azure

1. **Function Key**: Obtain the function key for GetAuditLogs:
   ```bash
   az functionapp keys list --name <function-app-name> --resource-group <rg-name>
   ```

2. **API Endpoint**: Use the Azure Functions URL:
   ```
   https://<your-function-app>.azurewebsites.net/api/GetAuditLogs?code=<function-key>
   ```

3. **Event Grid Setup**: Ensure Event Grid topic is created and subscribed to your functions

4. **Log Analytics**: Consider connecting to Azure Monitor for advanced querying

## Future Enhancements

- [ ] Add pagination for large result sets
- [ ] Implement audit log export (CSV/JSON)
- [ ] Add audit log search by amount, card number (masked)
- [ ] Create audit log dashboard visualization
- [ ] Add webhook notifications for critical audit events
- [ ] Implement audit log archival to Azure Blob Storage

## Troubleshooting

### Audit logs not appearing in console
- Ensure `Console.WriteLine` is not suppressed by logging configuration
- Check that functions are running with proper logging level

### GetAuditLogs returns empty
- Verify Event Grid events are being published
- Check that AuditLogWriter function is triggered
- Query database directly: `SELECT * FROM audit_events ORDER BY recorded_at DESC LIMIT 10;`

### Database connection issues
- Verify connection string in `local.settings.json`
- Ensure PostgreSQL is running and accessible
- Check firewall rules if using Azure Database for PostgreSQL

## Summary

✅ **Complete audit trail** from HTTP request → Service Bus → Database → Event Grid  
✅ **Real-time console logging** with formatted output  
✅ **REST API** for retrieving audit logs with flexible filtering  
✅ **Frontend-ready** with simple API integration  
✅ **Database persistence** for compliance and historical analysis
