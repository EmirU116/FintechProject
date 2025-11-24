# 📊 Code Coverage Enforcement (≥80%)

## Overview

Your CI/CD pipeline now **enforces 80% minimum code coverage** for all commits to `main`. This ensures high code quality and catches untested code early.

---

## 🎯 Coverage Requirements

| Metric | Threshold | Action |
|--------|-----------|--------|
| **Line Coverage** | ≥ 80% | ✅ Pass / ❌ Fail |
| **Branch Coverage** | Not enforced | 📊 Reported only |
| **Method Coverage** | Not enforced | 📊 Reported only |

---

## 🔍 How It Works

### 1. **Test Execution**
```bash
dotnet test --collect:"XPlat Code Coverage"
```
- Runs all unit tests in `test/FintechProject.Tests/`
- Collects coverage data using Coverlet

### 2. **Coverage Report Generation**
```bash
ReportGenerator creates:
- HTML report (human-readable)
- Cobertura XML (machine-readable)
- Badges (visual status)
- Markdown summary
```

### 3. **Coverage Extraction**
```bash
# Extract from Summary.md
Line coverage: 85.2%

# Or parse from Cobertura.xml
<coverage line-rate="0.852" branch-rate="0.743">
```

### 4. **Threshold Enforcement**
```bash
if coverage < 80%:
    echo "❌ Coverage {coverage}% is below threshold 80%"
    exit 1  # FAIL THE BUILD
else:
    echo "✅ Coverage {coverage}% meets threshold 80%"
    # CONTINUE TO DEPLOYMENT
```

### 5. **Badge Update** (Optional)
- 🟢 Green badge if ≥80%
- 🟡 Yellow badge if 60-79%
- 🟠 Orange badge if 40-59%
- 🔴 Red badge if <40%

---

## ✅ What Passes CI

### Example: Good Coverage (85%)

```
src/Core/MoneyTransferService.cs     92% ✅
src/Core/TransactionValidator.cs     88% ✅
src/Core/TransactionProcessor.cs     81% ✅
---------------------------------------------
Total Line Coverage:                  85% ✅

🎉 Build passes, deployment proceeds
```

### Example: Failed Coverage (75%)

```
src/Core/MoneyTransferService.cs     92% ✅
src/Core/TransactionValidator.cs     65% ⚠️
src/Core/TransactionProcessor.cs     68% ⚠️
---------------------------------------------
Total Line Coverage:                  75% ❌

❌ Build fails, PR cannot merge
```

---

## 📈 Current Coverage Status

### By File:

Check latest report: **Actions → Latest workflow → Artifacts → coverage-report**

```
Core/
├── MoneyTransferService.cs        ??%
├── TransactionValidator.cs        ??%
├── TransactionProcessor.cs        ??%
└── Database/
    ├── TransactionRepository.cs   ??%
    └── CreditCardRepository.cs    ??%
```

### By Category:

| Category | Coverage | Status |
|----------|----------|--------|
| **Business Logic** | ??% | ? |
| **Validation** | ??% | ? |
| **Data Access** | ??% | ? |
| **Functions** | ??% | ? |

**Run tests locally to check your coverage!**

---

## 🚀 Check Coverage Locally

### Quick Check:
```bash
# Run tests with coverage
dotnet test test/FintechProject.Tests/FintechProject.Tests.csproj `
  --collect:"XPlat Code Coverage" `
  --results-directory ./coverage

# View results
cat coverage/*/coverage.cobertura.xml | Select-String "line-rate"
```

### Full HTML Report:
```bash
# Install ReportGenerator globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate report
reportgenerator `
  -reports:"coverage/**/coverage.cobertura.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:Html

# Open report
start coveragereport/index.html
```

### Visual Studio:
```
Test → Analyze Code Coverage for All Tests
View → Code Coverage Results
```

### VS Code:
```bash
# Install extension
code --install-extension ryanluker.vscode-coverage-gutters

# Run tests with coverage
# Extension shows covered/uncovered lines in editor
```

---

## 📝 Writing Tests to Increase Coverage

### Find Untested Code:
```bash
# Generate report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coveragereport"

# Open HTML report
start coveragereport/index.html

# Look for files with low coverage (red/orange)
# Click file to see uncovered lines (red highlighting)
```

### Example: Coverage Report Shows

```csharp
// ✅ COVERED (green)
public decimal CalculateFee(decimal amount)
{
    if (amount <= 0) // ✅ tested
        throw new ArgumentException();
    
    return amount * 0.02m; // ✅ tested
}

// ❌ UNCOVERED (red)
public void ProcessRefund(Transaction tx)
{
    if (tx.Status != "completed") // ❌ not tested
        throw new InvalidOperationException();
    
    tx.Status = "refunded"; // ❌ not tested
    _repository.Update(tx); // ❌ not tested
}
```

### Add Missing Tests:
```csharp
[Fact]
public void ProcessRefund_ThrowsException_WhenTransactionNotCompleted()
{
    // Arrange
    var tx = new Transaction { Status = "pending" };
    
    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => 
        _service.ProcessRefund(tx));
}

[Fact]
public void ProcessRefund_UpdatesStatus_WhenTransactionCompleted()
{
    // Arrange
    var tx = new Transaction { Status = "completed" };
    
    // Act
    _service.ProcessRefund(tx);
    
    // Assert
    Assert.Equal("refunded", tx.Status);
    _mockRepo.Verify(r => r.Update(tx), Times.Once);
}
```

---

## 🎯 Coverage Best Practices

### DO Test:
- ✅ Business logic (MoneyTransferService)
- ✅ Validation rules (TransactionValidator)
- ✅ Data transformations (TransactionProcessor)
- ✅ Error handling paths
- ✅ Edge cases (null, empty, negative values)

### DON'T Test:
- ❌ Azure Functions HTTP binding code (integration tests)
- ❌ Database connection strings (configuration)
- ❌ Third-party library internals
- ❌ Simple DTOs/models with no logic

### Aim For:
- **80-90%**: Good coverage, meets threshold
- **90-95%**: Excellent coverage
- **>95%**: Diminishing returns, focus on quality over quantity

---

## 🔧 Adjusting the Threshold

### To Change Coverage Threshold:

Edit `.github/workflows/ci-cd.yml`:

```yaml
- name: Enforce coverage threshold
  run: |
    COVERAGE=${{ steps.coverage.outputs.coverage }}
    THRESHOLD=80  # ← CHANGE THIS VALUE
```

### Recommended Thresholds:

| Project Stage | Threshold | Rationale |
|---------------|-----------|-----------|
| **New project** | 70% | Getting started |
| **Active development** | 80% | ✅ Current setting |
| **Production critical** | 85-90% | High assurance |
| **Legacy/refactoring** | 60% | Incremental improvement |

---

## 🚨 What Happens When Coverage Fails

### On Push to Main:

1. ❌ **Build fails** at coverage check step
2. 🚫 **Deployment blocked** (CD job doesn't run)
3. 📧 **GitHub notification** sent to you
4. 📊 **Coverage report** available in artifacts

### On Pull Request:

1. ❌ **Status check fails** (red X)
2. 🚫 **Cannot merge** (if branch protection enabled)
3. 💬 **Comment on PR** with coverage details
4. 🔄 **Must fix tests** before merging

### How to Fix:

```bash
# 1. Check which files have low coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"report"
start report/index.html

# 2. Write tests for uncovered code
# Add tests to test/FintechProject.Tests/

# 3. Verify coverage locally
dotnet test --collect:"XPlat Code Coverage"
# Should see: "Line coverage: 8X%"

# 4. Commit and push
git add test/
git commit -m "Add tests to increase coverage to 82%"
git push
```

---

## 📊 Coverage Trends

### Track Over Time:

| Date | Commit | Coverage | Change | Status |
|------|--------|----------|--------|--------|
| 2025-11-24 | abc123 | 85.2% | +2.1% | ✅ |
| 2025-11-23 | def456 | 83.1% | -1.5% | ✅ |
| 2025-11-22 | ghi789 | 84.6% | +0.8% | ✅ |

### Goal:

📈 **Maintain ≥80% coverage on every commit**

### Optional: Coverage Badge

```markdown
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/fintech-coverage-badge.json)
```

Shows: ![Coverage](https://img.shields.io/badge/coverage-85.2%25-brightgreen)

---

## 🛠️ Troubleshooting

### Coverage Report Not Generated

**Problem**: No `coverage.cobertura.xml` file

**Solution**:
```bash
# Ensure Coverlet.collector is installed
dotnet add test/FintechProject.Tests package coverlet.collector

# Run with explicit format
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Coverage Lower Than Expected

**Problem**: Shows 45% but you have many tests

**Solution**:
```bash
# Check if all test projects are included
dotnet test FintechProject.sln --collect:"XPlat Code Coverage"

# Ensure tests are actually running
dotnet test --logger "console;verbosity=detailed"

# Check for test exclusions in coverlet
# Remove any [ExcludeFromCodeCoverage] attributes
```

### Badge Not Updating

**Problem**: Coverage badge shows old percentage

**Solution**:
```bash
# Ensure secrets are set:
# - GIST_SECRET (GitHub token with gist scope)
# - GIST_ID (ID from gist URL)

# Badge updates only on push to main
git push origin main

# Check workflow logs for badge creation step
```

### Threshold Too Strict

**Problem**: Cannot reach 80% coverage

**Options**:
1. **Write more tests** (recommended)
2. **Exclude untestable code** (use sparingly)
   ```csharp
   [ExcludeFromCodeCoverage]
   public class Configuration { }
   ```
3. **Lower threshold temporarily** (70-75%)
4. **Focus on critical paths** first

---

## 📚 Further Reading

- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [xUnit Best Practices](https://xunit.net/docs/getting-started)
- [Code Coverage Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

---

## 🎓 Understanding Coverage Metrics

### Line Coverage (Used for threshold)
- Percentage of code lines executed by tests
- **Example**: 85% = 85 out of 100 lines tested

### Branch Coverage
- Percentage of decision branches taken
- **Example**: `if/else` - both paths tested?

### Method Coverage
- Percentage of methods called by tests
- **Example**: 100% = all methods executed at least once

### Your Threshold: **Line Coverage ≥ 80%**

---

**Status**: ✅ 80% Coverage Threshold Enforced  
**Badge Color**: 🟢 ≥80% | 🟡 60-79% | 🟠 40-59% | 🔴 <40%  
**Last Updated**: November 24, 2025
