---
status: in-progress
created: '2025-12-30'
tags:
  - movie-mode
  - viewport
  - playback
  - ux
  - animation
priority: high
created_at: '2025-12-30T01:42:41.846Z'
updated_at: '2025-12-30T02:22:25.026Z'
transitions:
  - status: in-progress
    at: '2025-12-30T02:22:25.026Z'
---

# Movie Mode Viewport Tracking

> **Status**: ⏳ In progress · **Priority**: High · **Created**: 2025-12-30 · **Tags**: movie-mode, viewport, playback, ux, animation

## Overview

Currently, Movie Mode playback in LunaDraw replays the drawing process by animating drawable elements in chronological order. However, it does not track or replay the user's viewport transformations (pan and zoom) that occurred during the drawing session. This results in a static viewport during playback, which can feel disconnected from the original creative process.

**Problem**: Users often zoom in to add fine details, zoom out for broad strokes, and pan around the canvas while drawing. This navigational context is lost during Movie Mode playback, making it harder to appreciate the creative workflow and potentially confusing for viewers.

**Solution**: Record viewport transformations (stored in `NavigationModel.ViewMatrix`) alongside drawable elements during the drawing session. During playback, smoothly interpolate the viewport between recorded states to follow the artist's perspective as they drew.

**Impact**:
- Enhanced storytelling: Viewers experience the drawing process from the artist's perspective
- Better context: Zooming in/out shows intentionality behind detail work vs. broad strokes
- More engaging playback: Dynamic viewport creates a "camera work" effect
- Child-friendly: Young users will see "how they looked at" their artwork as it was made

**Constraints**:
- Must NOT modify existing movie mode architecture beyond viewport tracking
- Must restore viewport to final drawing state after playback completes
- Must maintain 60 FPS smooth playback performance
- Must gracefully handle legacy drawings without viewport data

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

This feature adds viewport tracking to Movie Mode, allowing playback to follow the user's zoom and pan movements as they created their drawing. The implementation records viewport snapshots at key moments (when elements are created or when the viewport changes significantly), then smoothly interpolates between these states during playback.

The core change involves extending `IDrawableElement` to optionally store a `ViewportSnapshot` (containing the SKMatrix from NavigationModel), modifying `PlaybackHandler` to apply viewport transformations during playback, and ensuring the viewport returns to its final state when playback completes. Legacy drawings without viewport data will continue to work with a static viewport.

Expected outcome: A more immersive, context-rich playback experience that shows not just what was drawn, but how the artist navigated the canvas while creating their masterpiece.

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
- [x] Should viewport snapshots be stored per element or as separate timeline events? **Decision: Store optional snapshot per element for simplicity**
- [x] How to handle drawings created before this feature? **Decision: Graceful degradation - use identity matrix if no snapshot exists**
- [x] Should viewport interpolation be linear or eased? **Decision: Start with linear, add easing in Phase 4 if time permits**

### Key Decisions
- **Non-Invasive Design**: Extend existing architecture minimally - add optional `ViewportSnapshot` property to `IDrawableElement`
- **Backward Compatibility**: Legacy drawings without viewport data use current viewport (no breaking changes)
- **Performance**: Viewport updates happen at 60 FPS in sync with existing animation timer
- **Restoration**: After playback (Stop or Complete), viewport returns to the final drawing state captured before playback started

### Constraints
- Must maintain compatibility with existing LunaDraw architecture
- Must NOT break serialization/deserialization of existing drawings
- Target audience: Children ages 3-8 (child-friendly UX required)
- Cross-platform: Windows, Android, iOS, MacCatalyst
- Must maintain 60 FPS playback performance
