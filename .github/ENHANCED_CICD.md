# 🚀 Enhanced CI/CD Pipeline

## Overview

This pipeline provides enterprise-grade CI/CD with:
- ✅ **CI**: Build, Test, CodeQL Security, Code Coverage Badge
- ✅ **CD**: Multi-environment deployment (Dev/Staging/Prod) with slot swaps

---

## 📊 Pipeline Architecture

```
Push/PR
   │
   ├─► CI: Build & Test (always runs)
   │   ├─► Build .NET solution
   │   ├─► Run unit tests with coverage
   │   ├─► Generate coverage report
   │   ├─► Upload to Codecov
   │   └─► Create coverage badge
   │
   ├─► CI: CodeQL Security (always runs)
   │   ├─► Initialize CodeQL
   │   ├─► Build for analysis
   │   └─► Security scan
   │
   ├─► CD: Deploy to Dev (on develop branch)
   │   └─► Direct deployment (no slots)
   │
   ├─► CD: Deploy to Staging (on main branch)
   │   ├─► Deploy to staging slot
   │   ├─► Run smoke tests
   │   └─► Swap to production slot
   │
   └─► CD: Deploy to Prod (manual trigger)
       ├─► Deploy to blue slot
       ├─► Run health checks
       └─► Blue-Green swap to production
```

---

## 🎯 Deployment Strategies

### **Development Environment**
- **Trigger**: Push to `develop` branch
- **Strategy**: Direct deployment (no slots)
- **Purpose**: Fast iteration, testing
- **Cost**: ~$2/month (Consumption plan)

### **Staging Environment**
- **Trigger**: Push to `main` branch
- **Strategy**: Slot deployment with swap
  1. Deploy to `staging` slot
  2. Run smoke tests
  3. Swap staging → production
- **Purpose**: Pre-production validation
- **Cost**: ~$150/month (Elastic Premium EP1 - supports slots)

### **Production Environment**
- **Trigger**: Manual workflow dispatch (requires approval)
- **Strategy**: Blue-Green deployment
  1. Deploy to `blue` slot
  2. Run health checks
  3. Blue-Green swap
  4. Keep green slot as rollback option
- **Purpose**: Zero-downtime production deployment
- **Cost**: ~$150/month (Elastic Premium EP1 - supports slots)

---

## 🔧 Environment Configuration

### GitHub Environments

You need to create these environments in GitHub:
**Settings → Environments → New environment**

1. **development** (no protection rules needed)
2. **staging** (optional: require reviewer)
3. **production** (required reviewers + wait timer)

#### Production Environment Setup:
- ✅ Required reviewers: 1-2 people
- ✅ Wait timer: 5 minutes (optional)
- ✅ Deployment branches: Selected branches (`main` only)

---

## 🔐 Required Secrets

### GitHub Secrets (Settings → Secrets and variables → Actions)

| Secret | Required For | Description |
|--------|-------------|-------------|
| `AZURE_CREDENTIALS` | All | Azure service principal JSON |
| `STORAGE_ACCOUNT_NAME` | All | Storage account name (optional) |
| `CODECOV_TOKEN` | Coverage | Codecov upload token (optional) |
| `GIST_SECRET` | Badge | GitHub PAT for gist (optional) |
| `GIST_ID` | Badge | Gist ID for coverage badge (optional) |

#### Getting Codecov Token:
1. Go to [codecov.io](https://codecov.io)
2. Sign in with GitHub
3. Add your repository
4. Copy the upload token

#### Getting Gist Secret:
1. Go to GitHub → Settings → Developer settings → Personal access tokens
2. Generate new token (classic)
3. Select scope: `gist`
4. Copy token

#### Getting Gist ID:
1. Create a new gist at [gist.github.com](https://gist.github.com)
2. Name it: `fintech-coverage-badge.json`
3. Content: `{"schemaVersion": 1}`
4. Copy the gist ID from URL

---

## 🚀 Deployment Flows

### Flow 1: Feature Development
```bash
git checkout -b feature/my-feature
git commit -m "Add feature"
git push origin feature/my-feature
```
**Result**: CI runs (build + test + CodeQL), no deployment

### Flow 2: Deploy to Dev
```bash
git checkout develop
git merge feature/my-feature
git push origin develop
```
**Result**: 
- CI runs
- Deploys to **Dev** environment
- No approval needed

### Flow 3: Deploy to Staging
```bash
git checkout main
git merge develop
git push origin main
```
**Result**:
- CI runs
- Deploys to **Staging** environment
- Uses slot swap (staging → production)

### Flow 4: Deploy to Production
```bash
# Go to GitHub → Actions → CI/CD Pipeline → Run workflow
# Select: environment = prod
```
**Result**:
- CI runs
- Waits for approval (if configured)
- Deploys to **Production** environment
- Uses Blue-Green deployment (blue → production)

---

## 🎨 Coverage Badge

Add this to your `README.md`:

```markdown
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/fintech-coverage-badge.json)
```

Replace:
- `YOUR_USERNAME` - Your GitHub username
- `YOUR_GIST_ID` - The gist ID you created

---

## 🔍 CodeQL Security Scanning

### What it does:
- Scans for security vulnerabilities
- Checks for code quality issues
- Runs extended security queries
- Creates alerts in Security tab

### View results:
**Repository → Security → Code scanning alerts**

### Queries run:
- `security-extended` - Extra security checks
- `security-and-quality` - Code quality + security

---

## 📈 Code Coverage

### Coverage Report Locations:

1. **GitHub Actions Artifacts**
   - Actions → Workflow run → Artifacts → `coverage-report`
   - Download and open `index.html`

2. **Codecov Dashboard**
   - [codecov.io](https://codecov.io) → Your repository
   - Visual coverage trends and reports

3. **Coverage Badge**
   - Displayed in README
   - Updates automatically on push to main

### Coverage Targets:
- 🟢 Green: ≥80%
- 🟡 Yellow: 60-79%
- 🔴 Red: <60%

---

## 🔄 Slot Deployment Details

### Staging Slot Flow:
```
1. Deploy to staging slot
   ↓
2. Warm up slot (30 seconds)
   ↓
3. Run smoke tests
   ↓
4. Swap staging ↔ production
   ↓
5. Users now hit new version
```

### Production Blue-Green Flow:
```
1. Deploy to blue slot
   ↓
2. Warm up slot (30 seconds)
   ↓
3. Run health checks
   ↓
4. Swap blue ↔ production (green)
   ↓
5. Users now hit new version
6. Green slot kept for rollback
```

### Rollback (if needed):
```bash
# Swap back to previous version
az functionapp deployment slot swap \
  --name fintech-func-prod \
  --resource-group fintech-rg-prod \
  --slot blue \
  --target-slot production
```

---

## 💰 Cost Breakdown

### Development
- Function App: Consumption (~$2/month)
- Service Bus: Basic (~$1/month)
- Storage: Cool tier (~$3/month)
- App Insights: 30-day retention (~$5/month)
- **Total: ~$11/month**

### Staging
- Function App: Elastic Premium EP1 (~$150/month)
- Service Bus: Basic (~$1/month)
- Storage: Cool tier (~$3/month)
- App Insights: 30-day retention (~$5/month)
- **Total: ~$159/month**

### Production
- Function App: Elastic Premium EP1 (~$150/month)
- Service Bus: Standard (~$10/month)
- Storage: Cool tier (~$5/month)
- App Insights: 30-day retention (~$10/month)
- **Total: ~$175/month**

### **Grand Total: ~$345/month** for all environments

**Cost Optimization:**
- Keep Dev on Consumption plan
- Use Staging only when needed (delete when not in use)
- Production is required for zero-downtime

---

## 🛡️ Security Features

### 1. CodeQL Analysis
- ✅ Runs on every push/PR
- ✅ Security-extended queries
- ✅ Automatic vulnerability detection

### 2. Secret Masking
- ✅ All secrets are masked in logs
- ✅ Service Bus connections hidden
- ✅ No credentials exposed

### 3. HTTPS Only
- ✅ All function apps HTTPS enforced
- ✅ TLS 1.2 minimum
- ✅ FTPS only for file uploads

### 4. Environment Protection
- ✅ Production requires approval
- ✅ Staging optional approval
- ✅ Audit trail for deployments

---

## 📋 Pre-Deployment Checklist

Before first deployment:

- [ ] Create GitHub environments (development, staging, production)
- [ ] Configure production environment protection rules
- [ ] Add required secrets to GitHub
- [ ] Run `setup-azure-credentials.ps1` to create service principal
- [ ] (Optional) Set up Codecov account
- [ ] (Optional) Create gist for coverage badge
- [ ] Update README with coverage badge
- [ ] Review Bicep template parameters
- [ ] Verify Azure subscription has budget alerts

---

## 🐛 Troubleshooting

### Issue: CodeQL fails
**Solution**: Ensure .NET SDK is properly installed in build step

### Issue: Coverage badge not updating
**Check**: 
- GIST_SECRET is valid GitHub PAT with `gist` scope
- GIST_ID is correct
- Gist is public

### Issue: Slot swap fails
**Solution**: 
- Ensure app is on Elastic Premium plan (not Consumption)
- Check slot exists before swapping
- Verify slot configuration is correct

### Issue: Deployment to production not starting
**Check**:
- Manual workflow dispatch was used
- Environment = 'prod' was selected
- Approval was granted (if required)

### Issue: Health checks failing
**Solution**:
- Add `/api/health` endpoint to your Functions
- Increase warm-up time (currently 30 seconds)
- Check Application Insights for errors

---

## 🎓 Best Practices

1. ✅ Always deploy to Dev first
2. ✅ Test in Staging before Production
3. ✅ Use manual trigger for Production
4. ✅ Monitor Application Insights after deployment
5. ✅ Keep coverage above 80%
6. ✅ Review CodeQL alerts immediately
7. ✅ Use slot swaps for zero-downtime
8. ✅ Keep blue/green slot for rollback
9. ✅ Set up budget alerts
10. ✅ Review deployment logs regularly

---

## 📞 Quick Commands

### View deployed functions:
```bash
# Dev
az functionapp list --resource-group fintech-rg-dev --output table

# Staging
az functionapp list --resource-group fintech-rg-staging --output table

# Production
az functionapp list --resource-group fintech-rg-prod --output table
```

### Check deployment slots:
```bash
az functionapp deployment slot list \
  --name fintech-func-prod \
  --resource-group fintech-rg-prod \
  --output table
```

### View logs:
```bash
az functionapp log tail \
  --name fintech-func-prod \
  --resource-group fintech-rg-prod
```

### Manual rollback:
```bash
az functionapp deployment slot swap \
  --name fintech-func-prod \
  --resource-group fintech-rg-prod \
  --slot blue \
  --target-slot production
```

---

**Status**: ✅ Enhanced CI/CD Ready  
**Last Updated**: November 24, 2025  
**Supports**: Multi-environment, Slot Swaps, Zero-Downtime Deployment
