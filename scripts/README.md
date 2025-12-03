# 🛠️ PowerShell Scripts

This folder contains PowerShell scripts for development, testing, and deployment automation.

---

## 📋 Scripts Overview

### 🗄️ Database Management

| Script | Description | Usage |
|--------|-------------|-------|
| **setup-database.ps1** | Complete database initialization | `.\setup-database.ps1` |
| **diagnose-database.ps1** | Database diagnostics and health check | `.\diagnose-database.ps1` |

Sets up PostgreSQL database, creates tables, and initializes schema for local development.

---

### 🧪 Testing Scripts

| Script | Description | Usage |
|--------|-------------|-------|
| **test-transfer.ps1** | Quick payment transfer test | `.\test-transfer.ps1` |
| **test-direct-payment.ps1** | Direct payment API test | `.\test-direct-payment.ps1` |
| **test-trace-execution-path.ps1** | Trace execution flow with detailed logs | `.\test-trace-execution-path.ps1` |
| **test-event-grid.ps1** | Test Event Grid integration | `.\test-event-grid.ps1` |

Quick testing scripts to verify payment processing, event handling, and execution paths.

---

### ☁️ Azure Deployment

| Script | Description | Usage |
|--------|-------------|-------|
| **setup-azure-credentials.ps1** | Create Azure service principal and generate CI/CD credentials | `.\setup-azure-credentials.ps1` |
| **setup-event-grid-subscription.ps1** | Configure Event Grid subscriptions | `.\setup-event-grid-subscription.ps1` |

Azure resource configuration and CI/CD pipeline setup scripts.

---

## 🚀 Quick Start

### 1. Database Setup (First Time)
```powershell
cd scripts
.\setup-database.ps1
```

### 2. Test Payment Flow
```powershell
# Start the function app first
cd ..\src\Functions
func start

# In another terminal
cd ..\..\scripts
.\test-transfer.ps1
```

### 3. Diagnose Issues
```powershell
cd scripts
.\diagnose-database.ps1
```

---

## 📖 Script Details

### setup-database.ps1
**Purpose:** Complete database initialization for local development

**What it does:**
- ✅ Tests PostgreSQL connection
- ✅ Creates `fintech_db` database
- ✅ Creates all required tables (credit_cards, processed_transactions, etc.)
- ✅ Sets up Event Grid tables
- ✅ Seeds test credit card data

**Prerequisites:**
- PostgreSQL installed (15+)
- psql.exe in PATH or at standard location

---

### test-transfer.ps1
**Purpose:** Quick payment test with minimal output

**What it does:**
- ✅ Sends a payment request ($500 transfer)
- ✅ Shows transaction ID and trace ID
- ✅ Reminds you to watch function logs

**Example Output:**
```
✓ Transaction ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
✓ Trace ID: abc12345
Watch function app terminal for 🟢 and 🔵 logs
```

---

### test-trace-execution-path.ps1
**Purpose:** Detailed execution flow analysis with timestamps

**What it does:**
- ✅ Shows HTTP request → Service Bus → SettleTransaction flow
- ✅ Verifies execution order with timestamps
- ✅ Displays queue status and database updates
- ✅ Provides detailed timeline

**Use this when:**
- Debugging async processing issues
- Understanding the execution flow
- Verifying Service Bus trigger behavior

---

### diagnose-database.ps1
**Purpose:** System diagnostics and troubleshooting

**What it does:**
- ✅ Checks PostgreSQL installation
- ✅ Tests database connectivity
- ✅ Verifies tables exist
- ✅ Checks Azure Functions Core Tools
- ✅ Validates psql.exe location

**Use this when:**
- First-time setup
- Connection issues
- "Table not found" errors

---

### setup-azure-credentials.ps1
**Purpose:** CI/CD pipeline credential generation

**What it does:**
- ✅ Creates Azure service principal
- ✅ Generates AZURE_CREDENTIALS JSON
- ✅ Tests service principal login
- ✅ Saves credentials to file

**Use this when:**
- Setting up GitHub Actions
- Setting up Azure DevOps
- Deploying to Azure for the first time

**⚠️ Security:** Delete the generated credentials file after copying to GitHub Secrets

---

### setup-event-grid-subscription.ps1
**Purpose:** Configure Event Grid subscriptions

**What it does:**
- ✅ Creates Event Grid subscription
- ✅ Links to Azure Functions
- ✅ Configures event filters
- ✅ Verifies subscription creation

**Use this when:**
- Deploying to Azure
- Setting up event-driven architecture

---

## 🔧 Prerequisites

All scripts require:
- **PowerShell 5.1+** (Windows PowerShell or PowerShell Core)
- **Azure CLI** (for Azure-related scripts)
- **PostgreSQL 15+** (for database scripts)
- **Azure Functions Core Tools v4** (for testing scripts)

---

## 📝 Tips

### Running from Root Directory
```powershell
# All scripts can be run from project root
.\scripts\setup-database.ps1
.\scripts\test-transfer.ps1
```

### Script Execution Policy
If you encounter execution policy errors:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Viewing Script Details
```powershell
# View script contents
Get-Content .\setup-database.ps1
```

---

## 🐛 Troubleshooting

### Script won't run
```powershell
# Check execution policy
Get-ExecutionPolicy

# Allow script execution
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### PostgreSQL connection fails
```powershell
# Run diagnostics
.\diagnose-database.ps1

# Check if PostgreSQL is running
Get-Service postgresql*
```

### Azure CLI not found
```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Or download from: https://aka.ms/installazurecliwindows
```

---

## 📚 Related Documentation

- [Database Setup Guide](../docs/setup/POSTGRESQL_INTEGRATION.md)
- [CI/CD Setup Guide](../docs/deployment/CICD_SETUP.md)
- [Testing Guide](../docs/guides/UNIT_TESTING_GUIDE.md)
- [API Reference](../docs/guides/API_REFERENCE.md)

---

**[← Back to Main README](../README.md)**
