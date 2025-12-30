# Lean-Spec TDD Templates - Index

> **Version**: 1.0
> **Last Updated**: 2025-12-29
> **Purpose**: TDD-optimized specification templates for AI-driven .NET MAUI development

---

## 📋 Quick Navigation

| Document | Purpose | Audience |
|----------|---------|----------|
| **[README.md](README.md)** | Entry point and route map for AI agents | AI Agents + Humans |
| **[DESIGN.md](DESIGN.md)** | Architectural blueprint with signatures and scenarios | Architects + AI Agents |
| **[PLAN.md](PLAN.md)** | Phase-by-phase TDD execution plan | AI Agents (Implementation) |
| **[TEST.md](TEST.md)** | Test templates and acceptance criteria | AI Agents (Testing) |
| **[TDD-WORKFLOW-GUIDE.md](TDD-WORKFLOW-GUIDE.md)** | Comprehensive workflow guide | Humans (Learning) |
| **[REFACTORING-SUMMARY.md](REFACTORING-SUMMARY.md)** | What changed and why | Humans (Context) |

---

## 🚀 Getting Started

### For Humans (Spec Authors)

1. **Start here**: [REFACTORING-SUMMARY.md](REFACTORING-SUMMARY.md) - Understand the refactoring rationale
2. **Learn the workflow**: [TDD-WORKFLOW-GUIDE.md](TDD-WORKFLOW-GUIDE.md) - Deep dive into the 4-phase process
3. **Create a spec**: Use the templates (README, DESIGN, PLAN, TEST) to define your feature
4. **Hand off to AI**: Copy the AI Agent Directive from [README.md](README.md) into your chat session

### For AI Agents (Implementation)

1. **Start here**: [README.md](README.md) - Entry point with AI Development Flow
2. **Read in order**:
   - [DESIGN.md](DESIGN.md) - Architecture, signatures, test scenarios
   - [PLAN.md](PLAN.md) - Phase-by-phase execution plan
   - [TEST.md](TEST.md) - Test structure and templates
3. **Execute phases sequentially**:
   - Phase 1: Scaffold (empty classes, compile-only)
   - Phase 2: Test Definition (write failing tests, RED state)
   - Phase 3: TDD Loop (implement one test at a time, GREEN state)
   - Phase 4: Integration & Refactoring (wire up DI, SOLID principles)
4. **Use NEW chat window for EACH phase** to minimize context drift

---

## 📖 Document Details

### Core Templates (Used for Every Spec)

#### [README.md](README.md) - Route Map

**Contains**:
- Overview and context
- AI Development Flow (step-by-step guide)
- AI Agent Directive (copy-paste prompt)
- Environment specifications (.NET 10, MAUI, dependencies)
- Project context (LunaDraw patterns, coding standards)

**Use when**:
- Starting a new spec (fill out overview)
- Beginning a new phase (copy AI Agent Directive)
- Linking chat sessions (update Notes section)

---

#### [DESIGN.md](DESIGN.md) - Architectural Blueprint

**Contains**:
- Goals & Non-Goals (explicit boundaries)
- Visual Architecture (Mermaid diagrams)
- Component Hierarchy (MVVM structure)
- Interface & Method Signatures (type-safe definitions)
- Plain English Test Scenarios (Given-When-Then)
- Technical Approach (frameworks, patterns)
- Design Decisions (rationale and trade-offs)

**Use when**:
- Defining system architecture
- Specifying public APIs
- Creating test scenarios
- Preventing scope creep (Non-Goals)

**Key for AI agents**: Provides type signatures to prevent compilation-driven hallucinations

---

#### [PLAN.md](PLAN.md) - Phase-by-Phase Execution Plan

**Contains**:
- TDD Development Process overview
- Phase Transition Requirements (gated progression)
- **Phase 1: The Scaffold** - Empty classes, `NotImplementedException`
- **Phase 2: Narrow Test Definition** - Write failing tests (RED)
- **Phase 3: The TDD Loop** - Implement one test at a time (GREEN)
- **Phase 4: Integration & Refactoring** - Wire up DI, SOLID principles
- Rollout Strategy
- Risks & Mitigation

**Use when**:
- Executing implementation (AI agents follow phase by phase)
- Tracking progress (check off tasks within each phase)
- Gating transitions (verify success criteria before moving to next phase)

**Key for AI agents**: Contains AI Agent Directives for each phase

---

#### [TEST.md](TEST.md) - Test Templates & Acceptance Criteria

**Contains**:
- Testing Strategy (test pyramid)
- Unit Tests (scenario-based with TODO comments)
- Test Naming Convention (`Should_{Expected}_{When}_{Condition}`)
- Integration Tests (end-to-end flows)
- Performance Tests (benchmarks)
- Security Tests (input validation)
- Test Data (TestDataBuilder pattern)
- Acceptance Criteria (definition of done)

**Use when**:
- Writing tests in Phase 2 (translate scenarios to test methods)
- Validating implementation in Phase 3 (run tests)
- Defining acceptance criteria (what makes the feature "done")

**Key for AI agents**: Provides concrete test templates derived from DESIGN.md scenarios

---

### Documentation (Reference for Humans)

#### [TDD-WORKFLOW-GUIDE.md](TDD-WORKFLOW-GUIDE.md) - Comprehensive Guide

**Contains**:
- Overview of TDD philosophy
- Red-Green-Refactor (RGR) explanation
- Document structure breakdown
- Four-phase workflow walkthrough
- Chat session management strategy
- Anti-patterns to avoid (e.g., "vibe coding", "Big Bang" implementation)
- Complete example walkthrough (Search feature)

**Use when**:
- Learning the TDD workflow
- Onboarding new team members
- Troubleshooting issues (anti-patterns section)
- Creating new specs (example walkthrough)

---

#### [REFACTORING-SUMMARY.md](REFACTORING-SUMMARY.md) - What Changed & Why

**Contains**:
- What changed (before/after comparison)
- Document-by-document changes
- Key improvements (prevents "vibe coding", enforces test-first, etc.)
- How to use the refactored templates
- Migration guide (updating existing specs)
- Benefits summary

**Use when**:
- Understanding the rationale for the refactoring
- Migrating existing specs to the new format
- Explaining the new workflow to stakeholders

---

## 🔄 Workflow Summary

### The 4-Phase TDD Process

```
┌──────────────────────────────────────────────────────────────────┐
│                       PHASE 1: SCAFFOLD                          │
│  Goal: Empty classes with NotImplementedException               │
│  Success: Project compiles                                       │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                  PHASE 2: TEST DEFINITION                        │
│  Goal: Write failing tests (RED state)                           │
│  Success: All tests FAIL                                         │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    PHASE 3: TDD LOOP                             │
│  Goal: Implement one test at a time (GREEN state)                │
│  Success: All tests PASS                                         │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│              PHASE 4: INTEGRATION & REFACTOR                     │
│  Goal: Wire up DI, refactor for SOLID                            │
│  Success: Production-ready code                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🎯 AI Agent Quick Start

### Copy-Paste This Into Your Chat

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
Previous Phase Chat: [Link to previous chat session]

Read the spec at: [Path to spec directory]
```

---

## 📊 File Structure Example

When you create a new spec using these templates:

```
specs/NNN-feature-name/
├── README.md                  # Route map (start here for AI agents)
├── DESIGN.md                  # Architectural blueprint
├── PLAN.md                    # Phase-by-phase execution plan
├── TEST.md                    # Test templates
└── [Additional files as needed]
```

---

## ✅ Phase Checklist

Use this checklist to track progress through the workflow:

- [ ] **Spec Definition**
  - [ ] Fill out README.md (overview, context)
  - [ ] Define Goals & Non-Goals in DESIGN.md
  - [ ] Create Mermaid diagram in DESIGN.md
  - [ ] Define all interface signatures in DESIGN.md
  - [ ] Write Plain English Test Scenarios in DESIGN.md
  - [ ] Review PLAN.md phases (customize if needed)

- [ ] **Phase 1: Scaffold** (New Chat)
  - [ ] Copy AI Agent Directive (Phase 1) to chat
  - [ ] AI creates empty classes/interfaces
  - [ ] Verify: `dotnet build` succeeds
  - [ ] Link chat session in README.md Notes

- [ ] **Phase 2: Test Definition** (New Chat)
  - [ ] Copy AI Agent Directive (Phase 2) to chat
  - [ ] AI writes failing tests for each scenario
  - [ ] Verify: `dotnet test` shows RED state (all tests fail)
  - [ ] Link chat session in README.md Notes

- [ ] **Phase 3: TDD Loop** (New Chat per test or group)
  - [ ] Copy AI Agent Directive (Phase 3) to chat
  - [ ] For each test: RED → GREEN → REFACTOR
  - [ ] Commit after each test passes: `git commit -m "test: pass TestName"`
  - [ ] Verify: `dotnet test` shows GREEN state (all tests pass)
  - [ ] Link chat session in README.md Notes

- [ ] **Phase 4: Integration & Refactoring** (New Chat)
  - [ ] Copy AI Agent Directive (Phase 4) to chat
  - [ ] AI registers services in MauiProgram.cs
  - [ ] AI adds integration tests
  - [ ] AI refactors for SOLID principles
  - [ ] Remove `[Obsolete]` attributes
  - [ ] Verify: `dotnet test` passes, code is production-ready
  - [ ] Link chat session in README.md Notes

- [ ] **Completion**
  - [ ] Update spec status: `planned` → `in-progress` → `complete`
  - [ ] Final code review
  - [ ] Merge feature branch
  - [ ] Archive spec (if appropriate)

---

## 🛠️ Common Commands

### Build & Test

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

### Lean-Spec Commands

```bash
# Create new spec
lean-spec create "feature-name"

# List specs
lean-spec list

# View spec
lean-spec view "feature-name"

# Update spec status
lean-spec update "feature-name" --status in-progress
```

---

## 🚨 Anti-Patterns to Avoid

| Anti-Pattern | Symptom | Fix |
|--------------|---------|-----|
| **Vibe Coding** | AI adds features not in DESIGN.md | Add to Non-Goals section |
| **Big Bang** | AI implements all code at once | Use Phase 3 chat prompt (one test at a time) |
| **Tests After** | AI writes code first, then tests | Enforce Phase 2 before Phase 3 |
| **Skipping Scaffold** | No interfaces defined | Complete Phase 1 before Phase 2 |
| **Context Drift** | AI forgets earlier instructions | Use NEW chat per phase |

---

## 📚 Additional Resources

- **LunaDraw CLAUDE.md**: Project-specific coding standards and architecture
- **Workspace CLAUDE.md**: Multi-project workspace overview
- **.clinerules**: Strict coding rules (NO underscores, NO regions, etc.)

---

## 🔗 Quick Links

- [README Template](README.md) - Route map for AI agents
- [DESIGN Template](DESIGN.md) - Architectural blueprint
- [PLAN Template](PLAN.md) - Phase-by-phase plan
- [TEST Template](TEST.md) - Test templates
- [Workflow Guide](TDD-WORKFLOW-GUIDE.md) - Comprehensive guide
- [Refactoring Summary](REFACTORING-SUMMARY.md) - What changed and why

---

## 📝 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-29 | Initial refactoring for TDD/RGR methodology |

---

## 💡 Tips for Success

1. **Always start with DESIGN.md**: Define Goals, Non-Goals, and test scenarios before writing code
2. **Use Mermaid diagrams**: Visualize architecture before implementation
3. **Define interfaces upfront**: Let the compiler guide the AI
4. **New chat per phase**: Prevent context drift
5. **One test at a time**: Resist the urge to implement everything at once
6. **Link chat sessions**: Maintain continuity across phases
7. **Verify gates**: Don't proceed to next phase until success criteria met

---

**Happy TDD-ing!** 🎉

For questions or improvements, update this index and related documents as you refine the process.
