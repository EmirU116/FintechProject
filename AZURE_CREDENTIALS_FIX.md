# 🔐 Azure Credentials Setup - Quick Fix Guide

## Problem
The GitHub Actions workflow failed at the Azure Login step because the `AZURE_CREDENTIALS` secret is either:
- Missing from your repository
- Incomplete (missing required fields)
- Incorrectly formatted

## ⚡ Quick Solution

### Option 1: Automated Setup (RECOMMENDED)

Run the provided PowerShell script to automatically create the service principal and generate the credentials:

```powershell
# Run this in PowerShell
.\setup-azure-credentials.ps1
```

This script will:
1. ✅ Check Azure CLI installation
2. ✅ Login to Azure
3. ✅ Create a service principal with proper permissions
4. ✅ Generate the AZURE_CREDENTIALS JSON
5. ✅ Save credentials to a file for easy copying

**After running the script:**
1. Copy the JSON output from the terminal or from `azure-credentials-output.txt`
2. Go to GitHub: Your Repository → **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Name: `AZURE_CREDENTIALS`
5. Value: Paste the entire JSON
6. Click **"Add secret"**
7. **Delete** `azure-credentials-output.txt` for security

---

### Option 2: Manual Setup

If you prefer manual setup or the script doesn't work:

#### Step 1: Install Azure CLI (if not installed)
```powershell
# Windows (using winget)
winget install Microsoft.AzureCLI

# Or download from: https://aka.ms/installazurecliwindows
```

#### Step 2: Login to Azure
```powershell
az login
```

#### Step 3: Get Your Subscription ID
```powershell
# List all subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"

# Verify
az account show
```

#### Step 4: Create Service Principal
```powershell
# Replace {subscription-id} with your actual subscription ID
az ad sp create-for-rbac `
  --name "github-actions-fintech" `
  --role contributor `
  --scopes /subscriptions/{subscription-id} `
  --sdk-auth
```

**Example:**
```powershell
az ad sp create-for-rbac `
  --name "github-actions-fintech" `
  --role contributor `
  --scopes /subscriptions/12345678-1234-1234-1234-123456789abc `
  --sdk-auth
```

#### Step 5: Copy the JSON Output

The command will output JSON like this:

```json
{
  "clientId": "abcd1234-ab12-cd34-ef56-abcdef123456",
  "clientSecret": "your-secret-here-keep-this-safe",
  "subscriptionId": "12345678-1234-1234-1234-123456789abc",
  "tenantId": "87654321-4321-4321-4321-fedcba987654",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**⚠️ IMPORTANT:** Copy the **ENTIRE JSON** (including all fields). All fields are required.

---

## 📝 Adding the Secret to GitHub

### Step-by-Step:

1. **Navigate to your GitHub repository**
   - URL: `https://github.com/YOUR_USERNAME/YOUR_REPO`

2. **Go to Settings**
   - Click the **Settings** tab (top navigation)

3. **Open Secrets and Variables**
   - In the left sidebar, expand **Secrets and variables**
   - Click **Actions**

4. **Create New Secret**
   - Click the green **"New repository secret"** button

5. **Enter Secret Details**
   - **Name:** `AZURE_CREDENTIALS`
   - **Value:** Paste the entire JSON from Step 4
   - Click **"Add secret"**

### Screenshot Reference:
```
GitHub Repo → Settings → Secrets and variables → Actions → New repository secret

┌─────────────────────────────────────────────────┐
│ Name: AZURE_CREDENTIALS                         │
├─────────────────────────────────────────────────┤
│ Secret:                                         │
│ {                                               │
│   "clientId": "...",                            │
│   "clientSecret": "...",                        │
│   "subscriptionId": "...",                      │
│   "tenantId": "...",                            │
│   ...                                           │
│ }                                               │
└─────────────────────────────────────────────────┘
              [Add secret]
```

---

## ✅ Verification

### Test the Secret:

1. **Trigger the workflow:**
   ```bash
   git commit --allow-empty -m "Test Azure credentials"
   git push origin main
   ```

2. **Monitor the workflow:**
   - Go to **Actions** tab in GitHub
   - Watch the "Azure Login" step
   - Should see: ✅ "Login successful"

### Expected Success Output:
```
Run azure/login@v2
  with:
    creds: ***
✓ Azure login successful
✓ Subscription: Your Subscription Name (12345678-1234-1234-1234-123456789abc)
```

---

## 🔍 Troubleshooting

### Error: "Could not find required values"
**Cause:** Missing fields in AZURE_CREDENTIALS  
**Fix:** Ensure you copied the **entire JSON** including:
- `clientId`
- `clientSecret`
- `subscriptionId`
- `tenantId`
- All other endpoint URLs

### Error: "Unauthorized"
**Cause:** Service principal doesn't have permissions  
**Fix:** 
```powershell
# Grant contributor role
az role assignment create `
  --assignee YOUR_CLIENT_ID `
  --role Contributor `
  --scope /subscriptions/YOUR_SUBSCRIPTION_ID
```

### Error: "Service principal not found"
**Cause:** Service principal was deleted or not created  
**Fix:** Recreate using Step 4 above

### Error: "Subscription not found"
**Cause:** Wrong subscription ID or no access  
**Fix:** 
```powershell
# List available subscriptions
az account list --output table

# Verify access
az account show --subscription YOUR_SUBSCRIPTION_ID
```

---

## 🔒 Security Best Practices

1. **Never commit** credentials to git
2. **Use GitHub Secrets** for all sensitive data
3. **Rotate credentials** every 90 days
4. **Use least privilege** - only grant necessary permissions
5. **Monitor usage** - check Azure AD sign-in logs
6. **Delete unused** service principals

### Rotate Credentials:
```powershell
# Delete old service principal
az ad sp delete --id YOUR_CLIENT_ID

# Create new one (follow Step 4 above)
az ad sp create-for-rbac ...

# Update GitHub secret with new credentials
```

---

## 📚 Additional Resources

- [Azure Service Principal Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
- [GitHub Actions Azure Login](https://github.com/marketplace/actions/azure-login)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/)
- [CI/CD Setup Guide](./CICD_SETUP.md) - Full pipeline setup

---

## 🎯 Quick Reference

**Required Secret:**
```
Name: AZURE_CREDENTIALS
Type: JSON
Required Fields:
  - clientId
  - clientSecret
  - subscriptionId
  - tenantId
```

**Where to Add:**
```
GitHub Repo → Settings → Secrets and variables → Actions → New repository secret
```

**How to Generate:**
```powershell
# Quick command (replace {subscription-id})
az ad sp create-for-rbac --name "github-actions-fintech" --role contributor --scopes /subscriptions/{subscription-id} --sdk-auth
```

---

## ✨ Summary

1. ✅ Run `setup-azure-credentials.ps1` (automated)
   OR manually create service principal with Azure CLI
2. ✅ Copy the entire JSON output
3. ✅ Add as `AZURE_CREDENTIALS` secret in GitHub
4. ✅ Push to main branch to trigger workflow
5. ✅ Verify deployment succeeds

**Need Help?** Check [CICD_SETUP.md](./CICD_SETUP.md) for detailed CI/CD documentation.

---

**Last Updated:** November 25, 2025  
**Status:** ✅ Ready to use
