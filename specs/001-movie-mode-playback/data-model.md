# Data Model: Movie Mode (Playback)

## New/Modified Entities

### 1. IDrawableElement (Modification)

Add timestamp to track creation order for playback reconstruction.

```csharp
public interface IDrawableElement
{
    // ... existing properties ...

    /// <summary>
    /// Timestamp when the element was created.
    /// Used for "Movie Mode" playback to reconstruct creation order.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }
}
```

### 2. PlaybackSpeed (Enum)

Defines the user-selectable speed presets.

```csharp
public enum PlaybackSpeed
{
    Slow,   // e.g., 500ms per element
    Quick,  // e.g., 100ms per element
    Fast    // e.g., 20ms per element
}
```

### 3. PlaybackState (Enum)

Tracks the current state of the playback engine.

```csharp
public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
    Completed
}
```

### 4. PlaybackSettings (Value Object / ViewModel Property)

Configuration for the playback session.

```csharp
public class PlaybackSettings
{
    public PlaybackSpeed Speed { get; set; } = PlaybackSpeed.Quick;
    public bool AutoLoop { get; set; } = false; // Potential future feature
}
```

## Storage Format (JSON)

No structural change to the file format is strictly necessary if `IDrawableElement` serialization includes the new `CreatedAt` property. The `Drawing` file (likely a JSON array of layers/elements) will simply include this new field.

```json
{
  "layers": [
    {
      "id": "...",
      "elements": [
        {
          "type": "DrawablePath",
          "id": "...",
          "createdAt": "2025-12-23T10:00:00Z",  // <--- NEW
          "points": [...]
        }
      ]
    }
  ]
}
```
