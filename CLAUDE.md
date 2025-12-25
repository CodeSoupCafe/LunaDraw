# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LunaDraw is a child-centric drawing application (ages 3-8) built with .NET MAUI targeting Windows, Android, iOS, and MacCatalyst. The app features 24+ magical brush effects, shape tools, stamps, undo/redo, layer management, and "Movie Mode" time-lapse replay.

**Important Context:**

- The codebase is heavily AI-generated ("vibe-coded") and can be fragile in places
- Some canvas functionality was migrated from a working app in `\Legacy\SurfaceBurnCalc`
- The app is in active development with missing features tracked in `Documentation/MissingFeatures.md`

## Build & Development Commands

### Building

```bash
# Build the project (Windows target)
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0

# Build for specific platform
dotnet build LunaDraw.csproj -f net10.0-android36.0
dotnet build LunaDraw.csproj -f net10.0-ios26.0
dotnet build LunaDraw.csproj -f net10.0-maccatalyst26.0
```

### Testing

```bash
# Run all tests
dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj

# Run specific test
dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### Running

- Use Visual Studio 2022 or VS Code with C# Dev Kit and .NET MAUI extensions
- Select target framework (e.g., `net10.0-windows10.0.19041.0`)
- Build and Run through IDE

## Architecture

### Core Technologies

- **.NET MAUI** - Cross-platform UI framework
- **SkiaSharp** - All vector graphics and rendering (primary graphics engine)
- **ReactiveUI** - MVVM framework and state management using observables
- **CommunityToolkit.Maui** - Extended MAUI controls and utilities

### Architectural Patterns

#### MVVM with ReactiveUI

- ViewModels inherit from `ReactiveObject` for property change notifications
- Use `this.RaiseAndSetIfChanged(ref field, value)` for reactive properties
- Leverage reactive subscriptions for messaging and state changes

#### Messaging & Communication

- **MessageBus** (ReactiveUI's `IMessageBus`): Use sparingly for loosely-coupled broadcast messages between disconnected components
- **Reactive Observables**: Preferred approach for component communication where possible
- **Command/Event Pattern**: Fallback when reactive approaches don't fit
- MessageBus is instance-based (injected via DI), NOT static for testability

#### Dependency Injection

Services registered in `MauiProgram.cs`:

- **Core State**: `IMessageBus`, `NavigationModel`, `SelectionObserver`, `ILayerFacade`
- **Logic Services**: `ICanvasInputHandler`, `ClipboardMemento`, `IBitmapCache`, `IPreferencesFacade`, `IDrawingStorageMomento`
- **ViewModels**: Singleton or Transient as appropriate
- **Pages**: Typically Transient

### Key Architectural Components

#### Drawing Model

- **IDrawableElement**: Base interface for all drawable objects (paths, shapes, stamps, images)

  - Supports selection, transforms, visibility, layering (ZIndex), opacity, fill/stroke
  - Each element has `Bounds`, `TransformMatrix`, and methods: `Draw()`, `HitTest()`, `Clone()`, `Translate()`, `Transform()`
  - Concrete implementations: `DrawablePath`, `DrawableEllipse`, `DrawableRectangle`, `DrawableLine`, `DrawableImage`, `DrawableStamps`, `DrawableGroup`

- **Layer**: Container for drawable elements with ReactiveUI observable collections

  - Uses **QuadTree spatial indexing** (`QuadTreeMemento<IDrawableElement>`) for efficient spatial queries and rendering
  - No bitmap tiling - renders directly from vector elements
  - Auto-assigns ZIndex to new elements to maintain draw order
  - Supports masking modes, visibility, locking

- **ILayerFacade**: Abstraction for layer management operations
  - Manages `ObservableCollection<Layer>` and current layer state
  - Integrates with `HistoryMemento` for undo/redo
  - Methods: `AddLayer()`, `RemoveLayer()`, `MoveLayer()`, `MoveElementsToLayer()`, `SaveState()`

#### Tool System

- **IDrawingTool**: Interface for all drawing/editing tools
- Tool implementations: `FreehandTool`, `EraserTool`, `EraserBrushTool`, `FillTool`, `SelectTool`, `LineTool`, `RectangleTool`, `EllipseTool`, `ShapeTool`
- Tools receive `ToolContext` with canvas state, navigation, layers, and brush settings
- Input handling delegated to `CanvasInputHandler` which dispatches to active tool

#### View & Viewport Management

- **NavigationModel**: Manages pan/zoom transformations via `ViewMatrix` (SKMatrix)
- **CanvasInputHandler**: Central touch/mouse input processor
  - Handles multi-touch gestures (pan, zoom, rotate) on canvas and selection
  - Delegates single-touch drawing to active tool
  - Right-click switches to Select tool
  - Applies smoothing to gestures for fluid interaction
- **MainPage**: Primary page hosting `SKCanvasView` for rendering
  - Subscribes to `CanvasInvalidateMessage` to trigger redraws
  - Manages context menus and flyout panels (brushes, shapes, settings)

#### State Management & History

- **HistoryMemento**: Undo/redo stack for layer states
- **ClipboardMemento**: Copy/paste buffer for drawable elements
- **DrawingStorageMomento**: Serialization/deserialization of drawings to file
- **QuadTreeMemento<T>**: Generic spatial partitioning for efficient hit-testing and culling

## Code Quality & Testing Standards

### Testing with xUnit, Moq

- **Test Format**: Arrange-Act-Assert (AAA)
  - If no `// Arrange` needed, start with `// Act`
- **Naming**: `Should_When_Returns` format
  - Example: `Should_Set_Sliding_Issued_At_Time_When_Valid_Credentials_Expired_Or_Invalid_Returns_Logout`
- **Test Instances**: Use class name for instance, NOT 'sut' or arbitrary names
  - Mocks: `mockClassName` (e.g., `mockLayerFacade`)
- **Assertions**: One assertion per line
- **Test Types**: Prefer `[Theory]`, `[InlineData]`, `[MemberData]` over multiple `[Fact]` methods. Include negative test cases

### Bug Fixing Workflow

1. **Write test first** to validate the bug exists
2. Implement the fix
3. Run test to confirm bug elimination

### Code Style Rules (from .clinerules)

- **NO underscores** in names
- **NO regions**
- **NO abbreviations** in variable names or otherwise (use full descriptive names)
- **NO legacy or duplicate code** - refactor to clean state, remove obsolete code
- **Static extensions**: Use ONLY for reusable logic (see `Logic/Extensions/`)

### SOLID & Design Principles

Ensure adherence to:

- Single Responsibility Principle (SRP)
- Open/Closed Principle (OCP)
- Liskov Substitution Principle (LSP)
- Interface Segregation Principle (ISP)
- Dependency Inversion Principle (DIP)
- DRY (Don't Repeat Yourself)
- Low Coupling / High Cohesion
- Separation of Concerns & Modularity

## Project Structure

```
LunaDraw/
├── Components/           # Reusable UI components and controls
│   ├── Carousel/        # Gallery carousel implementation
│   ├── *FlyoutPanel.xaml # Brush, shape, settings panels
│   └── *.cs             # Custom controls (BrushPreview, ShapePreview, etc.)
├── Converters/          # XAML value converters
├── Documentation/       # Architecture, features, missing features
├── Logic/               # Core business logic (non-UI)
│   ├── Constants/       # App-wide constants
│   ├── Extensions/      # Static extension methods (SkiaSharp, Preferences)
│   ├── Handlers/        # Input handling (CanvasInputHandler)
│   ├── Messages/        # MessageBus message types
│   ├── Models/          # Domain models (IDrawableElement, Layer, ToolContext, etc.)
│   ├── Tools/           # IDrawingTool implementations
│   ├── Utils/           # Utilities (LayerFacade, Mementos, BitmapCache, etc.)
│   └── ViewModels/      # ReactiveUI ViewModels
├── Pages/               # MAUI pages (MainPage)
├── Platforms/           # Platform-specific code
├── Resources/           # Images, fonts, splash, raw assets
├── tests/               # Unit tests
│   └── LunaDraw.Tests/
└── MauiProgram.cs       # DI registration and app configuration
```

## Important Technical Notes

### SkiaSharp Rendering

- All graphics rendered via SkiaSharp (`SKCanvas`, `SKPaint`, `SKPath`)
- `MainPage.OnCanvasViewPaintSurface`: Main rendering loop
  - Applies `NavigationModel.ViewMatrix` for pan/zoom
  - Iterates layers, uses QuadTree to cull off-screen elements
  - Elements sorted by ZIndex before drawing

### Brush Effects

- 24+ brush effects with custom shaders and blending modes
- Examples: Glow/Neon (additive blending, bloom), Star Sparkles, Rainbow, Fireworks, Crayon, Spray, Ribbon
- Brush settings stored in `ToolbarViewModel` and passed via `ToolContext`

### Movie Mode (Time-Lapse)

- Records drawing process in background
- Playback animates creation of drawing

### Child-Friendly UX Requirements

- Large touch targets (min 2cm x 2cm)
- Multi-sensory feedback (sounds, animations)
- Icon-driven, minimal text
- Visual/audio guidance over explicit instructions

## Legacy & Migration Notes

- Canvas functionality migrated from `\Legacy\SurfaceBurnCalc` (previous working app)
- Current branch `reactive-carousel-v2` is refactoring carousel infrastructure from SBC to MAUI
- Code is fragile in places due to AI generation - test thoroughly

## Additional Resources

- **README.md**: Project overview, features, screenshots, setup
- **Documentation/ArchitectureDesign.md**: Detailed architecture and design requirements
- **Documentation/Features.md**: Feature specifications
- **Documentation/MissingFeatures.md**: Pending features and known issues
- **.clinerules/**: Coding standards and SPARC methodology guidelines
