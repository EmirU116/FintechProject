# 🚀 Quick Start - Running Unit Tests

## Prerequisites
- .NET 8.0 SDK installed
- Project restored (`dotnet restore`)

## Run All Tests

```powershell
# From project root
cd test/FintechProject.Tests
dotnet test
```

## Run with More Details

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## Run Specific Test Class

```powershell
# Run only TransactionValidatorTests
dotnet test --filter "FullyQualifiedName~TransactionValidatorTests"

# Run only MoneyTransferServiceTests
dotnet test --filter "FullyQualifiedName~MoneyTransferServiceTests"

# Run only TransactionProcessorTests
dotnet test --filter "FullyQualifiedName~TransactionProcessorTests"
```

## Run Specific Test Method

```powershell
# Run a single test
dotnet test --filter "FullyQualifiedName~TransferMoneyAsync_WithValidCards_ShouldSucceed"
```

## Watch Mode (Auto-run on changes)

```powershell
dotnet watch test
```

## Expected Output

```
✅ Test Run Successful.
   Total tests: 51
   Passed: 51
   Total time: ~3 seconds
```

## Test Files

1. **TransactionValidatorTests.cs** - 14 tests
2. **MoneyTransferServiceTests.cs** - 14 tests  
3. **TransactionProcessorTests.cs** - 14 tests

## Reading Test Results

### ✅ Passing Test
```
✓ TransactionValidatorTests.ValidateTransaction_WithValidData_ShouldReturnTrue
```

### ❌ Failing Test (if any)
```
✗ TransactionValidatorTests.ValidateTransaction_WithValidData_ShouldReturnTrue
  Expected result.IsValid to be true, but found false.
  at TransactionValidatorTests.cs:line 45
```

## IDE Integration

### Visual Studio
1. Test → Test Explorer
2. Click "Run All Tests"

### VS Code
1. Install C# Dev Kit
2. Click test beaker icon
3. Select tests to run

### JetBrains Rider
1. View → Tool Windows → Unit Tests
2. Run All

## Common Commands

```powershell
# Build before testing
dotnet build

# Clean and rebuild
dotnet clean
dotnet build
dotnet test

# Run from solution root (runs all test projects)
dotnet test FintechProject.sln
```

## Troubleshooting

**Issue:** `error MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found.`  
**Fix:** Install .NET 8.0 SDK

**Issue:** `error CS0246: The type or namespace name could not be found`  
**Fix:** Run `dotnet restore`

**Issue:** Tests hang  
**Fix:** Make sure no background processes are locking files

## Learn More

See `test/FintechProject.Tests/README.md` for detailed explanation of:
- What unit testing is
- How mocking works
- Test structure (AAA pattern)
- Best practices

## Quick Test Explanation

### What These Tests Do:

1. **Validation Tests** - Check if inputs are valid
   - Card numbers must be 16 digits
   - Amounts must be positive
   - Currencies must be valid codes

2. **Money Transfer Tests** - Test money movement
   - Can transfer between valid cards
   - Can't transfer with insufficient funds
   - Can't use blocked/expired cards

3. **Transaction Processing Tests** - Test payment processing
   - Process valid card payments
   - Reject invalid cards
   - Handle different card types (Visa, Mastercard, AmEx)

**All tests run in memory - no database required!** 🚀
