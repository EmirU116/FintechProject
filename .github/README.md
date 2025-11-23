# CI/CD Pipeline Files

This directory contains all CI/CD related files for automated deployment.

## 📁 Files Overview

| File | Purpose | Platform |
|------|---------|----------|
| `.github/workflows/ci-cd.yml` | GitHub Actions pipeline configuration | GitHub |
| `azure-pipelines.yml` | Azure DevOps pipeline configuration | Azure DevOps |
| `setup-azure-credentials.ps1` | Helper script to create Azure service principal | Both |
| `CICD_SETUP.md` | Detailed setup and configuration guide | Both |
| `CICD_QUICKSTART.md` | Quick start guide (5-10 minutes) | Both |

## 🚀 Quick Start

### For GitHub Actions (Recommended for GitHub repos)

1. Run the setup script:
   ```powershell
   .\setup-azure-credentials.ps1
   ```

2. Copy the output and add to GitHub Secrets:
   - Go to: Repository → Settings → Secrets and variables → Actions
   - Add `AZURE_CREDENTIALS` and other required secrets

3. Push to main branch:
   ```bash
   git add .
   git commit -m "Add CI/CD pipeline"
   git push origin main
   ```

4. Watch deployment in the **Actions** tab

### For Azure DevOps

1. Import repository to Azure DevOps
2. Create service connection (Project Settings → Service connections)
3. Create new pipeline from `azure-pipelines.yml`
4. Add required variables in pipeline settings
5. Run the pipeline

## 📚 Documentation

- **Quick Start**: `CICD_QUICKSTART.md` - Get started in 5 minutes
- **Detailed Guide**: `CICD_SETUP.md` - Complete setup and troubleshooting
- **Main README**: `../README.md` - Project overview

## 🔧 What Gets Automated

✅ **Build**
- Restore NuGet packages
- Compile .NET solution
- Run unit tests
- Generate code coverage

✅ **Infrastructure**
- Create Azure Resource Group
- Deploy Azure Functions
- Deploy Service Bus
- Deploy Storage Account

✅ **Deployment**
- Deploy function code
- Configure app settings
- Run database migrations

✅ **Testing**
- Unit tests with xUnit
- Test results reporting
- Code coverage reporting

## 🌍 Deployment Environments

| Branch | Environment | Action |
|--------|-------------|--------|
| `main` | Production | Full deployment |
| `test/*` | N/A | Build & test only |
| `feature/*` | N/A | Build & test only |
| Pull Requests | N/A | Build & test only |

## 🔒 Required Secrets

### GitHub Actions
Add in: Settings → Secrets and variables → Actions

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON |
| `POSTGRES_CONNECTION_STRING` | PostgreSQL connection string |
| `POSTGRES_PASSWORD` | Database password |
| `STORAGE_ACCOUNT_NAME` | Storage account (optional) |

### Azure DevOps
Add in: Pipelines → Edit → Variables

| Variable | Description | Secret |
|----------|-------------|--------|
| `PostgresConnectionString` | PostgreSQL connection | No |
| `PostgresPassword` | Database password | Yes |
| `StorageAccountName` | Storage account | No |
| `subscriptionId` | Azure subscription ID | No |

## 🛠️ Customization

Edit environment variables in the pipeline files:

**GitHub Actions** (`.github/workflows/ci-cd.yml`):
```yaml
env:
  AZURE_FUNCTIONAPP_NAME: 'your-name'
  AZURE_RESOURCE_GROUP: 'your-rg'
  AZURE_LOCATION: 'your-location'
```

**Azure DevOps** (`azure-pipelines.yml`):
```yaml
variables:
  functionAppName: 'your-name'
  resourceGroupName: 'your-rg'
  location: 'your-location'
```

## 📊 Monitoring

### GitHub Actions
- View in: **Actions** tab → Select workflow run
- Test results: Automatically published in summary
- Logs: Available for each job

### Azure DevOps
- View in: **Pipelines** → Select run
- Test results: **Tests** tab
- Code coverage: **Code Coverage** tab
- Logs: Click on any job/task

## 🐛 Troubleshooting

Common issues and solutions:

1. **Authentication failed**: Check service principal credentials
2. **Resource exists**: Change resource names or delete existing
3. **Database connection failed**: Verify firewall rules
4. **Tests failing**: Run locally first with `dotnet test`

See `CICD_SETUP.md` for detailed troubleshooting guide.

## 🎯 Next Steps

After CI/CD is working:

- [ ] Add branch protection rules
- [ ] Set up monitoring alerts
- [ ] Configure auto-scaling
- [ ] Add integration tests
- [ ] Implement blue-green deployment

## 📞 Support

- Review `CICD_SETUP.md` for detailed documentation
- Check pipeline logs for error messages
- Verify all secrets/variables are configured
- Ensure Azure permissions are correct

---

**Ready?** Run the setup script and start deploying! 🚀
