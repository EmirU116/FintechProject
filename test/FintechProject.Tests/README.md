# 📚 Unit Testing Guide for Beginners

## 🎯 What is Unit Testing?

**Unit testing** is like having a safety net for your code. Imagine you're building a car - you wouldn't wait until it's fully assembled to check if the brakes work, right? You'd test each part individually. That's unit testing!

### Key Concepts

1. **Unit** = A small piece of code (usually one function or method)
2. **Test** = Code that checks if your unit works correctly
3. **Assert** = Verifying that something is true
4. **Mock** = A "fake" version of something (like a fake database)

---

## 🏗️ Project Structure

```
FintechProject/
├── src/
│   └── Core/                    # Your main code
│       ├── TransactionValidator.cs
│       ├── MoneyTransferService.cs
│       └── TransactionProcessor.cs
└── test/
    └── FintechProject.Tests/    # Your test code
        ├── TransactionValidatorTests.cs
        ├── MoneyTransferServiceTests.cs
        └── TransactionProcessorTests.cs
```

---

## 🔧 Test Framework & Tools

We use these tools for testing:

### 1. **xUnit** - The Testing Framework
   - Runs our tests
   - Provides `[Fact]` and `[Theory]` attributes
   - Shows pass/fail results

### 2. **FluentAssertions** - Readable Assertions
   - Makes tests easier to read
   - Instead of: `Assert.Equal(expected, actual)`
   - We write: `actual.Should().Be(expected)`

### 3. **Moq** - Mocking Library
   - Creates "fake" objects for testing
   - Lets us control what dependencies return
   - No need for a real database!

---

## 📖 Anatomy of a Test

Every test follows the **AAA Pattern**:

```csharp
[Fact]  // This attribute marks a test method
public void DescriptiveTestName()
{
    // ARRANGE - Set up test data
    var transaction = new Transaction { Amount = 100m };

    // ACT - Run the code we're testing
    var result = TransactionValidator.ValidateTransaction(transaction);

    // ASSERT - Check if result is correct
    result.IsValid.Should().BeTrue();
}
```

### Breaking it Down:

1. **`[Fact]`** - Tells xUnit "this is a test"
2. **Descriptive Name** - Explains what the test does
3. **ARRANGE** - Prepare your test data
4. **ACT** - Execute the method you're testing
5. **ASSERT** - Verify the result is correct

---

## 🎭 Understanding Mocking

### Why Mock?

Imagine testing a money transfer function:
- ❌ **Without Mocks**: Need a real database, real cards, real data
- ✅ **With Mocks**: Create fake repositories that do exactly what we need

### Example: Mocking a Repository

```csharp
// Create a fake repository
var mockRepository = new Mock<ICreditCardRepository>();

// Tell it what to return when asked for a card
mockRepository
    .Setup(x => x.GetCardByNumberAsync("1234567890123456"))
    .ReturnsAsync(testCard);

// Use the fake in our service
var service = new MoneyTransferService(mockRepository.Object);
```

**What happens:**
1. Service asks for card "1234567890123456"
2. Mock returns our test card
3. No database needed! 🎉

---

## 📝 Test Types in This Project

### 1. TransactionValidatorTests.cs
**What it tests:** Input validation rules

**Tests include:**
- ✅ Valid transactions pass
- ❌ Invalid card numbers fail
- ❌ Negative amounts fail
- ❌ Invalid currencies fail
- ❌ Future timestamps fail

**Example test:**
```csharp
[Fact]
public void ValidateTransaction_WithNegativeAmount_ShouldReturnFalse()
{
    // ARRANGE
    var transaction = CreateValidTransaction();
    transaction.Amount = -50m; // Negative!

    // ACT
    var result = TransactionValidator.ValidateTransaction(transaction);

    // ASSERT
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain("Amount must be greater than 0");
}
```

### 2. MoneyTransferServiceTests.cs
**What it tests:** Money transfer logic with mocked repositories

**Tests include:**
- ✅ Successful transfers
- ❌ Insufficient funds
- ❌ Blocked cards
- ❌ Expired cards
- ❌ Invalid cards
- ❌ Self-transfers

**Key concept:** Uses **Moq** to create fake repositories

**Example test:**
```csharp
[Fact]
public async Task TransferMoneyAsync_WithValidCards_ShouldSucceed()
{
    // ARRANGE - Create test cards
    var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
    var toCard = CreateTestCard("5555666677778888", balance: 500m);

    // Setup mocks to return our test cards
    _mockCardRepository
        .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
        .ReturnsAsync(fromCard);

    _mockCardRepository
        .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
        .ReturnsAsync(toCard);

    // ACT - Perform transfer
    var result = await _service.TransferMoneyAsync(
        "1111222233334444", 
        "5555666677778888", 
        100m
    );

    // ASSERT
    result.Success.Should().BeTrue();
    result.FromAccountNewBalance.Should().Be(900m); // 1000 - 100
    result.ToAccountNewBalance.Should().Be(600m);   // 500 + 100

    // VERIFY - Check repository methods were called
    _mockCardRepository.Verify(
        x => x.UpdateCardBalanceAsync(It.IsAny<DummyCreditCard>()), 
        Times.Exactly(2) // Both cards updated
    );
}
```

### 3. TransactionProcessorTests.cs
**What it tests:** Transaction processing with test cards

**Tests include:**
- ✅ Successful payments
- ❌ Invalid cards
- ❌ Blocked cards
- ❌ Insufficient funds
- ✅ Multiple card types (Visa, Mastercard, AmEx)

**Key concept:** Uses **real test data** from DummyCreditCardService

---

## 🚀 Running Tests

### Option 1: Command Line
```powershell
# Navigate to test project
cd test/FintechProject.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~TransactionValidatorTests"
```

### Option 2: Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All Tests"
3. View results in real-time

### Option 3: VS Code
1. Install C# Dev Kit extension
2. Click the test beaker icon in sidebar
3. Run individual or all tests

---

## 📊 Understanding Test Results

### ✅ Passing Test
```
✓ TransactionValidatorTests.ValidateTransaction_WithValidData_ShouldReturnTrue
```
**Meaning:** Test passed! Code works as expected.

### ❌ Failing Test
```
✗ TransactionValidatorTests.ValidateTransaction_WithValidData_ShouldReturnTrue
  Expected result.IsValid to be true, but found false.
```
**Meaning:** Something's wrong! The code didn't behave as expected.

---

## 🎓 Test Naming Convention

We use this pattern:
```
MethodName_Scenario_ExpectedResult
```

**Examples:**
- `TransferMoneyAsync_WithValidCards_ShouldSucceed`
- `ValidateTransaction_WithNegativeAmount_ShouldReturnFalse`
- `ProcessTransaction_WithExpiredCard_ShouldFail`

**Why?** Makes it crystal clear what each test does!

---

## 🔍 Advanced Concepts

### Theory vs Fact

**`[Fact]`** - Single test case
```csharp
[Fact]
public void MyTest() { }
```

**`[Theory]`** - Multiple test cases with different data
```csharp
[Theory]
[InlineData("USD")]
[InlineData("EUR")]
[InlineData("GBP")]
public void ValidCurrencies_ShouldPass(string currency) { }
```

### Verify vs Assert

**Assert** - Check the return value
```csharp
result.Success.Should().BeTrue();
```

**Verify** - Check that a method was called
```csharp
_mockRepository.Verify(
    x => x.SaveAsync(It.IsAny<Transaction>()), 
    Times.Once
);
```

---

## 💡 Best Practices

### ✅ DO:
- Write tests BEFORE or RIGHT AFTER writing code
- Test one thing at a time
- Use descriptive test names
- Keep tests simple and readable
- Test both success and failure cases

### ❌ DON'T:
- Test multiple things in one test
- Use real databases in unit tests
- Make tests dependent on each other
- Skip testing edge cases
- Write tests that can randomly fail

---

## 🐛 Common Issues & Solutions

### Issue: Test won't run
**Solution:** Make sure method has `[Fact]` attribute and is public

### Issue: "Object reference not set to an instance"
**Solution:** Check that you've set up your mocks properly

### Issue: Test passes locally but fails in CI
**Solution:** Avoid dependencies on local time, file system, etc.

### Issue: Test is flaky (sometimes passes, sometimes fails)
**Solution:** Remove randomness, use fixed dates/values

---

## 📚 Further Learning

### Key Terms to Google:
- Unit Testing Best Practices
- Test-Driven Development (TDD)
- Mocking vs Stubbing
- Integration Testing
- Test Coverage

### Recommended Resources:
- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Docs](https://fluentassertions.com/)

---

## 🎉 Summary

**Unit Testing = Safety Net for Your Code**

1. **Write small tests** for individual functions
2. **Use mocks** to avoid real databases/APIs
3. **Follow AAA pattern**: Arrange, Act, Assert
4. **Run tests often** to catch bugs early
5. **Tests = Documentation** - they show how code should work!

---

## 📞 Quick Reference Card

```csharp
// Basic Test Structure
[Fact]
public void TestName()
{
    // ARRANGE
    var input = /* test data */;
    
    // ACT
    var result = MethodUnderTest(input);
    
    // ASSERT
    result.Should().Be(expected);
}

// Mocking Setup
var mock = new Mock<IRepository>();
mock.Setup(x => x.GetAsync(id))
    .ReturnsAsync(testData);

// Verify Method Call
mock.Verify(x => x.SaveAsync(It.IsAny<Data>()), Times.Once);

// Common Assertions
result.Should().BeTrue();
result.Should().Be(expectedValue);
result.Should().BeNull();
result.Should().Contain("text");
list.Should().HaveCount(5);
```

**Happy Testing! 🚀**
