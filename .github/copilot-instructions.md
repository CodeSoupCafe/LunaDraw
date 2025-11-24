# Copilot Instructions for LunaDraw

## Project Overview

- **LunaDraw** is a cross-platform, child-centric drawing app built with .NET MAUI, SkiaSharp (vector graphics), and ReactiveUI.
- The architecture prioritizes simplicity, maintainability, and low coupling. Major features include a drawing canvas, magical brush effects, undo/redo, movie mode (time-lapse), art gallery, and ergonomic UI for children.

## Architecture & Patterns

- **Graphics:** All drawing logic uses SkiaSharp (`SKCanvas`, `SKPaint`, `SKPath`, etc.). See `docs/SkiaSharp.md` for API references.
- **Reactive Programming:** UI and state management leverage ReactiveUI and MessageBus for decoupled communication. Use messages only for events that must reach disconnected components.
- **Commands/Events:** Prefer command/event patterns for tool actions and state changes. See `Logic/Commands/` and `Logic/Messages/`.
- **Models:** Drawable elements are defined in `Logic/Models/` (e.g., `DrawableEllipse`, `DrawablePath`). Tools are in `Logic/Tools/` and must implement `IDrawingTool`.
- **ViewModels:** MVVM pattern is used. Main view logic is in `ViewModels/MainViewModel.cs` and `ViewModels/ToolbarViewModel.cs`.

## Developer Workflow

- **Build:** Use `dotnet build -t:Build -p:Configuration=Debug -f net9.0-windows10.0.19041.0 -p:WindowsPackageType=None LunaDraw.csproj` for Windows builds.
- **Test:** Always write a test for any bug fix before implementing the solution. Tests are in `Logic/Tools/SelectToolTests.cs` and `tests/LunaDraw.Tests/`.
- **Debug:** Use targeted logging and symbolic reasoning to identify root causes. Integrate precise logs for efficient debugging.
- **Manual Verification:** Complement automated tests with manual checks for UI/UX and brush effects.
- **Clean Refactoring** - NEVER keep legacy or duplicate code. A clean state and not leaving behind bloat and obsolete code.

## Project-Specific Conventions

- **SOLID Principles:** Enforce SRP, OCP, LSP, ISP, DIP. Maintain low coupling, high cohesion, and separation of concerns.
- **DRY:** Systematically avoid duplication using symbolic reasoning.
- **File Naming:** Use descriptive, permanent, and standardized names. Avoid one-time scripts.
- **Component Size:** Keep files concise (<300 lines) and proactively refactor.
- **Branching:** Follow defined branching guidelines and commit frequently with clear messages.

## Integration Points & Dependencies

- **SkiaSharp:** All rendering and brush effects use SkiaSharp. See `docs/SkiaSharp.md` for key classes.
- **ReactiveUI:** Used for state management and UI reactivity.
- **MessageBus:** Only for events needing broadcast to low-coupled components.
- **Shaders:** Glow/neon and glitter/sparkle effects use optimized shaders and additive blending (see non-functional requirements in `docs/Features.md`).

## Key Files & Directories

- `Logic/Models/` — Drawable element definitions
- `Logic/Tools/` — Drawing tool implementations
- `Logic/Messages/` — Message/event definitions
- `ViewModels/` — MVVM logic
- `Components/ToolbarView.xaml` — Toolbar UI
- `docs/ArchitectureDesign.md` — Architecture overview
- `docs/Features.md` — Feature requirements
- `docs/SkiaSharp.md` — SkiaSharp API references

## Testing & Validation

- **Test-Driven Development:** Write tests before implementing features or fixes. Provide thorough coverage for critical paths and edge cases.
- **Mandatory Passing:** Immediately address any failing tests.

## Security & Environment

- **No Ads:** App must remain free and ad-free.
- **Offline Mode:** App must be robust offline.
- **Credential Management:** Use environment variables for sensitive data; never hardcode credentials.

---

For unclear or incomplete sections, request clarification or review `docs/ArchitectureDesign.md` and `docs/Features.md` for further context.
