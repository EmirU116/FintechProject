# 🧪 Integration Tests

## Overview

This project contains integration tests that verify the end-to-end functionality of the Fintech Payment API.

## Test Coverage

### Included Tests

- ✅ **HTTP API Tests** - Full request/response validation
- ✅ **Payment Flow** - End-to-end payment processing
- ✅ **Validation** - Input validation and error handling
- ✅ **Database Integration** - Data persistence verification

### Future Tests (Not Yet Implemented)

- ⏳ **Service Bus Integration** - Message queue processing
- ⏳ **Event Grid Integration** - Event publishing/subscription
- ⏳ **Database Transactions** - ACID compliance
- ⏳ **Fraud Detection** - Real-time fraud analysis
- ⏳ **Notification System** - Email/SMS delivery

## Prerequisites

Before running integration tests:

1. **Azure Functions** must be running locally:
   ```bash
   cd src/Functions
   func start
   ```

2. **PostgreSQL** database must be accessible:
   - Host: `localhost` (or configured host)
   - Port: `5432`
   - Database: `fintech_db`

3. **Test data** should be seeded:
   ```bash
   curl -X POST http://localhost:7071/api/seed-cards
   ```

## Running Tests

### Run All Integration Tests

```bash
dotnet test test/FintechProject.IntegrationTests/FintechProject.IntegrationTests.csproj
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~PaymentFlowIntegrationTests.ProcessPayment_WithValidRequest_ShouldReturn202Accepted"
```

### Run with Detailed Output

```bash
dotnet test test/FintechProject.IntegrationTests/FintechProject.IntegrationTests.csproj --logger "console;verbosity=detailed"
```

## Test Structure

```
test/FintechProject.IntegrationTests/
├── FintechProject.IntegrationTests.csproj    # Project configuration
├── PaymentFlowIntegrationTests.cs            # End-to-end payment tests
├── README.md                                  # This file
└── (Future test files)
    ├── ServiceBusIntegrationTests.cs
    ├── EventGridIntegrationTests.cs
    └── FraudDetectionIntegrationTests.cs
```

## Current Tests

### PaymentFlowIntegrationTests

| Test | Description | Status |
|------|-------------|--------|
| `ProcessPayment_WithValidRequest_ShouldReturn202Accepted` | Valid payment request returns 202 | ✅ Active |
| `ProcessPayment_WithInvalidAmount_ShouldReturn400BadRequest` | Negative amount validation | ✅ Active |
| `ProcessPayment_WithMissingCardNumber_ShouldReturn400BadRequest` | Missing card number validation | ✅ Active |
| `GetCreditCards_ShouldReturnCardList` | Retrieve all credit cards | ✅ Active |
| `GetProcessedTransactions_ShouldReturnTransactionList` | Retrieve transaction history | ✅ Active |
| `CompletePaymentFlow_ShouldProcessSuccessfully` | Full async payment flow | ⚠️ Skipped (requires wait time) |

## Test Configuration

### Local Testing

Tests are configured to connect to:
- **Base URL**: `http://localhost:7071`
- **Function Key**: `test-key` (for local development)

### CI/CD Testing

For automated testing in CI/CD pipelines:
1. Use test containers for PostgreSQL
2. Deploy Function App to test environment
3. Configure connection strings via environment variables

## Troubleshooting

### Issue: Tests fail with connection errors

**Solution**: Ensure Azure Functions is running:
```bash
cd src/Functions
func start
```

### Issue: Tests fail with 404 Not Found

**Solution**: Verify API endpoints are deployed:
```bash
curl http://localhost:7071/api/cards
```

### Issue: Database connection errors

**Solution**: Check PostgreSQL is running and accessible:
```bash
psql -h localhost -U postgres -d fintech_db -c "SELECT 1;"
```

### Issue: Async tests fail inconsistently

**Solution**: Increase wait time in `CompletePaymentFlow` test:
```csharp
await Task.Delay(TimeSpan.FromSeconds(10)); // Increase from 5 to 10
```

## Future Enhancements

### Planned Integration Tests

1. **Service Bus Tests**
   - Verify message queuing
   - Test retry logic
   - Validate dead-letter queue handling

2. **Event Grid Tests**
   - Verify event publishing
   - Test subscriber triggers
   - Validate event schema compliance

3. **Database Transaction Tests**
   - Test concurrent transactions
   - Verify ACID properties
   - Test rollback scenarios

4. **Performance Tests**
   - Load testing with multiple concurrent requests
   - Stress testing database connections
   - Measure response times under load

5. **Security Tests**
   - Authentication bypass attempts
   - SQL injection prevention
   - Rate limiting enforcement

## Best Practices

✅ **Isolation** - Each test should be independent  
✅ **Cleanup** - Reset test data after each test  
✅ **Assertions** - Use FluentAssertions for readability  
✅ **Naming** - Follow `MethodName_Scenario_ExpectedResult` pattern  
✅ **Documentation** - Add XML comments to explain test purpose  

## Related Documentation

- [Unit Testing Guide](../../UNIT_TESTING_GUIDE.md) - Unit test documentation
- [API Reference](../../API_REFERENCE.md) - API endpoint details
- [Testing Strategy](../../docs/TESTING_STRATEGY.md) - Overall testing approach (future)

---

**Note**: This is a work in progress. More integration tests will be added as the project evolves.
