<!--
Sync Impact Report:
- Version Change: 2.0.0 -> 2.1.0
- Modified Principles:
    - [PRINCIPLE_3] -> III. Test-First & Quality (Expanded)
    - [PRINCIPLE_4] -> IV. SOLID & Clean Code (Expanded)
    - [PRINCIPLE_6] -> VI. Architecture Patterns & Naming (Refined)
- Added Sections:
    - VII. SPARC Methodology & Agentic Workflow (New)
- Templates Requiring Updates: None (Content expansion only)
- Follow-up TODOs: None
-->

# LunaDraw Constitution

## Core Principles

### I. Child-Centric UX
Target audience is children aged 3-8. User interface must prioritize:
- **Large Touch Targets**: Minimum 2cm x 2cm for all interactive elements.
- **Visual over Text**: Use icons, animations, and multi-sensory feedback (sounds) instead of text labels where possible.
- **Guidance**: Provide visual/audio guidance rather than explicit instructions.
- **Simplicity**: Minimal UI, hidden complexity, and immediate feedback.

### II. Reactive Architecture
The application utilizes the MVVM pattern with ReactiveUI.
- **Observables**: Use Reactive Observables for state management and component communication.
- **View-ViewModel Binding**: ViewModels inherit from `ReactiveObject`. Use `this.RaiseAndSetIfChanged` for properties.
- **Messaging**: Use `IMessageBus` strictly for loosely-coupled broadcast messages between disconnected components. It must be instance-based (injected), NOT static.

### III. Test-First & Quality
Testing is non-negotiable and precedes implementation for bug fixes.
- **Test-First Bug Fixes**: ALWAYS write a failing test first to validate a bug before fixing it.
- **Tools**: xUnit, Moq, FluentAssertions.
- **Structure**: Arrange-Act-Assert (AAA). Naming: `Should_When_Returns`.
- **Scope**: Prefer `[Theory]` and `[InlineData]` over multiple `[Fact]` methods. Include negative test cases.
- **Assertions**: Tests should only Assert a singular item on one line.
- **Naming Constraints**: DO NOT use 'sut' or arbitrary names. Use ONLY the class name (e.g., `mockClassName`).

### IV. SOLID & Clean Code
Code must adhere to clean coding standards to maintain maintainability.
- **Principles**: Strictly follow SOLID (SRP, OCP, LSP, ISP, DIP), DRY, Low Coupling, High Cohesion, and Separation of Concerns.
- **Refactoring**: NEVER keep legacy or duplicate code. Refactor to a clean state; do not leave "bloat".
- **Naming**: No underscores, no regions. DO NOT use abbreviations for anything (variable names or otherwise). Use descriptive names.
- **Static Extensions**: Use ONLY for re-usable logic, not for core business logic.
- **Regions**: No #regions allowed.

### V. SkiaSharp & Performance
All graphics rendering is handled via SkiaSharp.
- **Rendering**: Use `SKCanvas`, `SKPaint`, and `SKPath`.
- **Performance**: Optimize for high frame rates. Use QuadTree for spatial indexing and culling off-screen elements.
- **Vector Graphics**: Render directly from vector elements; avoid unnecessary bitmap caching unless strictly required for performance (e.g., complex layers).

### VI. Architecture Patterns & Naming
Naming conventions MUST reflect the structural pattern used.
- **Prohibited Terms**: "Service", "Manager". Do NOT use these vague suffixes.
- **Allowed Patterns**: Use specific Gang of Four (GoF) or SOLID patterns (e.g., `Memento`, `Facade`, `Factory`, `Strategy`, `Observer`).
- **Data Access**: The Repository pattern is prohibited. Use Command Handlers (Create, Update) connected to a Domain Facade (namespaced by domain, e.g., Users, Customers) for LINQ/DB queries.
- **Modularity**: Code must be modular and separated by domain.

### VII. SPARC Methodology & Agentic Workflow
Development follows the SPARC (Specification, Pseudocode, Architecture, Refinement, Completion) methodology.
- **Simplicity**: Prioritize clear, maintainable solutions; minimize complexity.
- **Documentation First**: Review/Create documentation (PRDs, specs) before implementation.
- **Agentic Collaboration**: Use `.clinerules` and `.cursorrules` to guide autonomous behaviors.
- **Memory Bank**: Continuously retain context to ensure coherent long-term planning.

## Architecture & Implementation Details

- **Framework**: .NET MAUI targeting Windows, Android, iOS, MacCatalyst.
- **Dependency Injection**: Components, Handlers, and Facades registered in `MauiProgram.cs`. ViewModels and Pages typically Transient or Singleton as appropriate.
- **Drawing Model**: `IDrawableElement` is the base for all drawn objects. `Layer` containers manage elements with `ObservableCollection`.
- **Tool System**: `IDrawingTool` interface for all tools. Input handled centrally by `CanvasInputHandler`.

## Development Workflow

- **Commit Protocol**: Check `git status` and `git diff` before committing. Write clear, "why"-focused commit messages.
- **Branching**: Use feature branches.
- **Verification**: Run tests (`dotnet test`) and build (`dotnet build`) before considering a task complete.
- **Legacy Migration**: Be aware of legacy code from `SurfaceBurnCalc`. Refactor and verify when touching these areas.

## Governance

- **Supremacy**: This Constitution supersedes all other practices.
- **Amendments**: Changes require documentation in this file and a version bump.
- **Compliance**: All PRs and code changes must verify compliance with these principles.
- **Runtime Guidance**: Refer to `CLAUDE.md` and `.clinerules` for specific day-to-day development commands and patterns.

**Version**: 2.1.0 | **Ratified**: 2025-12-23 | **Last Amended**: 2025-12-23