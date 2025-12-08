# LunaDraw

LunaDraw is a child-centric drawing application designed for children aged 3–8. It provides a safe, ad-free, and magical environment for creativity, featuring special effects brushes, easy-to-use tools, and a "Movie Mode" that replays the drawing process.

> ### **<span style="color:maroon">Missing Features / Parity / Testing / Deployment</span>**
> This application is a work in-progress and has several missing features yet to be implemented. Only Windows has been tested on one device. No mobile testing has been done. As the application is new and the holidays draw closer, it may be another month or two before app store submission.
> 
> The running list can be found below:
>
> - [Missing Features](Documentation/MissingFeatures.md)


> ### **Vibe-Coded**
> The application is heavily vibe-coded and guided by best practices in the [Cline Rules](.clinerules/.clinerules.md) using [SPARC Agentic Development](https://gist.github.com/ruvnet/7d4e1d5c9233ab0a1d2a66bf5ec3e58f); mostly using [Gemini 3](https://gemini.google.com/). Hallucinations are always possible. It is recognized and should be by all that AI is still sometimes as helpful as a sack of hair.
>
> There are some very poor implementations by the AI such as string names of commands.
>
> The code is quite fragile in a lot of places.

## Features

### 🎨 Core Drawing

- **Canvas:** Draw on a blank canvas or import photos to doodle on.
- **Magical Brushes:** A collection of 24+ high-impact brushes including:
  - Glow / Neon
  - Star Sparkles / Glitter
  - Fireworks
  - Rainbow (Color cycling)
  - Crayon, Spray, Ribbon, and more.
- **Tools:** Eraser, Fill Bucket (Pattern Paint), and Shapes (Lines, Rectangles, Ellipses).
- **Undo/Redo:** Universally accessible history navigation.

### 🎬 Movie Mode

- Automatically records the drawing process.
- Playback your art creation as a short animation.

### 🖼️ Art Management

- **Gallery:** Built-in gallery to store and view completed drawings and their animations.

### 👶 Child-Friendly UX

- **Ergonomic Design:** Large targets (2cm x 2cm min) for easy tapping.
- **Visual Feedback:** Immediate multi-sensory feedback (sounds, animations) for actions.
- **Simplicity:** Icon-driven interface with minimal text.
- **Ad-Free & Offline:** Completely free, no ads, and works without internet.

## Architecture

The application is built using **.NET MAUI** targeting Windows, Android, iOS, and MacCatalyst.

- **Graphics Engine:** [SkiaSharp](https://github.com/mono/SkiaSharp) is used for all vector graphics and rendering, ensuring high performance and cross-platform consistency.
- **MVVM Framework:** Uses [ReactiveUI](https://www.reactiveui.net/) for state management and MVVM implementation.
- **Dependency Injection:** Built-in MAUI dependency injection.

## Getting Started

### Prerequisites

- .NET 8.0 or later (Project targets .NET 10.0 preview/nightly builds based on configuration).
- Visual Studio 2022 or VS Code with C# Dev Kit.

### Setting Up VS Code for Deployment

1. Install the latest version of **[Visual Studio Code](https://code.visualstudio.com/)**.
2. Open the LunaDraw project folder in Visual Studio Code.
3. Install the recommended extensions (search in the Extensions view `Ctrl+Shift+X`):
   - **[C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)** (Essential for C# and MAUI development)
   - **[.NET MAUI](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui)** (Provides MAUI tooling and debug support)
4. Follow the official **[.NET MAUI in VS Code Guide](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?tabs=vsc)** to ensure all workloads and prerequisites are correctly installed.

### Building the Project

1. Clone the repository.
2. Open the solution `LunaDraw.sln`.
3. Select your target framework (e.g., `net10.0-windows...`).
4. Build and Run.

## Testing

The solution includes a unit test project `LunaDraw.Tests` using xUnit, Moq, and FluentValidation.

To run tests:

```bash
dotnet test Tests/LunaDraw.Tests/LunaDraw.Tests.csproj
```

## Documentation

For more detailed information, please refer to the `Documentation` directory:

- [Architecture Design](Documentation/ArchitectureDesign.md)
- [Features](Documentation/Features.md)

## Deployment

1. Build the project using Visual Studio or VS Code.
2. Deploy the generated application on the respective platform (Windows, Android, iOS, MacCatalyst).