# Plan: {name}

> Part of [{name}](README.md)

## TDD Development Process

This spec follows **Red-Green-Refactor (RGR)** methodology. Each phase must be completed in order, with green state achieved before proceeding.

### Phase Transition Requirements

**CRITICAL**: Use a NEW chat window for each phase to minimize context drift. Include a link to the previous phase's chat session.

- **Phase 1 → Phase 2**: All scaffolding must compile with `NotImplementedException`
- **Phase 2 → Phase 3**: At least one failing test must exist (RED state)
- **Phase 3 → Phase 4**: All tests must pass (GREEN state)
- **Phase 4 → Complete**: Code review and refactoring complete

## Phase 1: The Scaffold

**Goal**: Create the structural skeleton WITHOUT any business logic. Ensure the project compiles.

**AI Agent Directive**:
> "Act as a C# scaffolding expert. Read [DESIGN.md](DESIGN.md) and create ONLY empty classes and interfaces based on the signatures defined. All methods must contain `throw new NotImplementedException();`. Mark all classes with `[Obsolete("Work in Progress")]`. Do NOT implement any logic. Your only goal is a compiling project."

**Tasks**:
- [ ] Create all interfaces defined in DESIGN.md (Section: Interface & Method Signatures)
- [ ] Create all ViewModel classes with constructor signatures only
- [ ] Create all Service/Repository classes with method stubs
- [ ] Create all Model/Entity classes with properties only
- [ ] Add `[Obsolete("Work in Progress")]` attribute to all classes
- [ ] Verify project compiles: `dotnet build`

**Success Criteria**:
- [ ] Project builds successfully with zero errors
- [ ] All types from DESIGN.md exist in codebase
- [ ] All methods throw `NotImplementedException`
- [ ] No business logic implemented

**New Chat Session**: Link to Phase 1 chat: [Chat Session URL]

---

## Phase 2: Narrow Test Definition

**Goal**: Write failing tests that isolate ViewModels from UI. Define test scenarios WITHOUT implementation.

**AI Agent Directive**:
> "Act as a TDD expert. Read the 'Plain English Test Scenarios' from [DESIGN.md](DESIGN.md). For each scenario, create a corresponding xUnit test method in the appropriate test file. Use the naming convention `Should_{Expected}_{When}_{Condition}`. Write ONLY the test method signature and assertions—do NOT implement the code to make it pass. Use Moq to mock all dependencies. Your goal is RED state (failing tests)."

**Tasks**:
- [ ] Create test project: `tests/{name}.Tests/`
- [ ] Add xUnit, Moq, FluentAssertions NuGet packages
- [ ] Create test file for each ViewModel: `{ViewModelName}Tests.cs`
- [ ] Translate each Plain English Scenario from DESIGN.md into a test method
- [ ] Write assertions FIRST (e.g., `result.Should().NotBeNull();`)
- [ ] Mock all service dependencies using Moq
- [ ] Run tests: `dotnet test` (expect ALL to fail)

**Example Test Template**:

```csharp
[Fact]
public void Should_Show_Error_Message_When_Submit_With_Empty_Field()
{
    // Arrange
    var mockService = new Mock<IExampleService>();
    var viewModel = new ExampleViewModel(mockService.Object);
    viewModel.SearchText = string.Empty;

    // Act
    viewModel.SubmitCommand.Execute().Subscribe();

    // Assert
    viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    viewModel.IsFormValid.Should().BeFalse();
}
```

**Success Criteria**:
- [ ] All test files created and scenarios covered
- [ ] All tests FAIL (RED state)
- [ ] No implementation code written yet
- [ ] Test coverage for all scenarios in DESIGN.md

**New Chat Session**: Link to Phase 2 chat: [Chat Session URL]

---

## Phase 3: The TDD Loop (Red-Green-Refactor)

**Goal**: Implement minimal code to pass tests one at a time. Achieve GREEN state.

**AI Agent Directive**:
> "Act as a TDD practitioner. Read [TEST.md](TEST.md) for the first failing test scenario. Write the MINIMAL code required to make ONLY that test pass. Do NOT implement features not covered by tests. After the test passes, ask me to confirm before proceeding to the next test. Use C# compiler errors to self-correct type mismatches."

**Process** (Repeat for EACH test):

1. **RED**: Run `dotnet test` and identify ONE failing test
2. **GREEN**: Write minimal implementation to pass that test
3. **VERIFY**: Run `dotnet test --filter "FullyQualifiedName~{TestMethodName}"`
4. **REFACTOR**: Clean up code if needed (extract methods, rename variables)
5. **COMMIT**: Commit with message: `test: pass {TestMethodName}`
6. **REPEAT**: Move to next failing test

**Tasks**:
- [ ] Test 1: [Scenario name from DESIGN.md]
  - [ ] RED: Confirm test fails
  - [ ] GREEN: Implement minimal code
  - [ ] VERIFY: Test passes
  - [ ] REFACTOR: Clean up
- [ ] Test 2: [Scenario name from DESIGN.md]
  - [ ] RED: Confirm test fails
  - [ ] GREEN: Implement minimal code
  - [ ] VERIFY: Test passes
  - [ ] REFACTOR: Clean up
- [ ] Test 3: [Scenario name from DESIGN.md]
  - [ ] RED: Confirm test fails
  - [ ] GREEN: Implement minimal code
  - [ ] VERIFY: Test passes
  - [ ] REFACTOR: Clean up

**Success Criteria**:
- [ ] ALL tests pass: `dotnet test` shows 100% success
- [ ] No `NotImplementedException` remaining in production code
- [ ] Code coverage meets minimum threshold (e.g., 80%)
- [ ] GREEN state achieved

**New Chat Session**: Link to Phase 3 chat: [Chat Session URL]

---

## Phase 4: Integration & Refactoring

**Goal**: Integrate components, wire up DI, and refactor for SOLID principles.

**AI Agent Directive**:
> "Act as a C# architect. Review the implemented code for SOLID violations, code smells, and opportunities for refactoring. Ensure all dependencies are registered in `MauiProgram.cs`. Add integration tests that verify end-to-end flows. Do NOT add features—only improve existing code structure."

**Tasks**:
- [ ] Register all services in `MauiProgram.cs` DI container
- [ ] Add integration tests for ViewModel → Service → Data flow
- [ ] Refactor for SOLID principles (SRP, OCP, DIP)
- [ ] Remove `[Obsolete]` attributes
- [ ] Add XML documentation comments to public APIs
- [ ] Run static analysis (if configured)
- [ ] Final test run: `dotnet test`

**Success Criteria**:
- [ ] All services registered in DI container
- [ ] Integration tests pass
- [ ] No SOLID violations
- [ ] Code is production-ready

**New Chat Session**: Link to Phase 4 chat: [Chat Session URL]

---

## Rollout Strategy

<!-- How will this be deployed? -->

**Staging**:
- Build and test on local machine
- Test on Windows target device

**Production**:
- Merge feature branch to main
- Create release build
- Deploy to test devices (Windows/Android/iOS)

**Monitoring**:
- Check for runtime exceptions in logs
- Validate user flows in production

**Rollback Plan**:
- Revert commit if critical bugs found
- Disable feature flag (if applicable)

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| AI generates invalid C# syntax | High (compile failure) | Use compiler feedback to self-correct |
| Tests pass but feature is broken | High (false confidence) | Add integration tests in Phase 4 |
| Context drift across chat sessions | Medium (inconsistent code) | Use NEW chat per phase with links |
| Missing dependencies in DI | Medium (runtime crash) | Validate in Phase 4 integration tests |
