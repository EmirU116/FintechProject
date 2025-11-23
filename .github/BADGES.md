# Adding CI/CD Status Badges to README

## GitHub Actions Badge

Add this to your main `README.md` file to show build status:

```markdown
[![CI/CD Pipeline](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml)
```

Replace `EmirU116/FintechProject` with your GitHub username and repository name.

## Azure DevOps Badge

For Azure Pipelines, add this to your README:

```markdown
[![Build Status](https://dev.azure.com/{organization}/{project}/_apis/build/status/{pipeline-name}?branchName=main)](https://dev.azure.com/{organization}/{project}/_build/latest?definitionId={definition-id}&branchName=main)
```

Replace:
- `{organization}` - Your Azure DevOps organization
- `{project}` - Your project name
- `{pipeline-name}` - Your pipeline name
- `{definition-id}` - Your pipeline definition ID (found in pipeline URL)

## Example README Section

```markdown
# Fintech Payment Processing System

[![CI/CD Pipeline](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A production-ready fintech payment processing system built with .NET 8, Azure Functions, and PostgreSQL.

## 🚀 Features

- Asynchronous payment processing
- Service Bus integration
- PostgreSQL database
- Comprehensive unit tests
- **Automated CI/CD deployment**

## 📦 Deployment

This project uses automated CI/CD pipelines for deployment:

- **GitHub Actions**: Automatic deployment on push to main
- **Status**: See badge above for current build status
- **Setup Guide**: See [CICD_QUICKSTART.md](CICD_QUICKSTART.md)

### Quick Deploy

1. Set up secrets (see [CICD_SETUP.md](CICD_SETUP.md))
2. Push to main branch
3. Watch deployment in Actions tab

## 📚 Documentation

- [CI/CD Setup Guide](CICD_SETUP.md)
- [Quick Start](CICD_QUICKSTART.md)
- [Unit Testing Guide](UNIT_TESTING_GUIDE.md)
- [Money Transfer Guide](MONEY_TRANSFER_GUIDE.md)
```

## Additional Badges

You can add more badges for better visibility:

### Test Coverage
```markdown
[![codecov](https://codecov.io/gh/EmirU116/FintechProject/branch/main/graph/badge.svg)](https://codecov.io/gh/EmirU116/FintechProject)
```

### Code Quality (if using CodeQL)
```markdown
[![CodeQL](https://github.com/EmirU116/FintechProject/workflows/CodeQL/badge.svg)](https://github.com/EmirU116/FintechProject/security/code-scanning)
```

### License
```markdown
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
```

### .NET Version
```markdown
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
```

### Azure
```markdown
[![Azure](https://img.shields.io/badge/Azure-Functions-blue?logo=microsoft-azure)](https://azure.microsoft.com/en-us/services/functions/)
```

## Placement in README

Typical placement:
1. **Top**: Right after the title
2. **Badges Row**: All badges in one line
3. **Before Description**: After title, before project description

Example:
```markdown
# Fintech Payment System

[![Build](badge1)](link) [![Tests](badge2)](link) [![License](badge3)](link)

A production-ready payment processing system...
```
