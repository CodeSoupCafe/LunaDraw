---
status: planned
created: '{date}'
tags: []
priority: medium
---

# {name}

> **Status**: {status} · **Priority**: {priority} · **Created**: {date}

## Overview

<!-- What are we solving? Why now? Expected impact? -->

---

## AI Development Flow (Route Map)

**CRITICAL**: This spec follows **Test-Driven Development (TDD)** with **Red-Green-Refactor (RGR)** methodology.

### 📋 Entry Point for AI Agents

Before starting implementation, AI agents MUST:

1. **Read in order**:
   - ✅ This README (context and constraints)
   - ✅ [DESIGN.md](DESIGN.md) (architecture, signatures, test scenarios)
   - ✅ [PLAN.md](PLAN.md) (phase-by-phase execution plan)
   - ✅ [TEST.md](TEST.md) (test structure and examples)

2. **Execute phases sequentially** (see [PLAN.md](PLAN.md)):
   - **Phase 1**: Scaffold (empty classes, compile-only)
   - **Phase 2**: Test Definition (write failing tests, RED state)
   - **Phase 3**: TDD Loop (implement one test at a time, GREEN state)
   - **Phase 4**: Integration & Refactoring (wire up DI, SOLID principles)

3. **Use NEW chat window for EACH phase** to prevent context drift

### 🎯 AI Agent Directive (Copy to Chat)

```
Act as a TDD expert working on the LunaDraw .NET MAUI project.

ENVIRONMENT:
- Framework: .NET 10, MAUI (net10.0-windows10.0.19041.0)
- Testing: xUnit + Moq + FluentAssertions
- Architecture: MVVM with ReactiveUI
- Dependency Injection: Microsoft.Extensions.DependencyInjection

INSTRUCTIONS:
1. Do NOT perform "Big Bang" implementations
2. Read DESIGN.md and PLAN.md BEFORE writing code
3. Follow ONLY the current phase in PLAN.md
4. For Phase 1: Scaffold empty classes with NotImplementedException
5. For Phase 2: Write failing tests based on DESIGN.md scenarios
6. For Phase 3: Implement MINIMAL code to pass ONE test at a time
7. For Phase 4: Refactor for SOLID principles and wire up DI

USE COMPILER FEEDBACK to self-correct type mismatches.
DO NOT add features not defined in DESIGN.md.
DO NOT skip tests or implement multiple features at once.

Current Phase: [Specify Phase 1, 2, 3, or 4]
```

---

## Environment Specifications

### Target Frameworks
- **Primary**: `net10.0-windows10.0.19041.0`
- **Cross-platform**: `net10.0-android36.0`, `net10.0-ios26.0`, `net10.0-maccatalyst26.0`

### Required Dependencies
```xml
<PackageReference Include="ReactiveUI" Version="20.1.63" />
<PackageReference Include="ReactiveUI.Maui" Version="20.1.63" />
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="3.0.0" />
<PackageReference Include="CommunityToolkit.Maui" Version="10.2.0" />
```

### Testing Dependencies
```xml
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
```

### Build & Test Commands
```bash
# Build project
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0

# Run all tests
dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Check code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Sub-Specs (Technical Documentation)

For detailed information, see:

- **[DESIGN.md](DESIGN.md)** - Architecture, interface signatures, and test scenarios
- **[PLAN.md](PLAN.md)** - Phase-by-phase implementation plan with TDD workflow
- **[TEST.md](TEST.md)** - Testing strategy, test templates, and acceptance criteria

---

## Quick Summary

<!-- Brief summary of the spec (2-3 paragraphs max) -->
<!-- What problem does this solve? What is the expected outcome? -->

---

## Project Context (for AI Agents)

### LunaDraw Architecture Patterns

**MVVM with ReactiveUI**:
- ViewModels inherit from `ReactiveObject`
- Use `[Reactive]` attribute for observable properties
- Commands use `ReactiveCommand<TInput, TOutput>`

**Messaging**:
- Prefer reactive observables over `IMessageBus`
- Use `IMessageBus` ONLY for loosely-coupled broadcast messages
- MessageBus is instance-based (injected via DI), NOT static

**Dependency Injection**:
- Services registered in `MauiProgram.cs`
- ViewModels typically Singleton or Transient
- Pages typically Transient

### Coding Standards (from CLAUDE.md)

- ❌ NO underscores in names
- ❌ NO regions
- ❌ NO abbreviations (use full descriptive names)
- ❌ NO legacy or duplicate code
- ✅ Use SOLID principles (SRP, OCP, LSP, ISP, DIP)
- ✅ Test naming: `Should_{Expected}_{When}_{Condition}`
- ✅ One assertion per line in tests
- ✅ Write test BEFORE fixing bugs (TDD)

---

## Notes

<!-- Key decisions, constraints, open questions -->

### Context Links
- Previous Phase Chat: [Link to chat session]
- Related Specs: [Links to dependent specs]
- Design Documents: [Links to additional design docs]

### Open Questions
- [ ] Question 1
- [ ] Question 2

### Constraints
- Must maintain compatibility with existing LunaDraw architecture
- Target audience: Children ages 3-8 (child-friendly UX required)
- Cross-platform: Windows, Android, iOS, MacCatalyst
