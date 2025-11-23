# CI/CD Quick Start Guide

## Overview

This project supports **two CI/CD platforms**:

1. **GitHub Actions** (`.github/workflows/ci-cd.yml`) ✅ Recommended for GitHub-hosted repositories
2. **Azure Pipelines** (`azure-pipelines.yml`) ✅ Recommended for Azure DevOps

Choose the platform that best fits your team's needs.

---

## 🚀 Quick Setup

### GitHub Actions (5 minutes)

1. **Add Secrets** (Repository → Settings → Secrets and variables → Actions):
   ```
   AZURE_CREDENTIALS          # Azure service principal JSON
   POSTGRES_CONNECTION_STRING  # PostgreSQL connection
   POSTGRES_PASSWORD          # Database password
   STORAGE_ACCOUNT_NAME       # (Optional) Storage account
   ```

2. **Push to trigger**:
   ```bash
   git push origin main
   ```

3. **View results**: Actions tab → CI/CD Pipeline

### Azure Pipelines (10 minutes)

1. **Create Service Connection** (Project Settings → Service connections):
   - Type: Azure Resource Manager
   - Name: `Azure-Service-Connection`
   - Scope: Subscription

2. **Add Pipeline Variables** (Pipelines → Edit → Variables):
   ```
   PostgresConnectionString    # PostgreSQL connection
   PostgresPassword           # Database password (mark as secret)
   StorageAccountName         # Storage account name
   subscriptionId             # Your Azure subscription ID
   ```

3. **Create Pipeline**:
   - Go to Pipelines → New pipeline
   - Select your repository
   - Choose "Existing Azure Pipelines YAML file"
   - Select `/azure-pipelines.yml`

4. **Run**: Click "Run" to start the pipeline

---

## 📋 What Gets Deployed

Both pipelines deploy:

✅ **Build & Test**
- Restore NuGet packages
- Build .NET solution
- Run unit tests with coverage

✅ **Infrastructure**
- Azure Resource Group
- Azure Storage Account
- Azure Function App
- Service Bus Namespace & Queue

✅ **Application**
- Deploy Azure Functions code
- Configure app settings
- Set up connections

✅ **Database**
- Run PostgreSQL migrations
- Create tables
- Seed initial data

---

## 🔧 Configuration

### Customize Resource Names

Edit environment variables in the pipeline files:

**GitHub Actions** (`.github/workflows/ci-cd.yml`):
```yaml
env:
  AZURE_FUNCTIONAPP_NAME: 'your-func-name'
  AZURE_RESOURCE_GROUP: 'your-rg-name'
  AZURE_LOCATION: 'eastus'
```

**Azure Pipelines** (`azure-pipelines.yml`):
```yaml
variables:
  functionAppName: 'your-func-name'
  resourceGroupName: 'your-rg-name'
  location: 'eastus'
```

---

## 🌍 Environment Strategy

### Option 1: Branch-based Deployment

Current setup (default):
- `main` branch → Production
- Other branches → Build & test only

### Option 2: Multiple Environments

#### GitHub Actions
Create separate workflow files:
```
.github/workflows/
├── deploy-dev.yml       # Deploys to dev
├── deploy-staging.yml   # Deploys to staging
└── deploy-prod.yml      # Deploys to prod
```

#### Azure Pipelines
Use stages with different environments:
```yaml
- stage: DeployDev
  environment: 'dev'
  
- stage: DeployProd
  environment: 'production'
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
```

---

## 📊 Monitoring & Logs

### GitHub Actions
1. Go to **Actions** tab
2. Click on workflow run
3. View logs for each job
4. Test results appear in summary

### Azure Pipelines
1. Go to **Pipelines**
2. Click on pipeline run
3. View logs for each stage/job
4. Test results in **Tests** tab
5. Code coverage in **Code Coverage** tab

---

## 🔒 Security Best Practices

✅ **Never commit secrets** to repository
✅ **Use secrets/variables** for sensitive data
✅ **Rotate credentials** every 90 days
✅ **Enable branch protection** on main
✅ **Require PR reviews** before merge
✅ **Use least-privilege** service principals

---

## 🐛 Common Issues & Solutions

### Issue: Azure login failed
**Solution**: Verify service principal credentials
```bash
# Test locally
az login --service-principal \
  --username $CLIENT_ID \
  --password $CLIENT_SECRET \
  --tenant $TENANT_ID
```

### Issue: Resource already exists
**Solution**: Change resource names to be unique
- Function app names must be globally unique
- Or delete existing resources first

### Issue: Database connection failed
**Solution**: Check firewall rules
- Allow Azure services
- Allow your CI/CD runner IPs

### Issue: Tests failing
**Solution**: Run locally first
```bash
dotnet test test/FintechProject.Tests/FintechProject.Tests.csproj --logger "console;verbosity=detailed"
```

---

## 📚 Complete Documentation

- **Detailed Setup**: See `CICD_SETUP.md`
- **Unit Testing**: See `UNIT_TESTING_GUIDE.md`
- **Database Setup**: See `DATABASE_SETUP_WINDOWS.md`
- **Money Transfer**: See `MONEY_TRANSFER_GUIDE.md`

---

## 🎯 Next Steps

After CI/CD is running:

1. ✅ Add branch protection rules
2. ✅ Configure code quality checks
3. ✅ Set up monitoring alerts
4. ✅ Implement blue-green deployment
5. ✅ Add integration tests
6. ✅ Configure auto-scaling

---

## 📞 Need Help?

- Check pipeline logs first
- Review `CICD_SETUP.md` for detailed info
- Ensure all secrets/variables are set
- Verify Azure permissions

---

**Ready to deploy?** Push your code and watch the magic happen! 🚀
