using SkiaSharp;

namespace LunaDraw.Logic.Models;

/// <summary>
/// Immutable snapshot of viewport state at a moment in time.
/// Used for Movie Mode playback to reconstruct viewport transformations.
/// </summary>
public readonly struct ViewportSnapshot
{
    /// <summary>
    /// The view transformation matrix (pan, zoom, rotation).
    /// </summary>
    public SKMatrix ViewMatrix { get; init; }

    /// <summary>
    /// Timestamp when this snapshot was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Creates a snapshot from the current NavigationModel state.
    /// </summary>
    public static ViewportSnapshot FromNavigationModel(NavigationModel navigationModel)
    {
        return new ViewportSnapshot
        {
            ViewMatrix = navigationModel.ViewMatrix,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an identity snapshot (no transformation).
    /// </summary>
    public static ViewportSnapshot Identity => new()
    {
        ViewMatrix = SKMatrix.CreateIdentity(),
        Timestamp = DateTimeOffset.MinValue
    };
}
