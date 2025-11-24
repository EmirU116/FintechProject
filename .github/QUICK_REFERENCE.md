# 🚀 CI/CD Quick Reference

## Pipeline Summary

```
Push to main
     │
     ├─► CI: Build + Test + Coverage (≥80%) + CodeQL
     │   └─► ❌ FAIL if coverage <80%
     │   └─► ✅ PASS if coverage ≥80%
     │
     └─► CD: Deploy to Azure (~$1-10/month)
         └─► Bicep infrastructure + Function deployment
```

---

## ✅ Requirements

| Check | Requirement | Status |
|-------|-------------|--------|
| 🧪 | Unit tests must pass | Required |
| 📊 | Code coverage ≥ 80% | **Required** |
| 🔒 | CodeQL security scan | Required |
| ☁️ | Azure credentials set | Required for CD |

---

## 🎯 Coverage Threshold: 80%

### Quick Check:
```bash
# Run tests locally
dotnet test --collect:"XPlat Code Coverage"

# Check percentage
cat coverage/*/coverage.cobertura.xml | Select-String "line-rate"
```

### Build Outcome:

| Coverage | Badge | Build | Deploy |
|----------|-------|-------|--------|
| ≥80% | 🟢 | ✅ Pass | ✅ Yes |
| 60-79% | 🟡 | ❌ Fail | ❌ No |
| 40-59% | 🟠 | ❌ Fail | ❌ No |
| <40% | 🔴 | ❌ Fail | ❌ No |

---

## 📝 Before You Push

### Pre-Push Checklist:

```bash
# 1. Run tests locally
dotnet test

# 2. Check coverage
dotnet test --collect:"XPlat Code Coverage"
# Look for: "Line coverage: 8X%"

# 3. Build in Release mode
dotnet build --configuration Release

# 4. All checks pass? Push!
git push origin main
```

---

## 🔧 Common Commands

### Local Testing:
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator `
  -reports:"coverage/**/coverage.cobertura.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:Html

# Open report
start coveragereport/index.html
```

### Check Azure Resources:
```bash
# Login
az login

# List resources
az resource list --resource-group fintech-rg --output table

# View Function App
az functionapp show `
  --name fintech-func-free `
  --resource-group fintech-rg

# View logs
az functionapp log tail `
  --name fintech-func-free `
  --resource-group fintech-rg
```

### Check CI/CD Status:
```bash
# View latest workflow run
gh run list --limit 5

# View workflow details
gh run view

# Watch workflow in real-time
gh run watch
```

---

## 🚨 When Build Fails

### Coverage <80%

**Error Message:**
```
❌ Coverage 75% is below threshold 80%
Error: Code coverage (75%) is below the required threshold (80%)
```

**Fix:**
1. Download coverage report from Actions artifacts
2. Open `coveragereport/index.html`
3. Find files with low coverage (red/orange)
4. Write tests for uncovered code
5. Verify locally: `dotnet test --collect:"XPlat Code Coverage"`
6. Push again

**Example:**
```csharp
// Add tests for uncovered methods
[Fact]
public void NewMethod_ShouldReturnExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = _service.NewMethod(input);
    
    // Assert
    Assert.Equal("expected", result);
}
```

### Tests Fail

**Error Message:**
```
Failed!  - Failed:     2, Passed:    15, Skipped:     0, Total:    17
```

**Fix:**
1. Check test output in Actions logs
2. Run failing test locally: `dotnet test --filter FullyQualifiedName~TestName`
3. Debug and fix
4. Push again

### CodeQL Issues

**Error Message:**
```
⚠️ CodeQL found 3 potential security issues
```

**Fix:**
1. View CodeQL results: Security → Code scanning alerts
2. Review each alert
3. Fix security issues
4. Push again

### Deployment Fails

**Error Message:**
```
❌ Bicep deployment failed
```

**Fix:**
1. Check Azure credentials: `echo ${{ secrets.AZURE_CREDENTIALS }}`
2. Verify resource group exists: `az group show -n fintech-rg`
3. Check deployment logs in Actions
4. Fix infrastructure issues
5. Re-run workflow

---

## 📊 Pipeline Stages

### Stage 1: Build & Test (~2 min)
- Restore NuGet packages
- Build solution
- Run unit tests
- Collect coverage
- **GATE: Coverage must be ≥80%**

### Stage 2: CodeQL Scan (~1 min)
- Initialize CodeQL
- Build for analysis
- Scan for vulnerabilities
- Upload results to GitHub Security

### Stage 3: Coverage Reporting (~30 sec)
- Generate HTML/Cobertura reports
- Upload to Codecov (optional)
- Update coverage badge (optional)
- Upload artifacts

### Stage 4: Deploy (~5 min)
- Login to Azure
- Deploy Bicep infrastructure
- Deploy Function App code
- Configure app settings
- Verify deployment

**Total Time: ~8-9 minutes**

---

## 🎯 Coverage Tips

### What to Test:
✅ Business logic methods  
✅ Validation rules  
✅ Error handling  
✅ Edge cases (null, empty, negative)  
✅ Data transformations  

### What NOT to Test:
❌ Azure Functions HTTP bindings  
❌ Simple DTOs/POCOs  
❌ Configuration classes  
❌ Third-party libraries  

### Coverage Techniques:

**1. Test Happy Path**
```csharp
[Fact]
public void Transfer_Succeeds_WhenValidInput()
{
    var result = _service.Transfer(100, "USD");
    Assert.True(result.Success);
}
```

**2. Test Error Cases**
```csharp
[Fact]
public void Transfer_Throws_WhenAmountNegative()
{
    Assert.Throws<ArgumentException>(() => 
        _service.Transfer(-100, "USD"));
}
```

**3. Test Edge Cases**
```csharp
[Theory]
[InlineData(0)]
[InlineData(0.01)]
[InlineData(999999.99)]
public void Transfer_HandlesEdgeCases(decimal amount)
{
    var result = _service.Transfer(amount, "USD");
    Assert.NotNull(result);
}
```

---

## 📈 Monitoring

### GitHub Actions:
- **Actions tab** → View all workflow runs
- **Green checkmark** = All passed
- **Red X** = Failed (check logs)
- **Yellow circle** = In progress

### Azure Portal:
- **Function App** → Monitor executions
- **Application Insights** → View telemetry
- **Cost Management** → Track spending
- **Resource Group** → View all resources

### Coverage Trends:
- **Actions** → Latest workflow → Artifacts → coverage-report
- Download and open `index.html`
- Track coverage over time

---

## 🎨 Status Badges

Add to your `README.md`:

```markdown
![Build Status](https://github.com/EmirU116/FintechProject/actions/workflows/ci-cd.yml/badge.svg)
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/fintech-coverage-badge.json)
![CodeQL](https://github.com/EmirU116/FintechProject/workflows/CodeQL/badge.svg)
```

Displays:
- 🟢 Build: Passing
- 🟢 Coverage: 85%
- 🟢 CodeQL: No issues

---

## 💰 Cost Monitoring

### Expected Monthly Cost: ~$1-10

```bash
# Check current month
az consumption usage list --output table

# Set budget alert
az consumption budget create `
  --budget-name personal-limit `
  --amount 20 `
  --time-grain Monthly `
  --resource-group fintech-rg
```

### Cost Breakdown:
- Function App (Consumption): $0-2
- Service Bus (Basic): $0.05
- Storage (Cool): $1-3
- App Insights: $0-5

---

## 🔑 Required Secrets

| Secret | Required | Purpose |
|--------|----------|---------|
| `AZURE_CREDENTIALS` | ✅ Yes | Azure deployment |
| `CODECOV_TOKEN` | ❌ Optional | Coverage upload |
| `GIST_SECRET` | ❌ Optional | Coverage badge |
| `GIST_ID` | ❌ Optional | Coverage badge |

---

## 📚 Documentation

- **FREE_CICD.md** - Main setup guide
- **COVERAGE_ENFORCEMENT.md** - Coverage details ⭐
- **CICD_SETUP.md** - Detailed technical setup
- **CICD_QUICKSTART.md** - Fast setup
- **BADGES.md** - Status badge setup

---

## 🆘 Need Help?

1. **Check coverage**: `dotnet test --collect:"XPlat Code Coverage"`
2. **View logs**: GitHub Actions → Latest workflow → View logs
3. **Check Azure**: Azure Portal → Function App → Logs
4. **Review docs**: `.github/COVERAGE_ENFORCEMENT.md`

---

**Quick Start**: `.\setup-azure-credentials.ps1` → Add secrets → Push to main → Done! 🎉

**Status**: ✅ CI/CD with 80% Coverage Enforcement Ready  
**Last Updated**: November 24, 2025
