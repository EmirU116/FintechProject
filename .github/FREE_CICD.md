# 💰 Free/Minimal Cost CI/CD Pipeline

## Overview

This pipeline is optimized for **personal projects** with minimal Azure costs:
- ✅ **CI**: Build, Test, CodeQL Security, Code Coverage (Free on GitHub)
- ✅ **CD**: Single environment deployment with Consumption Plan
- ✅ **No deployment slots** (saves ~$150/month)
- ✅ **Consumption Plan only** (pay-per-execution)

---

## 💸 **Cost Breakdown**

### Monthly Azure Costs:

| Resource | Plan | Monthly Cost |
|----------|------|--------------|
| **Function App** | Consumption (Y1) | **$0-2** (1M executions free) |
| **Service Bus** | Basic | **$0.05** (first 12.5M operations free) |
| **Storage Account** | Standard LRS + Cool tier | **$1-3** |
| **Application Insights** | 5GB free/month | **$0-5** (usually under free tier) |
| **TOTAL** | | **~$1-10/month** 🎉 |

### GitHub Actions (Free):
- ✅ **2,000 minutes/month** for private repos
- ✅ **Unlimited** for public repos
- ✅ CodeQL scanning free
- ✅ Coverage reporting free

### **Total Project Cost: ~$1-10/month**

---

## 🎯 **Simplified Architecture**

```
Feature Branch
     │
     ├─► CI: Build + Test + CodeQL + Coverage
     │   (No deployment)
     │
     ▼
Main Branch
     │
     ├─► CI: Build + Test + CodeQL + Coverage
     │
     └─► CD: Deploy to Azure
         └─► Direct deployment to Consumption Plan
             (~$1-10/month)
```

---

## 🚀 **What's Included**

### CI (Continuous Integration) - FREE
1. ✅ **Build & Test** - .NET solution with unit tests
2. ✅ **Code Coverage ≥80%** - Enforced threshold, fails build if below
3. ✅ **Coverage Reports** - HTML, Cobertura, badges with detailed line coverage
4. ✅ **Coverage Upload** - Codecov integration (optional)
5. ✅ **Coverage Badge** - Dynamic badge with color coding (optional)
6. ✅ **CodeQL Security** - Automated vulnerability scanning with extended queries
7. ✅ **Test Results** - Published in GitHub UI with pass/fail details

### CD (Continuous Deployment) - ~$1-10/month
1. ✅ **Infrastructure** - Bicep deployment
2. ✅ **Function App** - Consumption Plan (pay-per-use)
3. ✅ **Service Bus** - Basic tier (minimal cost)
4. ✅ **Storage** - Cool tier with auto-cleanup
5. ✅ **App Insights** - 30-day retention

### What's NOT Included (Saves ~$300/month)
- ❌ No deployment slots
- ❌ No staging environment
- ❌ No Elastic Premium plans
- ❌ No multiple environments

---

## ⚠️ **Important: 80% Code Coverage Required**

**Your CI/CD pipeline enforces ≥80% code coverage.** If coverage falls below 80%, the build will fail and deployment will be blocked.

- ✅ Coverage ≥80% → Build passes, deploys to Azure
- ❌ Coverage <80% → Build fails, no deployment

**Check coverage locally before pushing:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Look for: "Line coverage: 8X%"
```

📖 **See `.github/COVERAGE_ENFORCEMENT.md` for details on:**
- How to check coverage locally
- How to increase coverage
- How to view coverage reports
- Troubleshooting coverage issues

---

## 📋 **Setup Steps**

### 1. Create GitHub Environment
```
Settings → Environments → New environment → "production"
```
- No protection rules needed (it's just you!)
- Optional: Add yourself as reviewer if you want manual approval

### 2. Add Azure Credentials
```bash
# Run the setup script
.\setup-azure-credentials.ps1

# Copy the JSON output
# Go to: Settings → Secrets and variables → Actions → New secret
# Name: AZURE_CREDENTIALS
# Value: <paste JSON>
```

### 3. Optional: Coverage Badge Setup
```bash
# Only if you want the coverage badge
# 1. Sign up at codecov.io (free for open source)
# 2. Add repository
# 3. Copy upload token → Add as CODECOV_TOKEN secret

# 4. Create GitHub Personal Access Token
#    Settings → Developer settings → Personal access tokens
#    Scope: gist
#    Copy token → Add as GIST_SECRET secret

# 5. Create gist at gist.github.com
#    Name: fintech-coverage-badge.json
#    Content: {"schemaVersion": 1}
#    Copy gist ID from URL → Add as GIST_ID secret
```

### 4. Deploy!
```bash
git add .
git commit -m "Add free CI/CD pipeline"
git push origin main
```

**That's it!** Pipeline runs automatically. 🎉

---

## 🎨 **Add Coverage Badge to README**

```markdown
![Build](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml/badge.svg)
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/fintech-coverage-badge.json)
![CodeQL](https://github.com/EmirU116/FintechProject/workflows/CodeQL/badge.svg)
```

---

## 📊 **Consumption Plan Details**

### Free Tier Includes:
- ✅ **1 million executions/month**
- ✅ **400,000 GB-seconds/month**
- ✅ **No base charge**

### After Free Tier:
- **$0.20 per million executions**
- **$0.000016 per GB-second**

### For Personal Project:
- Typical usage: 1,000-10,000 executions/month
- **Cost: $0-0.20/month** 🎉

---

## 🔒 **Cost Protection**

### Built-in Cost Limits:

1. **Function Scale Limit**
   ```bicep
   functionAppScaleLimit: 5  // Max 5 concurrent instances
   ```

2. **Storage Lifecycle**
   ```bicep
   delete logs after 30 days  // Auto-cleanup
   ```

3. **App Insights Retention**
   ```bicep
   RetentionInDays: 30  // Minimum retention
   ```

4. **Service Bus**
   ```bicep
   Basic tier  // Cheapest option
   ```

### Set Budget Alert (Recommended):

```bash
az consumption budget create \
  --budget-name personal-project-limit \
  --amount 20 \
  --time-grain Monthly \
  --start-date 2025-11-01 \
  --end-date 2030-12-31 \
  --resource-group fintech-rg
```

**Alert at $20/month** = way above typical usage, catches issues early

---

## 📈 **Monitoring Costs**

### Check Current Month:
```bash
az consumption usage list \
  --start-date $(date -d '30 days ago' +%Y-%m-%d) \
  --output table
```

### Azure Portal:
**Cost Management + Billing → Cost analysis**

### Expected Pattern:
- Month 1: ~$5 (new resources)
- Month 2+: ~$1-3 (typical usage)

---

## 🛠️ **Deployment Flow**

### On Push to Main:

1. **CI Phase** (~3 minutes)
   - Build solution
   - Run unit tests
   - Generate coverage
   - CodeQL scan

2. **CD Phase** (~5 minutes)
   - Deploy Bicep (infrastructure)
   - Deploy Functions (code)
   - Configure settings

3. **Total Time**: ~8 minutes ⏱️

---

## 🎯 **What You Get**

### After Successful Deployment:

```
✅ Azure Function App (Consumption)
   - URL: https://fintech-func-free.azurewebsites.net
   - Cost: $0-2/month

✅ Service Bus Queue
   - Queue: transactions
   - Cost: $0.05/month

✅ Storage Account
   - Cool tier with auto-cleanup
   - Cost: $1-3/month

✅ Application Insights
   - 30-day retention
   - Cost: $0-5/month (usually free)

✅ CI/CD Pipeline
   - Automated testing
   - Security scanning
   - Code coverage
   - Cost: FREE
```

---

## ⚡ **Quick Commands**

### View deployment:
```bash
az functionapp list --resource-group fintech-rg --output table
```

### Check costs:
```bash
az consumption usage list --output table
```

### View logs:
```bash
az functionapp log tail \
  --name fintech-func-free \
  --resource-group fintech-rg
```

### Delete everything (stop all costs):
```bash
az group delete --name fintech-rg --yes --no-wait
```

---

## 🚨 **If Costs Increase**

### Unexpected charges? Check:

1. **Function executions**
   ```bash
   # View in Application Insights
   # Check for infinite loops or excessive calls
   ```

2. **Storage growth**
   ```bash
   az storage account show-usage \
     --name stfintechfuncfree...
   ```

3. **Service Bus messages**
   ```bash
   az servicebus queue show \
     --name transactions \
     --namespace-name fintech-sb-...
   ```

### Emergency Stop:
```bash
# Stop Function App (preserves data)
az functionapp stop \
  --name fintech-func-free \
  --resource-group fintech-rg

# Or delete everything
az group delete --name fintech-rg --yes
```

---

## 💡 **Best Practices**

1. ✅ **Monitor costs weekly** (first month)
2. ✅ **Set budget alert** at $20/month
3. ✅ **Review Application Insights** for excessive calls
4. ✅ **Use local development** for testing (not Azure)
5. ✅ **Delete dev/test deployments** when not needed
6. ✅ **Keep Consumption Plan** (don't upgrade)
7. ✅ **Let auto-cleanup delete old logs**

---

## 📚 **Comparison: Before vs After**

### Before (Multi-Environment with Slots):
- Dev: ~$11/month
- Staging: ~$159/month (Elastic Premium)
- Prod: ~$175/month (Elastic Premium)
- **Total: ~$345/month** 💸

### After (Single Environment, No Slots):
- Production: ~$1-10/month (Consumption)
- **Total: ~$1-10/month** 🎉
- **Savings: ~$335/month**

---

## 🎊 **What You're NOT Missing**

### Removed (to save costs):
- ❌ Deployment slots (~$150/month)
- ❌ Staging environment (~$159/month)
- ❌ Blue-green deployment
- ❌ Multiple environments

### What's Still Included (FREE or minimal):
- ✅ Automated testing
- ✅ Security scanning (CodeQL)
- ✅ Code coverage
- ✅ Infrastructure as Code (Bicep)
- ✅ Continuous deployment
- ✅ Application monitoring

**For a personal project, you don't need enterprise features!**

---

## 🎓 **Learning Opportunities**

This setup teaches you:
- ✅ CI/CD pipelines
- ✅ GitHub Actions
- ✅ Azure Functions
- ✅ Infrastructure as Code (Bicep)
- ✅ Security scanning (CodeQL)
- ✅ Code coverage
- ✅ Cost optimization

**All for ~$1-10/month!** 🚀

---

## 📞 **Support**

### Need help?
1. Check GitHub Actions logs
2. Review Azure Portal → Function App
3. Check Application Insights for errors
4. Review this documentation

### Want to upgrade later?
- Easy to add deployment slots
- Easy to add environments
- Easy to upgrade to Premium plans
- All infrastructure already in place

---

**Status**: ✅ Free/Minimal Cost CI/CD Ready  
**Monthly Cost**: ~$1-10  
**Recommended For**: Personal projects, learning, portfolios  
**Last Updated**: November 24, 2025
