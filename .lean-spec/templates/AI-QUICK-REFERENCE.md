# AI Agent Quick Reference Card

> **Purpose**: Fast reference for AI agents executing TDD workflow
> **Print/Save**: Keep this open during implementation phases

---

## 🎯 Your Mission

Execute a **Test-Driven Development (TDD)** workflow using **Red-Green-Refactor (RGR)** methodology for .NET MAUI development.

---

## 📖 Read These First (In Order)

1. ✅ **README.md** - Context, environment, AI directive
2. ✅ **DESIGN.md** - Architecture, signatures, test scenarios
3. ✅ **PLAN.md** - Phase-by-phase execution plan
4. ✅ **TEST.md** - Test templates and examples

---

## 🚀 Phase Overview

| Phase | Goal | Success Criteria | New Chat? |
|-------|------|------------------|-----------|
| **1. Scaffold** | Create empty classes | ✅ `dotnet build` succeeds | YES |
| **2. Test Definition** | Write failing tests | ✅ `dotnet test` shows RED | YES |
| **3. TDD Loop** | Implement one test at a time | ✅ `dotnet test` shows GREEN | YES |
| **4. Integration** | Wire up DI, refactor SOLID | ✅ Production-ready code | YES |

---

## Phase 1: The Scaffold

### Your Role
`Act as a C# scaffolding expert.`

### Your Task
1. Read **DESIGN.md** → Section: "Interface & Method Signatures"
2. Create ALL interfaces listed
3. Create ALL classes listed (constructor signatures only)
4. All methods: `throw new NotImplementedException();`
5. Add `[Obsolete("Work in Progress")]` to all classes
6. Run: `dotnet build`

### You Are Done When
- [ ] Project compiles with ZERO errors
- [ ] All types from DESIGN.md exist in codebase
- [ ] All methods throw `NotImplementedException`

### Example Output

```csharp
// ISearchService.cs
public interface ISearchService
{
    Task<IEnumerable<Item>> SearchAsync(string query);
}

// SearchService.cs
[Obsolete("Work in Progress")]
public class SearchService : ISearchService
{
    public Task<IEnumerable<Item>> SearchAsync(string query)
    {
        throw new NotImplementedException();
    }
}
```

### Do NOT
- ❌ Implement any business logic
- ❌ Write tests yet
- ❌ Add features not in DESIGN.md

---

## Phase 2: Narrow Test Definition

### Your Role
`Act as a TDD expert.`

### Your Task
1. Read **DESIGN.md** → Section: "Plain English Test Scenarios"
2. For EACH scenario, create a test method
3. Name tests: `Should_{Expected}_{When}_{Condition}`
4. Write assertions FIRST (e.g., `result.Should().NotBeNull();`)
5. Mock all dependencies with Moq
6. Do NOT implement production code to make tests pass
7. Run: `dotnet test`

### You Are Done When
- [ ] All test files created
- [ ] All tests FAIL (RED state)
- [ ] No implementation code written

### Example Output

```csharp
// SearchViewModelTests.cs
public class SearchViewModelTests
{
    [Fact]
    public async Task Should_Populate_Results_When_Search_Returns_Items()
    {
        // Arrange
        var mockService = new Mock<ISearchService>();
        var testItems = new List<Item>
        {
            new Item { Id = 1, Name = "Test 1" },
            new Item { Id = 2, Name = "Test 2" }
        };
        mockService.Setup(s => s.SearchAsync("test"))
            .ReturnsAsync(testItems);

        var viewModel = new SearchViewModel(mockService.Object);

        // Act
        await viewModel.SearchCommand.Execute("test");

        // Assert
        viewModel.Results.Should().HaveCount(2);
        viewModel.Results.Should().BeEquivalentTo(testItems);
    }
}
```

### Do NOT
- ❌ Implement production code to make tests pass
- ❌ Skip writing tests for any scenario
- ❌ Add test scenarios not in DESIGN.md

---

## Phase 3: The TDD Loop

### Your Role
`Act as a TDD practitioner.`

### Your Task (REPEAT for EACH test)
1. **RED**: Run `dotnet test` and identify ONE failing test
2. **GREEN**: Write MINIMAL code to pass ONLY that test
3. **VERIFY**: Run `dotnet test --filter "FullyQualifiedName~TestMethodName"`
4. **REFACTOR**: Clean up code (extract methods, rename variables)
5. **ASK**: Wait for confirmation before proceeding to next test

### You Are Done When
- [ ] ALL tests pass (`dotnet test` shows 100% success)
- [ ] No `NotImplementedException` remaining
- [ ] Code is clean (SOLID principles)

### Example Process

**User**: "Implement the first failing test: Should_Populate_Results_When_Search_Returns_Items"

**You**:

```csharp
// SearchViewModel.cs
public class SearchViewModel : ReactiveObject
{
    private readonly ISearchService searchService;

    [Reactive] public ObservableCollection<Item> Results { get; set; }

    public ReactiveCommand<string, IEnumerable<Item>> SearchCommand { get; }

    public SearchViewModel(ISearchService searchService)
    {
        this.searchService = searchService;
        Results = new ObservableCollection<Item>();

        SearchCommand = ReactiveCommand.CreateFromTask<string, IEnumerable<Item>>(
            async query =>
            {
                var items = await searchService.SearchAsync(query);
                Results.Clear();
                foreach (var item in items)
                {
                    Results.Add(item);
                }
                return items;
            });
    }
}
```

**Verify**: Run `dotnet test --filter "Should_Populate_Results_When_Search_Returns_Items"`

**Result**: ✅ Test passes

**You**: "Test passes. Ready to proceed to the next test? (Should_Clear_Results_When_Search_Text_Is_Empty)"

### Do NOT
- ❌ Implement multiple tests at once
- ❌ Add features not covered by tests
- ❌ Skip refactoring step

---

## Phase 4: Integration & Refactoring

### Your Role
`Act as a C# architect.`

### Your Task
1. Review code for SOLID violations
2. Register all services in `MauiProgram.cs` DI container
3. Add integration tests (ViewModel → Service → Data)
4. Remove `[Obsolete]` attributes
5. Add XML documentation comments to public APIs
6. Run: `dotnet test` (all tests must pass)

### You Are Done When
- [ ] All services registered in DI
- [ ] Integration tests pass
- [ ] No SOLID violations
- [ ] Code is production-ready

### Example Output

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Register services
        builder.Services.AddSingleton<ISearchService, SearchService>();
        builder.Services.AddTransient<SearchViewModel>();

        // ...

        return builder.Build();
    }
}

// Integration test
[Fact]
public async Task Should_Load_And_Display_Search_Results_End_To_End()
{
    // Arrange: Full dependency chain
    var mockRepository = new Mock<IItemRepository>();
    mockRepository.Setup(r => r.SearchAsync("test"))
        .ReturnsAsync(new List<Item> { new Item { Id = 1 } });

    var service = new SearchService(mockRepository.Object);
    var viewModel = new SearchViewModel(service);

    // Act
    await viewModel.SearchCommand.Execute("test");

    // Assert
    viewModel.Results.Should().HaveCount(1);
}
```

### Do NOT
- ❌ Add new features
- ❌ Skip DI registration
- ❌ Leave `[Obsolete]` attributes

---

## 🛑 Critical Rules

### ALWAYS
- ✅ Read DESIGN.md BEFORE writing code
- ✅ Follow ONLY the current phase in PLAN.md
- ✅ Use NEW chat window for EACH phase
- ✅ Write tests BEFORE implementation (Phase 2 before Phase 3)
- ✅ Implement ONE test at a time (Phase 3)
- ✅ Use compiler errors to self-correct type mismatches
- ✅ Mock all dependencies with Moq
- ✅ Name tests: `Should_{Expected}_{When}_{Condition}`

### NEVER
- ❌ Perform "Big Bang" implementations
- ❌ Add features not defined in DESIGN.md (check "Non-Goals")
- ❌ Skip tests or implement multiple features at once
- ❌ Write implementation code in Phase 2 (Test Definition)
- ❌ Guess types or method signatures (they're in DESIGN.md)
- ❌ Use underscores, regions, or abbreviations (LunaDraw standards)

---

## 🧪 Test Template (Copy-Paste)

```csharp
// TODO: Implement test for "[Scenario name from DESIGN.md]"
// Given: [Precondition]
// When: [Action]
// Then: [Expected result]

[Fact]
public void Should_{Expected}_{When}_{Condition}()
{
    // Arrange
    var mockService = new Mock<IServiceName>();
    // Setup mock behavior
    mockService.Setup(s => s.MethodName(It.IsAny<Type>()))
        .ReturnsAsync(expectedResult);

    var viewModel = new ExampleViewModel(mockService.Object);

    // Act
    var result = viewModel.Command.Execute(input).Subscribe();

    // Assert
    result.Should().NotBeNull();
    result.PropertyName.Should().Be(expectedValue);
    mockService.Verify(s => s.MethodName(It.IsAny<Type>()), Times.Once);
}
```

---

## 🔧 Common Commands

```bash
# Build project
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Check code coverage
dotnet test /p:CollectCoverage=true
```

---

## 🔍 Self-Check Before Proceeding

### Phase 1 → Phase 2
- [ ] Does `dotnet build` succeed?
- [ ] Do all classes have `[Obsolete("Work in Progress")]`?
- [ ] Do all methods throw `NotImplementedException`?

### Phase 2 → Phase 3
- [ ] Does `dotnet test` show RED (all tests fail)?
- [ ] Is there a test for EVERY scenario in DESIGN.md?
- [ ] Did I avoid writing any implementation code?

### Phase 3 → Phase 4
- [ ] Does `dotnet test` show GREEN (all tests pass)?
- [ ] Is there no `NotImplementedException` remaining?
- [ ] Did I implement tests one at a time?

### Phase 4 → Complete
- [ ] Are all services registered in `MauiProgram.cs`?
- [ ] Do integration tests pass?
- [ ] Are `[Obsolete]` attributes removed?

---

## 🚨 Red Flags (Stop and Ask User)

If you encounter ANY of these, STOP and ask the user:

1. **Missing interface signature in DESIGN.md**
   - "I don't see the signature for `IExampleService` in DESIGN.md. Should I create it?"

2. **Unclear test scenario**
   - "The scenario 'User submits form' is ambiguous. What should happen on submit?"

3. **Feature not in DESIGN.md**
   - "Pagination is not mentioned in DESIGN.md Goals. Should I add it?"

4. **Compilation error after following instructions**
   - "The build fails with error X. The signature in DESIGN.md may need updating."

5. **Test passes immediately (should be RED first)**
   - "Test passes without implementation. The test may not be testing the right thing."

---

## 📊 Progress Tracking

Copy this checklist into your responses to track progress:

```markdown
### Phase Progress

**Phase 1: Scaffold**
- [ ] Interfaces created
- [ ] Classes created
- [ ] Project compiles

**Phase 2: Test Definition**
- [ ] Test files created
- [ ] All scenarios have tests
- [ ] All tests FAIL (RED)

**Phase 3: TDD Loop**
- [ ] Test 1: [Name] ✅
- [ ] Test 2: [Name] ⏳
- [ ] Test 3: [Name] ⏳

**Phase 4: Integration**
- [ ] Services registered in DI
- [ ] Integration tests added
- [ ] SOLID refactoring complete
```

---

## 💡 Pro Tips

1. **Use compiler feedback**: If you get a type error, check DESIGN.md for the correct signature
2. **One assertion per line**: Makes test failures easier to diagnose
3. **Prefer `[Theory]` over multiple `[Fact]`**: Test multiple inputs with `[InlineData]`
4. **Mock interfaces, not classes**: Use `Mock<IService>`, not `Mock<Service>`
5. **Verify mock calls**: Use `mockService.Verify(s => s.Method(...), Times.Once)`

---

## 🎓 Example Chat Flow

**User**: "Start Phase 1 for the search feature spec"

**You**:
```
Acting as a C# scaffolding expert for Phase 1.

I've read DESIGN.md and identified the following to scaffold:
- ISearchService interface
- SearchService class
- SearchViewModel class

Creating empty classes now...

[Create files with NotImplementedException]

✅ Phase 1 Complete
- Project compiles
- All methods throw NotImplementedException
- All classes marked [Obsolete("Work in Progress")]

Run `dotnet build` to verify.

Ready to proceed to Phase 2 (Test Definition)?
```

**User**: "Yes, proceed to Phase 2 in a NEW chat"

**You**:
```
Please open a NEW chat window and paste the following:

---

Act as a TDD expert working on the LunaDraw search feature.

Current Phase: Phase 2 (Narrow Test Definition)
Previous Phase Chat: [Link to this chat]

Read the spec at: specs/NNN-search-feature/DESIGN.md
Focus on section: "Plain English Test Scenarios"

Create xUnit tests for each scenario using naming: Should_{Expected}_{When}_{Condition}
Use Moq to mock ISearchService
Do NOT implement production code to make tests pass

Your goal: RED state (all tests fail)
```

---

**That's it! You're ready to execute TDD workflows.** 🚀

For detailed explanations, refer to:
- [TDD-WORKFLOW-GUIDE.md](TDD-WORKFLOW-GUIDE.md) - Comprehensive guide
- [DESIGN.md](DESIGN.md) - Architecture blueprint
- [PLAN.md](PLAN.md) - Phase-by-phase plan
- [TEST.md](TEST.md) - Test templates
