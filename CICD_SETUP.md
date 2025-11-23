# CI/CD Pipeline Setup Guide

This guide explains how to set up and use the CI/CD pipeline for the Fintech project.

## Overview

The CI/CD pipeline automates:
- ✅ Building the solution
- ✅ Running unit tests
- ✅ Deploying Azure infrastructure (Bicep)
- ✅ Deploying Azure Functions
- ✅ Running database migrations

## Pipeline Structure

The pipeline consists of 4 jobs:

1. **build-and-test**: Builds the .NET solution and runs unit tests
2. **deploy-infrastructure**: Deploys Azure resources using Bicep
3. **deploy-function-app**: Deploys the Azure Functions application
4. **deploy-database**: Runs PostgreSQL database migrations

## Prerequisites

Before setting up the pipeline, you need:

1. **Azure Account** with an active subscription
2. **GitHub Repository** with this code
3. **PostgreSQL Database** (Azure Database for PostgreSQL or other)

## Setup Instructions

### Step 1: Create Azure Service Principal

Create a service principal for GitHub Actions to authenticate with Azure:

```bash
az login

# Create service principal with contributor role
az ad sp create-for-rbac --name "github-actions-fintech" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

This will output JSON credentials. Copy the entire JSON output.

### Step 2: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions, and add these secrets:

#### Required Secrets:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `AZURE_CREDENTIALS` | JSON output from service principal creation | `{"clientId": "...", "clientSecret": "...", ...}` |
| `POSTGRES_CONNECTION_STRING` | PostgreSQL connection string | `postgresql://user:password@host:5432/dbname` |
| `POSTGRES_PASSWORD` | PostgreSQL password (for psql command) | `your-password` |
| `STORAGE_ACCOUNT_NAME` | Azure Storage Account name (optional) | `paystorageaccount` |

#### How to Add Secrets:

1. Navigate to: Repository → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Enter the name and value
4. Click "Add secret"

### Step 3: Configure Environment Variables (Optional)

In `.github/workflows/ci-cd.yml`, you can customize these variables:

```yaml
env:
  DOTNET_VERSION: '8.0.x'              # .NET version
  AZURE_FUNCTIONAPP_NAME: 'event-payment-func'  # Function App name
  AZURE_RESOURCE_GROUP: 'fintech-rg'   # Resource group name
  AZURE_LOCATION: 'eastus'              # Azure region
  BUILD_CONFIGURATION: 'Release'        # Build configuration
```

### Step 4: Enable GitHub Actions

1. Go to your repository on GitHub
2. Click on the "Actions" tab
3. If prompted, enable GitHub Actions for the repository

## Pipeline Triggers

The pipeline automatically runs when:

- **Push to main branch**: Full deployment (build, test, deploy)
- **Push to test/unit-testing branch**: Build and test only
- **Pull request to main**: Build and test only
- **Manual trigger**: Via "Actions" tab → "Run workflow"

## Pipeline Flow

### For Pull Requests and Non-Main Branches:
```
┌─────────────────┐
│ Build & Test    │
│ - Restore deps  │
│ - Build         │
│ - Run tests     │
└─────────────────┘
```

### For Main Branch Push:
```
┌─────────────────┐
│ Build & Test    │
│ - Restore deps  │
│ - Build         │
│ - Run tests     │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌────────────────┐  ┌──────────────────┐
│ Deploy Infra   │  │ Deploy Database  │
│ - Login Azure  │  │ - Run migrations │
│ - Create RG    │  │ - Setup tables   │
│ - Deploy Bicep │  └──────────────────┘
└────────┬───────┘
         │
         ▼
┌─────────────────┐
│ Deploy Functions│
│ - Config app    │
│ - Deploy code   │
└─────────────────┘
```

## Monitoring the Pipeline

### View Pipeline Status:

1. Go to the "Actions" tab in your GitHub repository
2. Click on a workflow run to see details
3. Each job shows logs and status

### Test Results:

- Test results are automatically published
- View them in the "Actions" → Workflow run → "Test Results"

### Failed Deployments:

If a deployment fails:
1. Check the logs in the Actions tab
2. Common issues:
   - Missing secrets
   - Invalid Azure credentials
   - Resource naming conflicts
   - Database connection issues

## Manual Deployment

To manually trigger the pipeline:

1. Go to Actions tab
2. Select "CI/CD Pipeline"
3. Click "Run workflow"
4. Select branch
5. Click "Run workflow" button

## Local Development

For local testing before pushing:

### Build locally:
```bash
dotnet restore FintechProject.sln
dotnet build FintechProject.sln --configuration Release
```

### Run tests locally:
```bash
dotnet test test/FintechProject.Tests/FintechProject.Tests.csproj
```

### Deploy infrastructure locally:
```bash
az login
az group create --name fintech-rg --location eastus
az deployment group create \
  --resource-group fintech-rg \
  --template-file infra/main.bicep \
  --parameters functionAppName=event-payment-func
```

## Troubleshooting

### Common Issues:

#### 1. Azure Authentication Failed
**Error**: "Azure login failed"
**Solution**: 
- Verify `AZURE_CREDENTIALS` secret is correctly formatted
- Ensure service principal has correct permissions
- Check if subscription ID is correct

#### 2. Resource Already Exists
**Error**: "Resource already exists"
**Solution**: 
- Change `AZURE_FUNCTIONAPP_NAME` to a unique name
- Or delete existing resources in Azure portal

#### 3. Database Connection Failed
**Error**: "Could not connect to PostgreSQL"
**Solution**: 
- Verify `POSTGRES_CONNECTION_STRING` is correct
- Check firewall rules allow GitHub Actions IP ranges
- Ensure database exists

#### 4. Tests Failed
**Error**: "Tests failed"
**Solution**: 
- Run tests locally to debug: `dotnet test`
- Check test logs in Actions tab
- Fix failing tests before merging

#### 5. Function App Deployment Failed
**Error**: "Failed to deploy function app"
**Solution**: 
- Verify function app name is unique
- Check app settings are correctly configured
- Ensure storage account exists

## Security Best Practices

1. **Never commit secrets** to the repository
2. **Use GitHub Secrets** for all sensitive data
3. **Rotate credentials** regularly
4. **Use least-privilege** service principals
5. **Enable branch protection** on main branch
6. **Require pull request reviews** before merging

## Environment-Specific Deployments

To deploy to different environments (dev, staging, prod):

### Option 1: Multiple Workflows
Create separate workflow files:
- `.github/workflows/deploy-dev.yml`
- `.github/workflows/deploy-staging.yml`
- `.github/workflows/deploy-prod.yml`

### Option 2: Environments in GitHub
Use GitHub Environments with protection rules:

```yaml
deploy-production:
  name: Deploy to Production
  runs-on: ubuntu-latest
  environment: production  # Requires approval
  # ... deployment steps
```

## Next Steps

1. ✅ Complete setup by adding all required secrets
2. ✅ Test the pipeline by pushing to a branch
3. ✅ Configure branch protection rules
4. 🔄 Consider adding code quality checks (SonarQube, CodeQL)
5. 🔄 Add integration tests
6. 🔄 Implement blue-green deployment strategy
7. 🔄 Add monitoring and alerts

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Functions Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions)
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [PostgreSQL on Azure](https://docs.microsoft.com/en-us/azure/postgresql/)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review pipeline logs in Actions tab
3. Consult Azure and GitHub documentation
4. Open an issue in the repository

---

**Last Updated**: November 2025
