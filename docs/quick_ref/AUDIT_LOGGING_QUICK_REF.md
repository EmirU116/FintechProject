# 📋 Audit Logging Quick Reference

## 🚀 Quick Start

### 1. Start the Functions
```bash
cd src/Functions
func start
```

### 2. Test Audit Logging
```powershell
.\scripts\test-audit-logs.ps1
```

## 📍 API Endpoints

### Get All Audit Logs
```
GET /api/GetAuditLogs?limit=100
```

### Get Transaction Audit Trail
```
GET /api/GetAuditLogs?transactionId={id}
```

### Filter by Event Type
```
GET /api/GetAuditLogs?eventType=Transaction.Settled
```

### Filter by Date Range
```
GET /api/GetAuditLogs?fromDate=2025-12-01&toDate=2025-12-04
```

## 📊 Event Types

| Event Type | When Triggered |
|------------|----------------|
| `Transaction.Queued` | HTTP request validated and queued |
| `Transaction.Settled` | Transfer completed successfully |
| `Transaction.Failed` | Transfer failed |

## 🎯 Console Audit Points

### ProcessPayment Function
1. ✅ HTTP Request Received
2. ✅ Request Validated
3. ✅ Queued to Service Bus

### SettleTransaction Function
1. ✅ Service Bus Triggered
2. ✅ Transaction Received
3. ✅ Processing Transfer
4. ✅ Transfer Success/Failure
5. ✅ Database Updated

### AuditLogWriter Function
1. ✅ Event Grid Event Received
2. ✅ Audit Log Written to Database
3. ✅ Formatted Output to Console

## 💻 Usage in Code

### Log Audit Information
```csharp
AuditLogger.LogAuditToConsole("STAGE_NAME", transactionId, new Dictionary<string, object>
{
    { "Key1", "Value1" },
    { "Key2", "Value2" }
});
```

### Log Success
```csharp
AuditLogger.LogAuditSuccess("OPERATION_NAME", transactionId, "Success message");
```

### Log Failure
```csharp
AuditLogger.LogAuditFailure("OPERATION_NAME", transactionId, "Failure reason");
```

### Log Warning
```csharp
AuditLogger.LogAuditWarning("OPERATION_NAME", transactionId, "Warning message");
```

## 🌐 Frontend Integration

### JavaScript/TypeScript
```javascript
// Fetch audit logs
async function getAuditLogs(transactionId) {
  const response = await fetch(
    `${API_URL}/GetAuditLogs?transactionId=${transactionId}`
  );
  return await response.json();
}

// Display audit trail
const data = await getAuditLogs(txnId);
data.auditLogs.forEach(log => {
  console.log(`[${log.eventType}] ${log.eventTime}`);
});
```

### React Example
```jsx
function AuditLogViewer({ transactionId }) {
  const [logs, setLogs] = useState([]);
  
  useEffect(() => {
    fetch(`/api/GetAuditLogs?transactionId=${transactionId}`)
      .then(res => res.json())
      .then(data => setLogs(data.auditLogs));
  }, [transactionId]);
  
  return (
    <div>
      {logs.map(log => (
        <div key={log.id}>
          <strong>{log.eventType}</strong>
          <span>{new Date(log.eventTime).toLocaleString()}</span>
        </div>
      ))}
    </div>
  );
}
```

## 🗄️ Database Queries

### Get Recent Audit Logs
```sql
SELECT * FROM audit_events 
ORDER BY recorded_at DESC 
LIMIT 10;
```

### Get Logs for Transaction
```sql
SELECT * FROM audit_events 
WHERE event_subject LIKE '%{transactionId}%'
ORDER BY event_time;
```

### Get Logs by Event Type
```sql
SELECT * FROM audit_events 
WHERE event_type = 'Transaction.Settled'
ORDER BY event_time DESC;
```

### Count Events by Type
```sql
SELECT event_type, COUNT(*) as count
FROM audit_events
GROUP BY event_type
ORDER BY count DESC;
```

## 📁 Files

| File | Purpose |
|------|---------|
| `src/Functions/GetAuditLogs.cs` | HTTP API endpoint |
| `src/Functions/AuditLogWriter.cs` | Event Grid listener |
| `src/Core/AuditLogger.cs` | Utility functions |
| `src/Functions/ProcessPayment.cs` | HTTP trigger with auditing |
| `src/Functions/SettleTransaction.cs` | Service Bus trigger with auditing |
| `docs/AUDIT_LOGGING.md` | Full documentation |
| `scripts/test-audit-logs.ps1` | Test script |

## 🔧 Troubleshooting

### Logs not appearing in console
- Check logging level in `host.json`
- Ensure functions are running

### GetAuditLogs returns empty
- Verify Event Grid events are published
- Check AuditLogWriter is triggered
- Query database directly

### Database connection issues
- Verify connection string in `local.settings.json`
- Check PostgreSQL is running
- Test with: `psql -U postgres -d fintech_db`

## 📖 Full Documentation
See `docs/AUDIT_LOGGING.md` for complete guide

---
**Created**: December 4, 2025
**Status**: ✅ Production Ready
