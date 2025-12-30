# TDD Workflow Guide for AI-Driven Development

> **Purpose**: This guide explains how to use the lean-spec templates for Test-Driven Development (TDD) with AI agents following Red-Green-Refactor (RGR) methodology.

---

## Table of Contents

1. [Overview](#overview)
2. [The TDD Philosophy](#the-tdd-philosophy)
3. [Document Structure](#document-structure)
4. [Workflow: Four Phases](#workflow-four-phases)
5. [Chat Session Management](#chat-session-management)
6. [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
7. [Example Walkthrough](#example-walkthrough)

---

## Overview

Traditional spec documents are written for human developers who can "read between the lines" and make reasonable assumptions. AI agents, however, require **explicit instructions** to avoid "vibe coding" (hallucinating features or guessing implementations).

These templates refactor the spec-driven development workflow into a **blueprint-first approach**:

- **DESIGN.md**: The architectural blueprint (what to build, not how)
- **PLAN.md**: The assembly sequence (phase-by-phase execution)
- **TEST.md**: The inspection criteria (test templates and acceptance)
- **README.md**: The route map (entry point for AI agents)

### Analogy: Building a Skyscraper

| Traditional Spec | TDD Spec (Refactored) |
|------------------|------------------------|
| "Build a house" (vague) | Structural blueprints (DESIGN.md) |
| Developer figures it out | Assembly sequence (PLAN.md) |
| Tests written after | Inspection criteria (TEST.md) |
| Unclear starting point | Route map (README.md) |

---

## The TDD Philosophy

### Red-Green-Refactor (RGR) Cycle

```
┌─────────────┐
│     RED     │  Write a failing test
│  (Test First)│
└──────┬──────┘
       │
       ▼
┌─────────────┐
│    GREEN    │  Write minimal code to pass
│ (Make it Work)│
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   REFACTOR  │  Clean up code (SOLID, DRY)
│ (Make it Right)│
└──────┬──────┘
       │
       └──────► Repeat for next test
```

### Why TDD for AI Agents?

1. **Prevents "Big Bang" implementations**: AI agents tend to write large chunks of code at once, leading to brittle, untestable code.
2. **Compiler-driven self-correction**: C# is strongly typed. Compiler errors guide the AI to fix type mismatches.
3. **Atomic progress**: Each test is a measurable milestone (RED → GREEN).
4. **Prevents hallucinated features**: Tests define the ONLY features to implement.

---

## Document Structure

### DESIGN.md (The Blueprint)

**Purpose**: Define the architecture, interfaces, and test scenarios WITHOUT implementation details.

**Key Sections**:

1. **Goals & Non-Goals**: Explicit boundaries (prevents scope creep)
2. **Visual Architecture**: Mermaid diagrams for component hierarchy
3. **Interface & Method Signatures**: Define ALL public APIs upfront
4. **Plain English Test Scenarios**: User stories that become tests
5. **Technical Approach**: Frameworks, patterns, libraries
6. **Design Decisions**: Rationale for key choices (with trade-offs)

**Example**:

```markdown
## Goals & Non-Goals

### Goals
- ✅ Implement search functionality with debounce (500ms)
- ✅ Paginate results (20 items per page)

### Non-Goals
- ❌ Infinite scroll (use pagination only)
- ❌ Advanced filtering (only basic search)
```

---

### PLAN.md (The Assembly Sequence)

**Purpose**: Break implementation into 4 sequential phases, each with gated transitions.

**Phases**:

1. **Phase 1: The Scaffold**
   - Goal: Create empty classes/interfaces (compile-only)
   - Success: Project builds with `NotImplementedException`

2. **Phase 2: Narrow Test Definition**
   - Goal: Write failing tests (RED state)
   - Success: All tests fail, no implementation code

3. **Phase 3: The TDD Loop**
   - Goal: Implement one test at a time (GREEN state)
   - Success: All tests pass, no `NotImplementedException`

4. **Phase 4: Integration & Refactoring**
   - Goal: Wire up DI, refactor for SOLID
   - Success: Integration tests pass, production-ready

**Phase Transition Gates**:

- **Phase 1 → 2**: Must compile
- **Phase 2 → 3**: Must have failing tests (RED)
- **Phase 3 → 4**: All tests must pass (GREEN)
- **Phase 4 → Done**: Code review complete

---

### TEST.md (The Inspection Criteria)

**Purpose**: Provide test templates and scenarios derived from DESIGN.md.

**Key Sections**:

1. **Unit Tests**: Scenario-based tests with TODO comments
2. **Integration Tests**: End-to-end flow validation
3. **Performance Tests**: Benchmark thresholds
4. **Security Tests**: Input validation and sanitization
5. **Manual Testing**: UI/UX verification checklist

**Example Test Template**:

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
    viewModel.SearchText = string.Empty;

    // Act
    viewModel.SubmitCommand.Execute().Subscribe();

    // Assert
    viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
}
```

---

### README.md (The Route Map)

**Purpose**: Serve as the entry point for AI agents with explicit instructions.

**Key Sections**:

1. **AI Development Flow**: Step-by-step guide for agents
2. **AI Agent Directive**: Copy-paste prompt for chat sessions
3. **Environment Specifications**: Frameworks, dependencies, build commands
4. **Project Context**: LunaDraw-specific patterns and coding standards

**Example AI Directive**:

```
Act as a TDD expert working on the LunaDraw .NET MAUI project.

Current Phase: Phase 1 (Scaffold)

Read DESIGN.md and create ONLY empty classes based on the signatures.
All methods must throw NotImplementedException.
Mark classes with [Obsolete("Work in Progress")].
Your ONLY goal is a compiling project.
```

---

## Workflow: Four Phases

### Phase 1: The Scaffold (Empty Classes)

**Chat Prompt**:

```
You are a C# scaffolding expert. Read DESIGN.md and create empty classes/interfaces.
All methods must throw NotImplementedException.
Add [Obsolete("Work in Progress")] to all classes.
Verify the project compiles: dotnet build
```

**Tasks**:

1. Create all interfaces from DESIGN.md
2. Create all classes with constructor signatures only
3. Stub all methods with `throw new NotImplementedException();`
4. Run `dotnet build` (must succeed)

**Exit Criteria**: ✅ Project builds with zero errors

---

### Phase 2: Narrow Test Definition (Write Failing Tests)

**Chat Prompt**:

```
You are a TDD expert. Read the "Plain English Test Scenarios" from DESIGN.md.
For each scenario, create a corresponding xUnit test method.
Use naming: Should_{Expected}_{When}_{Condition}
Write ONLY the test—do NOT implement the code to make it pass.
Use Moq to mock dependencies.
Run dotnet test (expect all to FAIL—this is RED state).
```

**Tasks**:

1. Create test project: `tests/{name}.Tests/`
2. Add xUnit, Moq, FluentAssertions packages
3. Translate each Plain English Scenario into a test method
4. Write assertions FIRST
5. Run `dotnet test` (expect failures)

**Exit Criteria**: ✅ All tests FAIL (RED state)

---

### Phase 3: The TDD Loop (Implement One Test at a Time)

**Chat Prompt**:

```
You are a TDD practitioner. Read TEST.md for the first failing test.
Write the MINIMAL code to make ONLY that test pass.
Do NOT implement features not covered by tests.
After the test passes, wait for confirmation before proceeding to the next test.
Use compiler errors to self-correct type mismatches.
```

**Process** (Repeat for EACH test):

1. **RED**: Run `dotnet test`, identify ONE failing test
2. **GREEN**: Write minimal code to pass that test
3. **VERIFY**: Run `dotnet test --filter "FullyQualifiedName~TestMethodName"`
4. **REFACTOR**: Clean up code (extract methods, rename variables)
5. **COMMIT**: `git commit -m "test: pass TestMethodName"`
6. **REPEAT**: Move to next failing test

**Exit Criteria**: ✅ ALL tests pass (GREEN state)

---

### Phase 4: Integration & Refactoring (Wire Up DI, SOLID)

**Chat Prompt**:

```
You are a C# architect. Review the code for SOLID violations and code smells.
Ensure all dependencies are registered in MauiProgram.cs.
Add integration tests that verify end-to-end flows.
Do NOT add features—only improve code structure.
Remove [Obsolete] attributes.
```

**Tasks**:

1. Register all services in `MauiProgram.cs` DI container
2. Add integration tests (ViewModel → Service → Data)
3. Refactor for SOLID principles
4. Add XML documentation comments
5. Run `dotnet test` (all tests must pass)

**Exit Criteria**: ✅ Production-ready code

---

## Chat Session Management

### Why New Chat Windows per Phase?

**Problem**: AI agents suffer from **context drift**—as conversations grow longer, the agent may:

- Forget earlier instructions
- Introduce inconsistencies
- Mix concerns from different phases

**Solution**: Use a **NEW chat window for EACH phase** to minimize context drift.

### Phase Transition Checklist

Before starting a new phase:

1. ✅ Verify current phase exit criteria met
2. ✅ Open NEW chat window
3. ✅ Copy AI Agent Directive from README.md
4. ✅ Update `Current Phase: [Phase X]`
5. ✅ Link to previous phase's chat session in README.md "Notes" section

**Example README.md Notes Section**:

```markdown
## Notes

### Context Links
- Phase 1 Chat: https://chatgpt.com/c/abc123
- Phase 2 Chat: https://chatgpt.com/c/def456
- Phase 3 Chat: https://chatgpt.com/c/ghi789
```

---

## Anti-Patterns to Avoid

### ❌ "Vibe Coding" (Hallucinated Features)

**Symptom**: AI adds features not defined in DESIGN.md

**Example**:

```csharp
// ❌ BAD: AI added pagination when only search was specified
public async Task<PagedResult<Item>> SearchAsync(string query, int page, int pageSize)
```

**Fix**: Add to DESIGN.md Non-Goals:

```markdown
### Non-Goals
- ❌ Pagination (implement in future spec)
```

---

### ❌ "Big Bang" Implementation

**Symptom**: AI writes all code at once instead of one test at a time

**Example**:

```
// ❌ BAD: AI implements 10 methods in one go
Implemented SearchViewModel: 10 methods, 300 lines of code
```

**Fix**: Use Phase 3 chat prompt:

```
Write MINIMAL code to pass ONLY the first failing test.
Wait for my confirmation before proceeding.
```

---

### ❌ Tests Written After Implementation

**Symptom**: AI writes code first, then adds tests to match

**Example**:

```
1. Implemented SearchViewModel.cs
2. Added tests for SearchViewModel
```

**Fix**: Enforce Phase 2 (Test Definition) BEFORE Phase 3 (Implementation):

```
Do NOT write ANY implementation code in Phase 2.
Your ONLY goal is failing tests (RED state).
```

---

### ❌ Skipping Scaffold Phase

**Symptom**: AI jumps straight to implementation without defining interfaces

**Example**:

```csharp
// ❌ BAD: No interface defined, direct implementation
public class SearchService
{
    public async Task Search(string query) { /* code */ }
}
```

**Fix**: Phase 1 must create interfaces FIRST:

```csharp
// ✅ GOOD: Interface defined in Phase 1
public interface ISearchService
{
    Task<IEnumerable<Item>> SearchAsync(string query);
}

[Obsolete("Work in Progress")]
public class SearchService : ISearchService
{
    public Task<IEnumerable<Item>> SearchAsync(string query)
    {
        throw new NotImplementedException();
    }
}
```

---

## Example Walkthrough

### Scenario: Implement a Search Feature

#### Step 1: Create Spec Documents

```bash
# Create new spec
lean-spec create "search-feature"

# Files created:
# 001-search-feature/README.md
# 001-search-feature/DESIGN.md
# 001-search-feature/PLAN.md
# 001-search-feature/TEST.md
```

---

#### Step 2: Fill Out DESIGN.md

```markdown
## Goals & Non-Goals

### Goals
- ✅ Implement search with debounce (500ms)
- ✅ Display search results in a list

### Non-Goals
- ❌ Advanced filtering
- ❌ Pagination

## Interface & Method Signatures

### Interface: ISearchService

```csharp
public interface ISearchService
{
    Task<IEnumerable<Item>> SearchAsync(string query);
}
```

### Class: SearchViewModel

```csharp
public class SearchViewModel : ReactiveObject
{
    [Reactive] public string SearchText { get; set; }
    [Reactive] public ObservableCollection<Item> Results { get; set; }

    public ReactiveCommand<string, IEnumerable<Item>> SearchCommand { get; }

    public SearchViewModel(ISearchService searchService);
}
```

## Plain English Test Scenarios

### Scenario: Search returns results
- **Given**: Service returns 3 items for query "test"
- **When**: User types "test" in search box
- **Then**: Results collection contains 3 items

### Scenario: Empty search clears results
- **Given**: Results collection has items
- **When**: User clears search text
- **Then**: Results collection is empty
```

---

#### Step 3: Phase 1 - Scaffold

**Chat Prompt**:

```
Act as a C# scaffolding expert.

Read the spec at: 001-search-feature/DESIGN.md

Create ONLY empty classes and interfaces based on the signatures defined.
All methods must throw NotImplementedException.
Add [Obsolete("Work in Progress")] to all classes.

Current Phase: Phase 1 (Scaffold)
```

**AI Output**:

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

// SearchViewModel.cs
[Obsolete("Work in Progress")]
public class SearchViewModel : ReactiveObject
{
    [Reactive] public string SearchText { get; set; }
    [Reactive] public ObservableCollection<Item> Results { get; set; }

    public ReactiveCommand<string, IEnumerable<Item>> SearchCommand { get; }

    public SearchViewModel(ISearchService searchService)
    {
        throw new NotImplementedException();
    }
}
```

**Verify**:

```bash
dotnet build
# Build succeeded. 0 Warning(s). 0 Error(s).
```

✅ **Phase 1 Complete**: Project compiles

---

#### Step 4: Phase 2 - Narrow Test Definition

**Open NEW Chat Window**

**Chat Prompt**:

```
Act as a TDD expert.

Read the spec at: 001-search-feature/DESIGN.md
Read the test template at: 001-search-feature/TEST.md

For each "Plain English Test Scenario", create a corresponding xUnit test.
Use naming: Should_{Expected}_{When}_{Condition}
Write ONLY the test—do NOT implement the code to make it pass.
Use Moq to mock ISearchService.

Current Phase: Phase 2 (Narrow Test Definition)
Previous Phase: [Link to Phase 1 chat]
```

**AI Output**:

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
            new Item { Id = 2, Name = "Test 2" },
            new Item { Id = 3, Name = "Test 3" }
        };
        mockService.Setup(s => s.SearchAsync("test"))
            .ReturnsAsync(testItems);

        var viewModel = new SearchViewModel(mockService.Object);

        // Act
        await viewModel.SearchCommand.Execute("test");

        // Assert
        viewModel.Results.Should().HaveCount(3);
        viewModel.Results.Should().BeEquivalentTo(testItems);
    }

    [Fact]
    public void Should_Clear_Results_When_Search_Text_Is_Empty()
    {
        // Arrange
        var mockService = new Mock<ISearchService>();
        var viewModel = new SearchViewModel(mockService.Object);
        viewModel.Results.Add(new Item { Id = 1 });

        // Act
        viewModel.SearchText = string.Empty;

        // Assert
        viewModel.Results.Should().BeEmpty();
    }
}
```

**Verify**:

```bash
dotnet test
# Test Run Failed.
# Total tests: 2
#      Passed: 0
#      Failed: 2
```

✅ **Phase 2 Complete**: All tests FAIL (RED state)

---

#### Step 5: Phase 3 - TDD Loop

**Open NEW Chat Window**

**Chat Prompt**:

```
Act as a TDD practitioner.

Read the spec at: 001-search-feature/TEST.md

Implement MINIMAL code to pass the FIRST failing test ONLY:
- Should_Populate_Results_When_Search_Returns_Items

Do NOT implement the second test yet.
Wait for my confirmation before proceeding.

Current Phase: Phase 3 (TDD Loop - Test 1)
Previous Phase: [Link to Phase 2 chat]
```

**AI Output**:

```csharp
// SearchViewModel.cs
public class SearchViewModel : ReactiveObject
{
    private readonly ISearchService searchService;

    [Reactive] public string SearchText { get; set; }
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

**Verify**:

```bash
dotnet test --filter "Should_Populate_Results_When_Search_Returns_Items"
# Test Run Successful.
# Total tests: 1
#      Passed: 1
```

✅ **Test 1 GREEN**

**Commit**:

```bash
git add .
git commit -m "test: pass Should_Populate_Results_When_Search_Returns_Items"
```

**Continue to Test 2** (repeat RED-GREEN-REFACTOR for remaining tests)

---

#### Step 6: Phase 4 - Integration & Refactoring

**Open NEW Chat Window**

**Chat Prompt**:

```
Act as a C# architect.

Review the implemented SearchViewModel for SOLID violations.
Ensure ISearchService is registered in MauiProgram.cs.
Add integration test that verifies end-to-end flow.
Remove [Obsolete] attributes.

Current Phase: Phase 4 (Integration & Refactoring)
Previous Phase: [Link to Phase 3 chat]
```

**AI Output**:

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
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
    var mockRepository = new Mock<IItemRepository>();
    mockRepository.Setup(r => r.SearchAsync("test"))
        .ReturnsAsync(new List<Item> { new Item { Id = 1 } });

    var service = new SearchService(mockRepository.Object);
    var viewModel = new SearchViewModel(service);

    await viewModel.SearchCommand.Execute("test");

    viewModel.Results.Should().HaveCount(1);
}
```

**Verify**:

```bash
dotnet test
# Test Run Successful.
# Total tests: 3
#      Passed: 3
```

✅ **Phase 4 Complete**: Production-ready

---

## Summary

By refactoring the lean-spec templates for TDD:

1. **DESIGN.md** becomes the architectural blueprint with hard boundaries
2. **PLAN.md** enforces scaffold-first, phase-gated development
3. **TEST.md** provides scenario-based test templates with TODO comments
4. **README.md** serves as the AI-centric route map

This approach **prevents "vibe coding"** and ensures AI agents follow a disciplined, compiler-driven, test-first workflow.

---

## Quick Reference

### Phase Checklist

- [ ] **Phase 1**: Scaffold empty classes → ✅ Compiles
- [ ] **Phase 2**: Write failing tests → ✅ RED state
- [ ] **Phase 3**: Implement one test at a time → ✅ GREEN state
- [ ] **Phase 4**: Refactor & integrate → ✅ Production-ready

### Chat Session Strategy

- ✅ Use NEW chat per phase
- ✅ Copy AI Agent Directive from README.md
- ✅ Link previous phase's chat in README.md Notes
- ✅ Verify exit criteria before transitioning

### AI Agent Directive Template

```
Act as a [Role: scaffolding expert / TDD expert / architect].

Read: [DESIGN.md / PLAN.md / TEST.md]

Current Phase: [Phase 1/2/3/4]

Instructions:
[Phase-specific instructions from PLAN.md]
```

---

**Last Updated**: 2025-12-29
**Version**: 1.0
