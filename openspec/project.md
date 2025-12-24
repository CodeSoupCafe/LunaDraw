# Project Context

## Purpose
LunaDraw is a child-centric drawing application designed for children aged 3–8. It provides a safe, ad-free, and magical environment for creativity, featuring special effects brushes, easy-to-use tools, and a "Movie Mode" that replays the drawing process. The app prioritizes immediate visual feedback, ergonomic design for small hands, and a "vibe-coded" user experience.

## Tech Stack
- **Framework:** .NET MAUI (targeting .NET 10.0)
- **Language:** C#
- **Graphics Engine:** SkiaSharp (SkiaSharp.Views.Maui.Controls)
- **MVVM Framework:** ReactiveUI (primary), CommunityToolkit.Mvvm
- **UI Components:** CommunityToolkit.Maui, nor0x.Maui.ColorPicker
- **Dependency Injection:** .NET MAUI built-in DI
- **Testing:** xUnit, Moq, AwesomeValidation

## Project Conventions

### Code Style
- **ReactiveUI:** Utilize ReactiveUI for ViewModels, command binding, and property change notifications (`ReactiveObject`, `[Reactive]`).
- **SkiaSharp:** All custom drawing and rendering logic should use SkiaSharp.
- **Extensions:** Use static extensions ONLY for truly re-usable logic.
- **Formatting:** Standard C# formatting conventions.
- **Comments:** Sparse, high-value comments focusing on "why" rather than "what".

### Architecture Patterns
- **MVVM:** Strict Model-View-ViewModel separation.
- **Messaging:** Use `MessageBus` sparingly for broadcasting events between loosely coupled components (e.g., `CanvasInvalidateMessage`).
- **Dependency Injection:** Register services and ViewModels in `MauiProgram.cs`.
- **Legacy Integration:** Some canvas functionality is adapted from `Legacy\SurfaceBurnCalc`.

### Testing Strategy
- **Unit Tests:** Located in `LunaDraw.Tests`. Run via `dotnet test`.
- **Frameworks:** xUnit, Moq.
- **Scope:** Focus on business logic and ViewModel states.

### Git Workflow
- Standard branching and commit strategies.
- Commit messages should be clear, concise, and explain the "why".

## Domain Context
- **Target Audience:** Children aged 3-8 (requiring large touch targets, simple icons, and immediate feedback).
- **Core Features:**
  - **Magical Brushes:** Glow, sparkles, rainbow effects.
  - **Movie Mode:** Time-lapse recording and playback of the drawing session.
  - **Stamps & Shapes:** Easy placement of pre-defined assets.
  - **Trace Mode:** Transparency overlay for tracing (Windows specific).

## Important Constraints
- **Ad-Free:** No advertisements or IAP.
- **Offline-First:** Fully functional without an internet connection.
- **Platform Targets:** Windows, Android, iOS, MacCatalyst.
- **Performance:** Must remain responsive on mobile devices despite heavy graphical effects (SkiaSharp).

## External Dependencies
- **SkiaSharp:** Primary rendering engine.
- **ReactiveUI:** State management core.
- **CodeSoupCafe.Maui:** Custom internal library.