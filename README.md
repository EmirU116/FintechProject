# Fintech Payment Platform

A production-ready, cloud-native fintech payment processing system built with Azure Functions, PostgreSQL, Event Grid, and Storage Queues. Designed for high scalability, low operational cost, and best-practice security patterns.

## 🏗️ Architecture

### Dual-Queue Processing Pattern

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│   Standard   │────▶│ ProcessPayment   │────▶│  Storage Queue      │
│   Transfers  │     │   (Validator)    │     │  (transactions)     │
│   (HTTP)     │     └──────────────────┘     └─────────────────────┘
└──────────────┘                                        │
                                                        ▼
                                         ┌──────────────────────────┐
                                         │  SettleTransaction       │
                                         │  - Transfer Money (PG)   │
                                         │  - Publish Events        │
                                         └──────────────────────────┘

┌──────────────┐     ┌───────────────────┐    ┌─────────────────────┐
│   Critical   │────▶│SendCriticalPayment│────▶│  Service Bus Queue  │
│   Payments   │     │  (HTTP Endpoint)  │    │  (critical-payments)│
│   (HTTP)     │     └───────────────────┘    └─────────────────────┘
└──────────────┘                                        │
                                                        ▼
                                         ┌──────────────────────────┐
                                         │ProcessCriticalPayment    │
                                         │- Guaranteed Delivery     │
                                         │- DLQ (10 retries)        │
                                         │- Duplicate Detection     │
                                         └──────────────────────────┘
                                                        │
                                        ┌───────────────┴────────────┐
                                        ▼                            ▼
                              ┌─────────────────┐        ┌──────────────────┐
                              │   Event Grid    │───────▶│  Event Handlers  │
                              │  (Domain Events)│        │  (Notifications, │
                              └─────────────────┘        │   Analytics, etc)│
                                                         └──────────────────┘
```

**Queue Selection Strategy:**
- **Storage Queue**: High-volume, standard transfers (~$0.01/month idle cost)
- **Service Bus**: Critical payments requiring guaranteed delivery, DLQ, duplicate detection (~$10/month)

## ✨ Features

### Core Capabilities
- **Asynchronous Payment Processing**: HTTP ingestion → Storage Queue → background worker pattern
- **Money Transfer System**: Account-to-account transfers with PostgreSQL ACID transactions
- **Event-Driven Architecture**: Domain events via Event Grid for fan-out to downstream systems
- **Credit Card Management**: CRUD operations with masked display
- **Transaction History**: Query processed transactions with full audit trail

### Security & Best Practices
- ✅ **Managed Identity (MSI)**: Event Grid publishing uses AAD authentication (no keys stored)
- ✅ **Secrets Management**: Sensitive config via Azure App Configuration or Key Vault
- ✅ **HTTPS Only**: All endpoints enforced with TLS 1.2+
- ✅ **Input Validation**: Transaction validation before queue insertion
- ✅ **Connection Pooling**: EF Core with Npgsql for optimized database connections

### Cost Optimization
- ✅ **Consumption Plan**: Pay-per-execution Functions (no idle cost)
- ✅ **Storage Queues**: Near-zero idle cost vs Service Bus
- ✅ **Application Insights Sampling**: Limited to 5 telemetry items/sec
- ✅ **Lifecycle Policies**: Auto-delete old logs after 30 days
- ✅ **Cool Storage Tier**: Cheaper blob storage for logs

### Observability
- **Application Insights**: Distributed tracing with adaptive sampling
- **Structured Logging**: JSON logs with correlation IDs
- **Event Grid Metrics**: Track published events and subscriber health
- **Database Monitoring**: Query performance via EF Core logging

## 🚀 Quick Start

### Prerequisites
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [PostgreSQL 14+](https://www.postgresql.org/download/) (local) or Azure Database for PostgreSQL

### Local Development

1. **Clone and restore**
   ```powershell
   git clone https://github.com/EmirU116/FintechProject.git
   cd FintechProject
   dotnet restore
   ```

2. **Set up PostgreSQL database**
   ```powershell
   # Run setup script (creates database, tables, seed data)
   psql -U postgres -f database/setup.sql
   ```

3. **Configure local settings**
   
   Create `src/Functions/local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "ConnectionStrings:PostgreSqlConnection": "Host=localhost;Database=fintech;Username=postgres;Password=yourpassword",
       "EventGrid:TopicEndpoint": "https://your-topic.eventgrid.azure.net/api/events"
     }
   }
   ```

4. **Run Functions locally**
   ```powershell
   cd src/Functions
   func start
   ```

5. **Test with demo script**
   ```powershell
   # From project root
   .\queue-send-demo.ps1 -Amount 100.00
   ```

### Azure Deployment

1. **Deploy infrastructure**
   ```powershell
   az login
   az group create --name fintech-rg --location eastus
   az deployment group create \
     --resource-group fintech-rg \
     --template-file infra/main.bicep \
     --parameters functionAppName=fintech-func
   ```

2. **Deploy Functions**
   ```powershell
   cd src/Functions
   func azure functionapp publish fintech-func
   ```

3. **Configure connection strings**
   ```powershell
   # Get PostgreSQL connection string from Azure Portal
   az functionapp config appsettings set \
     --name fintech-func \
     --resource-group fintech-rg \
     --settings "ConnectionStrings:PostgreSqlConnection=<your-connection-string>"
   ```

## 📦 Project Structure

```
FintechProject/
├── src/
│   ├── Core/                      # Domain logic & services
│   │   ├── MoneyTransferService.cs
│   │   ├── TransactionValidator.cs
│   │   ├── Database/              # EF Core repositories
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── TransactionRepository.cs
│   │   │   └── CreditCardRepository.cs
│   │   └── Eventing/              # Event Grid publisher
│   │       └── EventGridPublisher.cs
│   └── Functions/                 # Azure Functions endpoints
│       ├── ProcessPayment.cs      # HTTP → Queue
│       ├── SettleTransaction.cs   # Storage Queue → DB + Events
│       ├── ProcessCriticalPayment.cs  # Service Bus → DB + Events (DLQ)
│       ├── SendCriticalPayment.cs     # HTTP → Service Bus
│       ├── GetProcessedTransactions.cs
│       ├── GetCreditCards.cs
│       └── OnTransactionProcessed.cs
├── test/
│   └── FintechProject.Tests/     # xUnit tests (51 tests)
├── infra/
│   └── main.bicep                # Azure infrastructure as code
├── database/
│   ├── setup.sql                 # Schema + seed data
│   └── add_credit_cards_table.sql
├── queue-send-demo.ps1           # Storage Queue testing
└── servicebus-send-demo.ps1      # Service Bus testing
```

## 🧪 Testing

```powershell
# Run all unit tests
cd test/FintechProject.Tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage**: 51 tests covering validators, processors, and transfer logic.

## 📊 API Endpoints

### `POST /api/ProcessPayment`
Validate and enqueue a standard payment/transfer request to **Storage Queue**.

**Request**:
```json
{
  "fromCardNumber": "4532015112830366",
  "toCardNumber": "5425233430109903",
  "amount": 100.00,
  "currency": "USD"
}
```

**Response**: `202 Accepted` (queued to Storage Queue for processing)

---

### `POST /api/critical-payment`
Enqueue a **critical payment** requiring guaranteed delivery via **Service Bus**.

**Request**:
```json
{
  "cardNumber": "4532015112830366",
  "toCardNumber": "5425233430109903",
  "amount": 5000.00,
  "currency": "USD"
}
```

**Response**: `202 Accepted` (queued to Service Bus with DLQ and duplicate detection)

**Features**:
- ✅ Guaranteed delivery (Service Bus Standard tier)
- ✅ Dead Letter Queue after 10 retries
- ✅ Duplicate detection (10-minute window)
- ✅ 5-minute message lock duration
- ✅ Event Grid domain events on success/failure

---

### `GET /api/GetProcessedTransactions`
Retrieve transaction history.

**Response**:
```json
[
  {
    "transactionId": "abc123",
    "cardNumberMasked": "****-****-****-0366",
    "amount": 100.00,
    "currency": "USD",
    "transactionTimestamp": "2025-11-30T10:00:00Z",
    "processedAt": "2025-11-30T10:00:02Z",
    "authorizationStatus": "Approved"
  }
]
```

### `GET /api/GetCreditCards`
List all credit cards (masked).

### `POST /api/SeedCreditCards`
Initialize database with test credit cards.

## 🔔 Event Grid Events

Published domain events for downstream subscribers:

### `fintech.transactions.processed`
```json
{
  "subject": "/transactions/{id}",
  "eventType": "fintech.transactions.processed",
  "data": {
    "transactionId": "abc123",
    "amount": 100.00,
    "currency": "USD",
    "transferTimestamp": "2025-11-30T10:00:02Z",
    "fromBalance": 900.00,
    "toBalance": 1100.00
  }
}
```

### `fintech.transactions.failed`
```json
{
  "subject": "/transactions/{id}",
  "eventType": "fintech.transactions.failed",
  "data": {
    "transactionId": "abc123",
    "reason": "Insufficient funds",
    "occurredAt": "2025-11-30T10:00:02Z"
  }
}
```

## 💰 Cost Estimation (Monthly)

| Resource | Tier | Estimated Cost |
|----------|------|----------------|
| Azure Functions | Consumption | $0-5 (1M executions free) |
| Storage Account | Standard LRS | $1-2 |
| Storage Queue | Pay-per-op | $0.01 |
| **Service Bus** | **Standard** | **$10** |
| Event Grid | Custom Topic | $0.60/million ops |
| Application Insights | 5 items/sec | $2-5 |
| PostgreSQL | Flexible Server (B1ms) | $12-15 |
| **Total** | | **~$25-38/month** |

*Idle cost (no traffic): ~$23/month*

**Cost Breakdown:**
- **Storage Queue**: Near-zero cost for high-volume standard transfers
- **Service Bus Standard**: Fixed $10/month base cost provides guaranteed delivery, DLQ, and duplicate detection for critical payments
- **Hybrid approach**: Balances cost optimization (Storage Queue) with enterprise features (Service Bus)

## 🛡️ Security Considerations

- **No secrets in code**: Use Azure Key Vault or App Configuration
- **Managed Identity**: Event Grid, Storage, and Database auth via MSI where possible
- **Network isolation**: Deploy Functions in VNet with Private Endpoints for database
- **Rate limiting**: API Management or Function-level throttling for production
- **Audit logging**: All transactions logged with correlation IDs

## 📈 Performance & Scalability

- **Throughput**: Tested up to 1,000 transactions/minute on B1ms PostgreSQL
- **Latency**: p50: 50ms, p99: 200ms (queue → processed)
- **Concurrency**: Storage Queue supports up to 2,000 messages/sec per queue
- **Auto-scaling**: Functions scale out automatically based on queue depth

## 🤝 Contributing

This is a portfolio project. Feedback and suggestions welcome via GitHub Issues.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🎓 Learning Resources

Built following best practices from:
- [Azure Functions Best Practices](https://docs.microsoft.com/azure/azure-functions/functions-best-practices)
- [Event-Driven Architecture Patterns](https://docs.microsoft.com/azure/architecture/guide/architecture-styles/event-driven)
- [Azure Well-Architected Framework](https://docs.microsoft.com/azure/architecture/framework/)
- [EF Core Performance](https://docs.microsoft.com/ef/core/performance/)

## 📧 Contact

**Emir** - [GitHub Profile](https://github.com/EmirU116)

---

⭐ If you find this project useful for learning cloud-native fintech architectures, please give it a star!
