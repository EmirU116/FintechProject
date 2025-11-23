# CI/CD Guide for FintechProject

This guide explains the CI/CD pipelines set up for the Fintech project.

## Overview

The project has three main GitHub Actions workflows:

1. **CI (Continuous Integration)** - Builds and tests the code on every push and pull request
2. **CD - Azure Functions** - Deploys the application to Azure Functions
3. **Deploy Infrastructure** - Deploys Azure infrastructure using Bicep templates

## Workflows

### 1. CI Workflow (`.github/workflows/ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches

**What it does:**
- Runs on Ubuntu and Windows to ensure cross-platform compatibility
- Sets up .NET 8.0
- Restores NuGet packages
- Builds the solution in Release configuration
- Runs all unit tests
- Uploads test results as artifacts
- Performs code quality analysis using `dotnet format`

**Status:** Runs automatically on code changes

### 2. CD - Azure Functions (`.github/workflows/cd-azure-functions.yml`)

**Triggers:**
- Automatic deployment to **Staging** on push to `main` branch
- Manual deployment via workflow dispatch to **Staging** or **Production**

**What it does:**
1. **Build Job:**
   - Builds the application
   - Runs tests
   - Publishes the Azure Function
   - Creates deployment artifact

2. **Deploy to Staging:**
   - Downloads the artifact
   - Deploys to Azure Functions (Staging environment)
   - Provides deployment summary

3. **Deploy to Production:**
   - Requires manual approval (GitHub environment protection)
   - Downloads the artifact
   - Deploys to Azure Functions (Production environment)
   - Provides deployment summary

**Required Secrets:**
- `AZURE_FUNCTIONAPP_NAME_STAGING` - Name of staging Function App
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_STAGING` - Staging publish profile
- `AZURE_FUNCTIONAPP_NAME_PRODUCTION` - Name of production Function App
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_PRODUCTION` - Production publish profile

### 3. Deploy Infrastructure (`.github/workflows/deploy-infrastructure.yml`)

**Triggers:**
- Manual workflow dispatch only

**What it does:**
1. **Validate Job:**
   - Validates Bicep template syntax and parameters
   - Checks for deployment errors before actual deployment

2. **Deploy Job:**
   - Requires environment approval
   - Creates or updates Azure Resource Group
   - Deploys infrastructure using Bicep:
     - Azure Storage Account
     - Azure Functions App Service Plan (Consumption)
     - Azure Function App
     - Azure Service Bus Namespace
     - Azure Service Bus Queue
   - Outputs Service Bus connection string

**Required Secrets:**
- `AZURE_CREDENTIALS` - Azure service principal credentials (JSON format)

**Parameters:**
- `environment` - Target environment (staging/production)
- `resource_group` - Azure Resource Group name
- `location` - Azure region (default: eastus)

## Setup Instructions

### 1. Set up Azure Credentials

Create an Azure service principal:

```bash
az ad sp create-for-rbac --name "fintech-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

Add the output JSON to GitHub Secrets as `AZURE_CREDENTIALS`.

### 2. Set up Function App Secrets

For each environment (staging and production):

1. Create Azure Function App (or use the infrastructure workflow)
2. Download the publish profile from Azure Portal
3. Add to GitHub Secrets:
   - `AZURE_FUNCTIONAPP_NAME_STAGING`
   - `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_STAGING`
   - `AZURE_FUNCTIONAPP_NAME_PRODUCTION`
   - `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_PRODUCTION`

### 3. Configure GitHub Environments

Create two environments in GitHub repository settings:

1. **staging** - Automatic deployment, no approval required
2. **production** - Requires manual approval from designated reviewers

To configure:
1. Go to repository **Settings** → **Environments**
2. Click **New environment**
3. Name it (staging/production)
4. For production, add required reviewers

### 4. Enable Workflows

All workflows are ready to use. They will trigger based on their configuration:
- CI runs automatically on pushes and PRs
- CD to staging runs on push to main
- Infrastructure and production deployments are manual

## Running Workflows Manually

### Deploy to Production

1. Go to **Actions** tab
2. Select **CD - Azure Functions**
3. Click **Run workflow**
4. Select `production` environment
5. Confirm and run
6. Wait for staging deployment (if on main)
7. Approve production deployment when prompted

### Deploy Infrastructure

1. Go to **Actions** tab
2. Select **Deploy Infrastructure**
3. Click **Run workflow**
4. Enter parameters:
   - Environment (staging/production)
   - Resource Group name
   - Azure location
5. Confirm and run
6. Approve deployment when prompted

## Monitoring

### Build Status

Check the status of CI/CD pipelines:
- Go to **Actions** tab in GitHub
- View workflow runs and logs
- Download artifacts (test results, deployment packages)

### Deployment Status

After deployment:
- Check the workflow summary for deployment URLs
- Verify Function App is running in Azure Portal
- Test endpoints to ensure successful deployment

## Best Practices

1. **Always run CI before merging** - Ensure all tests pass
2. **Deploy to staging first** - Automatic on main branch
3. **Test in staging** - Verify functionality before production
4. **Manual production deploys** - Use workflow dispatch with approval
5. **Monitor after deployment** - Check logs and Application Insights
6. **Keep secrets secure** - Never commit secrets to code

## Dependabot

Dependabot is configured to automatically:
- Update NuGet packages weekly (Mondays at 6 AM)
- Update GitHub Actions versions weekly
- Create pull requests for dependency updates
- Label PRs appropriately

Review and merge Dependabot PRs after CI passes.

## Troubleshooting

### CI Failures

- Check test results artifacts
- Review build logs
- Ensure all dependencies are restored
- Verify .NET version compatibility

### Deployment Failures

- Verify Azure credentials are valid
- Check Function App exists and is accessible
- Ensure publish profile is up to date
- Review deployment logs in workflow

### Infrastructure Deployment Failures

- Verify Bicep template syntax
- Check Azure subscription permissions
- Ensure resource names are unique
- Review Azure Activity Log

## Additional Resources

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Dependabot Documentation](https://docs.github.com/code-security/dependabot)

## Support

For issues or questions:
1. Check workflow logs in Actions tab
2. Review Azure Function App logs
3. Consult project documentation
4. Open an issue in the repository
