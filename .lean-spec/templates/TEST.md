# Test: {name}

> Part of [{name}](README.md)

## Testing Strategy

This spec follows **Test-Driven Development (TDD)** with atomic, scenario-based test files. Each test scenario from [DESIGN.md](DESIGN.md) becomes a discrete test method.

### Test Pyramid

```
    /\
   /  \  UI/E2E Tests (Manual)
  /____\
 /      \ Integration Tests
/________\ Unit Tests (TDD Focus)
```

**Primary Focus**: Unit tests for ViewModels and Services using xUnit + Moq + FluentAssertions

---

## Unit Tests (Red-Green-Refactor)

### Test File Organization

Create one test file per production class:

- `{ViewModelName}Tests.cs` - ViewModel behavior tests
- `{ServiceName}Tests.cs` - Service logic tests
- `{ExtensionName}Tests.cs` - Extension method tests

### Test Naming Convention

Use LunaDraw standard: `Should_{Expected}_{When}_{Condition}`

**Examples**:
- `Should_Set_Error_Message_When_Submit_With_Empty_Field`
- `Should_Load_Items_When_Service_Returns_Data`
- `Should_Navigate_To_Detail_When_Item_Selected`

### Scenario-Based Test Structure

For EACH scenario in [DESIGN.md](DESIGN.md#plain-english-test-scenarios), create a corresponding test method:

#### Scenario: User submits empty form

```csharp
// TODO: Implement test for "User submits empty form" scenario
// Given: The form is displayed
// When: User clicks "Submit" with an empty required field
// Then: An error message appears and form is not submitted

[Fact]
public void Should_Set_Error_Message_When_Submit_With_Empty_Field()
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var viewModel = new ExampleViewModel(mockService.Object);
    viewModel.SearchText = string.Empty; // Empty field

    // Act
    viewModel.SubmitCommand.Execute().Subscribe();

    // Assert
    viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    viewModel.IsSubmitting.Should().BeFalse();
    mockService.Verify(s => s.SubmitAsync(It.IsAny<string>()), Times.Never);
}
```

#### Scenario: Successful data load

```csharp
// TODO: Implement test for "Successful data load" scenario
// Given: The service returns 5 items
// When: The ViewModel initializes
// Then: The Items collection contains 5 elements

[Fact]
public async Task Should_Load_Items_When_Service_Returns_Data()
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var testItems = new List<Item>
    {
        new Item { Id = 1, Name = "Item 1" },
        new Item { Id = 2, Name = "Item 2" },
        new Item { Id = 3, Name = "Item 3" },
        new Item { Id = 4, Name = "Item 4" },
        new Item { Id = 5, Name = "Item 5" }
    };
    mockService.Setup(s => s.GetItemsAsync(0, 10))
        .ReturnsAsync(testItems);

    var viewModel = new ExampleViewModel(mockService.Object);

    // Act
    await viewModel.LoadCommand.Execute();

    // Assert
    viewModel.Items.Should().HaveCount(5);
    viewModel.Items.Should().BeEquivalentTo(testItems);
    viewModel.IsLoading.Should().BeFalse();
}
```

#### Scenario: Navigation on item selection

```csharp
// TODO: Implement test for "Navigation on item selection" scenario
// Given: A list of items is displayed
// When: User taps an item
// Then: Navigation occurs to the detail page with the selected item

[Fact]
public void Should_Navigate_To_Detail_When_Item_Selected()
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var mockNavigationService = new Mock<INavigationService>();
    var viewModel = new ExampleViewModel(mockService.Object, mockNavigationService.Object);
    var selectedItem = new Item { Id = 1, Name = "Test Item" };

    // Act
    viewModel.SelectItemCommand.Execute(selectedItem).Subscribe();

    // Assert
    mockNavigationService.Verify(
        n => n.NavigateToAsync("DetailPage", It.Is<Item>(i => i.Id == 1)),
        Times.Once
    );
}
```

### Test Coverage Requirements

**Minimum Coverage**: 80% for all ViewModels and Services

**Required Test Types**:
- ✅ Happy path (successful execution)
- ✅ Edge cases (null, empty, boundary values)
- ✅ Error handling (exceptions, network failures)
- ✅ State transitions (loading → loaded, idle → busy)

---

## Integration Tests

### Scope

Test the full flow: ViewModel → Service → Repository → Data Layer (mocked)

### Key Test Cases

#### Integration Test 1: End-to-End Data Flow

```csharp
[Fact]
public async Task Should_Load_And_Display_Items_End_To_End()
{
    // Arrange: Set up full dependency chain
    var mockRepository = new Mock<IItemRepository>();
    mockRepository.Setup(r => r.GetAllAsync())
        .ReturnsAsync(new List<Item> { new Item { Id = 1 } });

    var service = new ExampleService(mockRepository.Object);
    var viewModel = new ExampleViewModel(service);

    // Act: Execute ViewModel command
    await viewModel.LoadCommand.Execute();

    // Assert: Verify data flows through all layers
    viewModel.Items.Should().HaveCount(1);
    mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
}
```

#### Integration Test 2: Dependency Injection Resolution

```csharp
[Fact]
public void Should_Resolve_All_Dependencies_From_DI_Container()
{
    // Arrange: Build DI container (as in MauiProgram.cs)
    var services = new ServiceCollection();
    services.AddSingleton<IExampleService, ExampleService>();
    services.AddTransient<ExampleViewModel>();
    var serviceProvider = services.BuildServiceProvider();

    // Act: Resolve ViewModel
    var viewModel = serviceProvider.GetRequiredService<ExampleViewModel>();

    // Assert: ViewModel is not null and dependencies are injected
    viewModel.Should().NotBeNull();
}
```

---

## Performance Tests

### Requirements

- ViewModel initialization: < 100ms
- Data loading (100 items): < 500ms
- UI responsiveness: No blocking on main thread

### Test Scenarios

#### Performance Test: Load Large Dataset

```csharp
[Fact]
public async Task Should_Load_1000_Items_Within_1_Second()
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var largeDataset = Enumerable.Range(1, 1000)
        .Select(i => new Item { Id = i, Name = $"Item {i}" })
        .ToList();
    mockService.Setup(s => s.GetItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
        .ReturnsAsync(largeDataset);

    var viewModel = new ExampleViewModel(mockService.Object);

    // Act
    var stopwatch = Stopwatch.StartNew();
    await viewModel.LoadCommand.Execute();
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    viewModel.Items.Should().HaveCount(1000);
}
```

---

## Security Tests

### Security Checks

- [ ] Input validation: Reject SQL injection patterns
- [ ] Authentication: Verify unauthenticated users cannot access protected features
- [ ] Authorization: Verify role-based access control
- [ ] Data sanitization: User input is sanitized before storage

### Example Security Test

```csharp
[Theory]
[InlineData("<script>alert('XSS')</script>")]
[InlineData("'; DROP TABLE Users; --")]
[InlineData("../../etc/passwd")]
public void Should_Reject_Malicious_Input_When_Validating_Search_Text(string maliciousInput)
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var viewModel = new ExampleViewModel(mockService.Object);

    // Act
    viewModel.SearchText = maliciousInput;
    viewModel.SubmitCommand.Execute().Subscribe();

    // Assert
    viewModel.ErrorMessage.Should().Contain("Invalid input");
    mockService.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
}
```

---

## Acceptance Criteria

<!-- What must be true for this to be considered complete? -->

- [ ] All unit tests pass (100% success rate)
- [ ] Code coverage ≥ 80% for ViewModels and Services
- [ ] Integration tests verify end-to-end flows
- [ ] Performance benchmarks met
- [ ] Security tests validate input sanitization
- [ ] Manual testing confirms UI behavior

---

## Test Data

### Mock Data Setup

Create a `TestDataBuilder.cs` class for consistent test data:

```csharp
public static class TestDataBuilder
{
    public static List<Item> CreateItems(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Item { Id = i, Name = $"Item {i}" })
            .ToList();
    }

    public static ExampleViewModel CreateViewModelWithMockedService(List<Item> items)
    {
        var mockService = new Mock<IExampleService>();
        mockService.Setup(s => s.GetItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(items);
        return new ExampleViewModel(mockService.Object);
    }
}
```

---

## Manual Testing

### UI/UX Validation

- [ ] **Visual Regression**: Compare screenshots before/after changes
- [ ] **Touch Targets**: Verify buttons are ≥ 2cm x 2cm (child-friendly)
- [ ] **Animations**: Confirm smooth transitions (60fps)
- [ ] **Error States**: Verify error messages display correctly
- [ ] **Accessibility**: Test with screen reader (Windows Narrator)

### Platform-Specific Testing

- [ ] **Windows**: Test on Windows 11 with touch and mouse input
- [ ] **Android**: Test on emulator and physical device (API 36+)
- [ ] **iOS**: Test on simulator and physical device (iOS 17+)
- [ ] **MacCatalyst**: Verify app runs on macOS

### Test Execution Checklist

1. Run unit tests: `dotnet test`
2. Check code coverage: `dotnet test /p:CollectCoverage=true`
3. Run integration tests: `dotnet test --filter Category=Integration`
4. Build release: `dotnet build -c Release`
5. Deploy to test device and perform manual testing
