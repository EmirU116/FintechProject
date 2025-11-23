# Fintech Project

[![CI](https://github.com/EmirU116/FintechProject/actions/workflows/ci.yml/badge.svg)](https://github.com/EmirU116/FintechProject/actions/workflows/ci.yml)
[![CD - Azure Functions](https://github.com/EmirU116/FintechProject/actions/workflows/cd-azure-functions.yml/badge.svg)](https://github.com/EmirU116/FintechProject/actions/workflows/cd-azure-functions.yml)
[![Deploy Infrastructure](https://github.com/EmirU116/FintechProject/actions/workflows/deploy-infrastructure.yml/badge.svg)](https://github.com/EmirU116/FintechProject/actions/workflows/deploy-infrastructure.yml)

A financial technology project built with .NET 8.0 and Azure Functions, featuring transaction processing, credit card validation, and money transfer capabilities.

## 🚀 Features

- **Transaction Processing**: Real-time transaction validation and processing
- **Credit Card Management**: Support for Visa, Mastercard, and American Express
- **Money Transfer Service**: Secure money transfers between accounts
- **Database Integration**: PostgreSQL for persistent data storage
- **Event-Driven Architecture**: Azure Service Bus for asynchronous processing
- **Comprehensive Testing**: 51+ unit tests with high coverage

## 🏗️ Architecture

The project follows a clean architecture pattern with:

- **Azure Functions**: Serverless compute for API endpoints
- **PostgreSQL**: Relational database for transaction storage
- **Azure Service Bus**: Message queue for async processing
- **Entity Framework Core**: ORM for database operations

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Azure Subscription](https://azure.microsoft.com/free/) (for deployment)

## 🛠️ Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/EmirU116/FintechProject.git
cd FintechProject
```

### 2. Set up PostgreSQL Database

Follow the [Database Setup Guide](database/README.md) to configure your local PostgreSQL instance.

Quick setup:
```bash
# Create database
psql -U postgres -c "CREATE DATABASE fintech_db;"

# Run setup script
psql -U postgres -d fintech_db -f database/setup.sql
```

### 3. Configure Local Settings

Create `src/Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "your-service-bus-connection-string"
  },
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=fintech_db;Username=postgres;Password=postgres"
  }
}
```

### 4. Restore Dependencies

```bash
dotnet restore
```

### 5. Build the Project

```bash
dotnet build
```

### 6. Run Tests

```bash
dotnet test
```

### 7. Run Locally

```bash
cd src/Functions
func start
```

The API will be available at `http://localhost:7071`

## 🧪 Testing

The project includes comprehensive unit tests covering:

- Transaction validation
- Credit card processing
- Money transfer operations
- Error handling

Run tests with detailed output:
```bash
dotnet test --verbosity normal
```

View test coverage:
```bash
dotnet test /p:CollectCoverage=true
```

## 📚 Documentation

- [CI/CD Guide](CI_CD_GUIDE.md) - Complete guide to CI/CD pipelines
- [Database Setup](database/README.md) - PostgreSQL setup instructions
- [Money Transfer Guide](MONEY_TRANSFER_GUIDE.md) - How to use money transfer features
- [Unit Testing Guide](UNIT_TESTING_GUIDE.md) - Testing guidelines
- [Implementation Summary](IMPLEMENTATION_SUMMARY.md) - Technical implementation details
- [Async Transfer Flow](ASYNC_TRANSFER_FLOW.md) - Asynchronous processing flow

## 🚢 Deployment

### CI/CD Pipelines

The project includes three automated workflows:

1. **CI**: Runs on every push and PR
   - Builds on Ubuntu and Windows
   - Runs all tests
   - Performs code quality checks

2. **CD - Azure Functions**: Deploys to Azure
   - Automatic staging deployment on main branch
   - Manual production deployment with approval

3. **Deploy Infrastructure**: Deploys Azure resources
   - Manual trigger only
   - Validates and deploys Bicep templates

See [CI/CD Guide](CI_CD_GUIDE.md) for detailed setup instructions.

### Manual Deployment

Deploy to Azure manually:

```bash
# Build and publish
dotnet publish src/Functions/Functions.csproj -c Release -o ./publish

# Deploy using Azure CLI
func azure functionapp publish <your-function-app-name>
```

## 📁 Project Structure

```
FintechProject/
├── .github/
│   ├── workflows/          # CI/CD workflows
│   ├── ISSUE_TEMPLATE/     # Issue templates
│   └── PULL_REQUEST_TEMPLATE.md
├── src/
│   ├── Core/               # Domain models and business logic
│   └── Functions/          # Azure Functions endpoints
├── test/
│   └── FintechProject.Tests/  # Unit tests
├── infra/
│   └── main.bicep          # Infrastructure as Code
├── database/
│   ├── setup.sql           # Database schema
│   └── README.md           # Database setup guide
└── docs/                   # Additional documentation
```

## 🔧 Available Endpoints

- `POST /api/ProcessPayment` - Process a payment transaction
- `GET /api/GetCreditCards` - Retrieve credit cards
- `GET /api/GetTestCards` - Get test credit cards
- `POST /api/SeedCreditCards` - Initialize test data
- `GET /api/GetProcessedTransactions` - View transaction history
- `POST /api/SettleTransaction` - Settle a transaction

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure:
- All tests pass
- Code follows existing style
- Documentation is updated
- PR template is filled out

## 📊 Technology Stack

- **Backend**: .NET 8.0, Azure Functions v4
- **Database**: PostgreSQL 16, Entity Framework Core 8
- **Messaging**: Azure Service Bus
- **Testing**: xUnit, Moq, FluentAssertions
- **Infrastructure**: Bicep (Azure IaC)
- **CI/CD**: GitHub Actions

## 🔒 Security

- Never commit sensitive data or connection strings
- Use Azure Key Vault for production secrets
- Keep dependencies up to date (Dependabot enabled)
- Review security alerts in GitHub

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **EmirU116** - Initial work

## 🙏 Acknowledgments

- Azure Functions team for excellent documentation
- .NET community for valuable resources
- Contributors and testers

## 📞 Support

For issues, questions, or feature requests:
- Open an [Issue](https://github.com/EmirU116/FintechProject/issues)
- Check existing [Documentation](docs/)
- Review [CI/CD Guide](CI_CD_GUIDE.md)

---

**Built with ❤️ using .NET and Azure**
