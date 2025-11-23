# 🧪 Unit Testing Documentation

## Overview

This document explains the unit testing implementation for the Fintech Project. We have created **51 comprehensive unit tests** covering validation, money transfers, and transaction processing.

## Test Results Summary

```
✅ Total Tests: 51
✅ Passed: 51
❌ Failed: 0
⏭️ Skipped: 0
⏱️ Duration: ~3 seconds
```

## Project Structure

```
test/FintechProject.Tests/
├── FintechProject.Tests.csproj      # Test project configuration
├── README.md                         # Detailed beginner's guide
├── TransactionValidatorTests.cs     # 14 tests for validation logic
├── MoneyTransferServiceTests.cs     # 14 tests for money transfers
└── TransactionProcessorTests.cs     # 14 tests for transaction processing
```

## Test Coverage

### 1. TransactionValidatorTests (14 tests)

**Purpose:** Validates transaction input data before processing

**Tests cover:**
- ✅ Valid transactions
- ❌ Null transactions
- ❌ Invalid card numbers (too short, contains letters)
- ✅ Card numbers with and without dashes
- ❌ Negative and zero amounts
- ❌ Invalid currency codes (wrong length, unknown codes)
- ✅ Multiple valid currencies (USD, EUR, GBP, JPY, CAD)
- ❌ Empty transaction IDs
- ❌ Future timestamps
- ❌ Multiple validation errors at once

**Example:**
```csharp
// Tests that negative amounts are rejected
[Fact]
public void ValidateTransaction_WithNegativeAmount_ShouldReturnFalse()
{
    var transaction = CreateValidTransaction();
    transaction.Amount = -50m;

    var result = TransactionValidator.ValidateTransaction(transaction);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain("Amount must be greater than 0");
}
```

### 2. MoneyTransferServiceTests (14 tests)

**Purpose:** Tests money transfer logic with mocked dependencies

**Key Concept:** Uses **Moq** to create fake repositories (no real database needed)

**Tests cover:**
- ✅ Successful transfers between valid cards
- ❌ Zero and negative amounts
- ❌ Source card not found
- ❌ Source card blocked or expired
- ❌ Insufficient funds
- ❌ Destination card not found or blocked
- ❌ Self-transfers (same card)
- ✅ Transaction ID generation
- ✅ Multiple sequential transfers
- ✅ Exception handling
- ✅ Custom currency support

**Example:**
```csharp
// Tests successful money transfer using mocked repositories
[Fact]
public async Task TransferMoneyAsync_WithValidCards_ShouldSucceed()
{
    // Create test cards
    var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
    var toCard = CreateTestCard("5555666677778888", balance: 500m);

    // Setup mocks to return our test cards
    _mockCardRepository
        .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
        .ReturnsAsync(fromCard);

    _mockCardRepository
        .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
        .ReturnsAsync(toCard);

    // Perform transfer
    var result = await _service.TransferMoneyAsync(
        "1111222233334444", 
        "5555666677778888", 
        100m
    );

    // Verify results
    result.Success.Should().BeTrue();
    result.FromAccountNewBalance.Should().Be(900m);
    result.ToAccountNewBalance.Should().Be(600m);

    // Verify both cards were updated
    _mockCardRepository.Verify(
        x => x.UpdateCardBalanceAsync(It.IsAny<DummyCreditCard>()), 
        Times.Exactly(2)
    );
}
```

### 3. TransactionProcessorTests (14 tests)

**Purpose:** Tests transaction processing with real test card data

**Key Concept:** Uses DummyCreditCardService test cards (integration-style tests)

**Tests cover:**
- ✅ Valid card transactions
- ❌ Invalid (non-existent) cards
- ❌ Insufficient funds
- ❌ Blocked cards
- ❌ Expired cards
- ✅ Large amount transactions
- ✅ Small amount transactions
- ✅ Exact balance transactions
- ❌ Transactions just over balance
- ✅ Different card types (Visa, Mastercard, AmEx)
- ✅ Multiple sequential transactions
- ✅ Processing time simulation
- ✅ Cardholder name in results
- ✅ Different currencies

**Example:**
```csharp
// Tests transaction with insufficient funds
[Fact]
public async Task ProcessTransaction_WithInsufficientFunds_ShouldFail()
{
    var transaction = CreateTransaction(
        cardNumber: "4000000000000010", // Card with only $25
        amount: 100m // Trying to charge $100
    );

    var result = await TransactionProcessor.ProcessTransaction(transaction);

    result.IsSuccessful.Should().BeFalse();
    result.Status.Should().Be("INSUFFICIENT_FUNDS");
    result.Message.Should().Contain("Insufficient funds");
    result.RemainingBalance.Should().Be(25m);
}
```

## Key Testing Concepts Used

### 1. **AAA Pattern** (Arrange, Act, Assert)

Every test follows this structure:
```csharp
[Fact]
public void TestName()
{
    // ARRANGE - Set up test data
    var input = CreateTestData();

    // ACT - Execute the method being tested
    var result = MethodUnderTest(input);

    // ASSERT - Verify the result
    result.Should().Be(expectedValue);
}
```

### 2. **Mocking** (Moq Library)

Creates fake objects to isolate code under test:
```csharp
// Create a fake repository
var mockRepository = new Mock<ICreditCardRepository>();

// Configure what it returns
mockRepository
    .Setup(x => x.GetCardByNumberAsync("1234"))
    .ReturnsAsync(testCard);

// Verify methods were called
mockRepository.Verify(
    x => x.SaveAsync(It.IsAny<CreditCard>()), 
    Times.Once
);
```

### 3. **Fluent Assertions**

Makes tests readable:
```csharp
// Instead of: Assert.Equal(true, result.IsValid)
result.IsValid.Should().BeTrue();

// Instead of: Assert.Contains("error", result.Errors)
result.Errors.Should().Contain("error");

// Chaining:
result.Should().NotBeNull()
      .And.Should().BeOfType<TransferResult>()
      .Which.Success.Should().BeTrue();
```

### 4. **Theory Tests** (Data-Driven Tests)

Run same test with different inputs:
```csharp
[Theory]
[InlineData("USD")]
[InlineData("EUR")]
[InlineData("GBP")]
public void ValidCurrencies_ShouldPass(string currency)
{
    // Test runs 3 times with different currency values
}
```

## Running the Tests

### Command Line

```powershell
# Navigate to test directory
cd test/FintechProject.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~TransactionValidatorTests"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### Visual Studio

1. Open **Test Explorer** (Test → Test Explorer)
2. Click **Run All Tests**
3. View results and coverage

### VS Code

1. Install **C# Dev Kit** extension
2. Click test beaker icon in sidebar
3. Run individual or all tests

## Test Naming Convention

We use this pattern for clarity:
```
MethodName_Scenario_ExpectedResult
```

**Examples:**
- `TransferMoneyAsync_WithValidCards_ShouldSucceed`
- `ValidateTransaction_WithNegativeAmount_ShouldReturnFalse`
- `ProcessTransaction_WithExpiredCard_ShouldFail`

## Dependencies

```xml
<!-- Test Framework -->
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />

<!-- Mocking Library -->
<PackageReference Include="Moq" Version="4.20.72" />

<!-- Fluent Assertions -->
<PackageReference Include="FluentAssertions" Version="6.12.1" />

<!-- Test SDK -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
```

## Best Practices Demonstrated

### ✅ We Do:
1. **Test one thing at a time** - Each test has a single purpose
2. **Use descriptive names** - Test names explain what they verify
3. **Follow AAA pattern** - Arrange, Act, Assert structure
4. **Use mocks appropriately** - Isolate code under test
5. **Test edge cases** - Zero amounts, exact balance, etc.
6. **Test both success and failure** - Happy paths and error cases
7. **Keep tests independent** - Each test can run alone
8. **Use helper methods** - `CreateTestCard()`, `CreateValidTransaction()`

### ❌ We Avoid:
1. **Testing multiple things** - One assertion per concept
2. **Touching real databases** - Use mocks instead
3. **Dependent tests** - Tests don't rely on execution order
4. **Hard-coded dates** - Use `DateTime.UtcNow` relative to test time
5. **Random data** - Tests are deterministic and repeatable

## Common Test Patterns

### Testing Success Cases
```csharp
[Fact]
public async Task Method_WithValidInput_ShouldSucceed()
{
    // ARRANGE
    var validInput = CreateValidInput();

    // ACT
    var result = await Service.Method(validInput);

    // ASSERT
    result.Success.Should().BeTrue();
    result.Value.Should().Be(expectedValue);
}
```

### Testing Validation Failures
```csharp
[Fact]
public void Validator_WithInvalidInput_ShouldReturnErrors()
{
    // ARRANGE
    var invalidInput = CreateInvalidInput();

    // ACT
    var result = Validator.Validate(invalidInput);

    // ASSERT
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain("Expected error message");
}
```

### Testing with Mocks
```csharp
[Fact]
public async Task Service_WhenDependencyFails_ShouldHandleGracefully()
{
    // ARRANGE
    _mockDependency
        .Setup(x => x.Method())
        .ThrowsAsync(new Exception("Simulated failure"));

    // ACT
    var result = await _service.Method();

    // ASSERT
    result.Success.Should().BeFalse();
    result.Message.Should().Contain("error");
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"

- name: Check Coverage
  run: dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80
```

## Troubleshooting

### Issue: Tests fail with "Object reference not set"
**Solution:** Ensure all mocks are properly configured with `.Setup()` before use

### Issue: "Method was expected to be called once, but was called 0 times"
**Solution:** Check that your test actually triggers the mocked method

### Issue: Async test hangs
**Solution:** Always use `await` with async methods, never `.Result`

### Issue: Random test failures
**Solution:** Check for dependencies on current time, random data, or external state

## Further Resources

- **xUnit Documentation**: https://xunit.net/
- **Moq Quickstart**: https://github.com/moq/moq4/wiki/Quickstart
- **FluentAssertions**: https://fluentassertions.com/
- **Test-Driven Development**: https://en.wikipedia.org/wiki/Test-driven_development

## Next Steps

1. **Increase Coverage**: Add tests for other services/repositories
2. **Integration Tests**: Test with real database (separate project)
3. **Performance Tests**: Measure transaction processing speed
4. **Load Tests**: Test system under high concurrent load
5. **E2E Tests**: Test entire workflows end-to-end

## Summary

We've created a comprehensive unit test suite with:
- ✅ **51 passing tests**
- ✅ **3 test classes** covering key business logic
- ✅ **Both unit and integration-style tests**
- ✅ **Proper mocking** for isolated testing
- ✅ **Clear documentation** with extensive comments
- ✅ **Best practices** throughout

**These tests ensure code quality, catch bugs early, and serve as living documentation!** 🚀
