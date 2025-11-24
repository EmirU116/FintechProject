# ✅ Local Database Configuration

This project is configured to use a **local PostgreSQL database** for development.

---

## 🎯 What Changed in CI/CD

### ✅ GitHub Actions (`.github/workflows/ci-cd.yml`)
- ❌ **REMOVED**: `PostgresConnection` from Function App settings
- ✅ **NOTE ADDED**: Comments explain local database usage
- ✅ **RESULT**: Azure Functions won't have database access (as expected for local dev)

### ✅ Azure DevOps Pipeline (`azure-pipelines.yml`)
- ❌ **DISABLED**: Entire `DeployDatabase` stage (commented out)
- ❌ **DISABLED**: Database scripts artifact publishing (commented out)
- ❌ **REMOVED**: `PostgresConnection` from Function App settings
- ✅ **RESULT**: No database deployment attempts

### ✅ Setup Script (`setup-azure-credentials.ps1`)
- ⚠️ **MARKED OPTIONAL**: PostgreSQL connection string secret
- ⚠️ **MARKED OPTIONAL**: PostgreSQL password secret
- ✅ **RESULT**: Clear guidance that these are only for Azure PostgreSQL

---

## 🔐 Required Secrets (Updated)

### For GitHub Actions:

| Secret | Required? | Purpose |
|--------|-----------|---------|
| `AZURE_CREDENTIALS` | ✅ YES | Deploy Azure resources |
| `STORAGE_ACCOUNT_NAME` | ✅ YES | Azure Functions storage |
| ~~`POSTGRES_CONNECTION_STRING`~~ | ❌ NO | Not needed for local DB |
| ~~`POSTGRES_PASSWORD`~~ | ❌ NO | Not needed for local DB |

### For Azure DevOps:

| Variable | Required? | Purpose |
|----------|-----------|---------|
| `subscriptionId` | ✅ YES | Azure subscription |
| `StorageAccountName` | ✅ YES | Azure Functions storage |
| ~~`PostgresConnectionString`~~ | ❌ NO | Not needed for local DB |
| ~~`PostgresPassword`~~ | ❌ NO | Not needed for local DB |

---

## 🏗️ Current Architecture

```
┌──────────────────────────────────┐
│  Local Development                │
│  ├─ PostgreSQL (localhost:5432)  │ ← Local database (manual management)
│  ├─ Azure Functions (local)      │ ← Connects to local DB
│  └─ Service Bus (local/Azure)    │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  Azure Production (CI/CD Deploy) │
│  ├─ Function App                 │ ← No database connection
│  ├─ Service Bus                  │ ← Deployed by CI/CD
│  ├─ Storage Account              │ ← Deployed by CI/CD
│  └─ Application Insights         │ ← Deployed by CI/CD
└──────────────────────────────────┘
```

---

## 📋 What CI/CD Does Now

### ✅ Automated:
- Build and test .NET solution
- Deploy Azure infrastructure (Functions, Service Bus, Storage)
- Deploy function code
- Configure app settings (without database)

### ❌ NOT Automated:
- Database creation
- Database migrations
- Database schema updates
- Database seeding

### 👉 Manual Tasks:
- Run `database/setup.sql` locally
- Run `database/add_credit_cards_table.sql` locally
- Manage local PostgreSQL yourself

---

## 🚀 When CI/CD Runs Successfully

**What gets deployed to Azure:**
1. ✅ Azure Function App
2. ✅ Service Bus namespace and queue
3. ✅ Storage Account
4. ✅ Application Insights

**What does NOT get deployed:**
1. ❌ PostgreSQL database (stays local)
2. ❌ Database schema
3. ❌ Database migrations

**Expected behavior:**
- Functions will deploy successfully
- Functions **won't access database** (connection not configured)
- Service Bus will work
- If functions try to access database → will fail (expected)

---

## 🔄 Migrating to Azure PostgreSQL (Future)

When you want to use a cloud database:

### Step 1: Create Azure PostgreSQL
```bash
az postgres flexible-server create \
  --name fintech-postgres \
  --resource-group fintech-rg \
  --location eastus \
  --admin-user pgadmin \
  --admin-password YourPassword123! \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --public-access 0.0.0.0
```

### Step 2: Add Secrets
**GitHub:**
- Add `POSTGRES_CONNECTION_STRING`
- Add `POSTGRES_PASSWORD`

**Azure DevOps:**
- Add `PostgresConnectionString` variable
- Add `PostgresPassword` variable (marked secret)

### Step 3: Update CI/CD
**GitHub Actions (`.github/workflows/ci-cd.yml`):**
```yaml
# Uncomment this line in deploy-function-app job:
"PostgresConnection=${{ secrets.POSTGRES_CONNECTION_STRING }}" \
```

**Azure DevOps (`azure-pipelines.yml`):**
```yaml
# Uncomment the entire DeployDatabase stage (lines 88-124)
# Uncomment database artifact publishing (lines 75-77)
# Uncomment PostgresConnection in app settings (line 155)
```

### Step 4: Migrate Data
```bash
# Export local
pg_dump -h localhost -U postgres fintech > backup.sql

# Import to Azure
psql "Host=fintech-postgres.postgres.database.azure.com..." < backup.sql
```

---

## 💰 Cost Impact

**Current setup (local database):**
- Azure Functions: ~$2/month (Consumption Plan)
- Service Bus: ~$1/month (Basic tier)
- Storage: ~$3/month (Cool tier)
- App Insights: ~$5/month (with 30-day retention)
- **Total: ~$11/month** ✅

**With Azure PostgreSQL:**
- Add ~$12/month (Burstable B1ms tier)
- **Total: ~$23/month**

---

## ✅ Verification Checklist

After CI/CD runs:

- [ ] GitHub Actions shows all green checkmarks
- [ ] Azure Function App is running
- [ ] Service Bus queue exists
- [ ] Storage Account created
- [ ] Application Insights receiving telemetry
- [ ] Local database still accessible at localhost:5432
- [ ] NO database deployment errors (because it's disabled)
- [ ] Functions deploy successfully (even without DB connection)

---

## 🐛 Troubleshooting

### Issue: "Function can't connect to database"
**Expected!** Functions in Azure have no database connection configured.
**Solution:** Either use local functions OR migrate to Azure PostgreSQL.

### Issue: "POSTGRES_CONNECTION_STRING secret not found"
**Not an issue!** This secret is optional for local database setup.
**Solution:** Ignore or set to placeholder: `Host=localhost;Database=fintech`

### Issue: "Database deployment failed"
**Should not happen!** Database deployment is disabled.
**Solution:** Check that `DeployDatabase` stage is commented out.

---

## 📞 Quick Reference

**Local database management:**
```bash
# Start PostgreSQL
# (depends on your installation method)

# Run migrations manually
psql -h localhost -U postgres -d fintech -f database/setup.sql
psql -h localhost -U postgres -d fintech -f database/add_credit_cards_table.sql

# Check tables
psql -h localhost -U postgres -d fintech -c "\dt"
```

**CI/CD deployment:**
```bash
# Commit and push
git add .
git commit -m "Your changes"
git push origin main

# Watch in GitHub Actions tab
# Functions deploy WITHOUT database connection
```

---

**Status:** ✅ Configured for local database  
**Last Updated:** November 24, 2025  
**Migration to Azure PostgreSQL:** Not started
