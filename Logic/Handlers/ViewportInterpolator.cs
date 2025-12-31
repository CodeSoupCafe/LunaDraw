using SkiaSharp;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Handlers;

/// <summary>
/// Provides interpolation between viewport snapshots for smooth transitions.
/// </summary>
public static class ViewportInterpolator
{
  /// <summary>
  /// Linearly interpolates between two viewport snapshots.
  /// </summary>
  /// <param name="from">Starting viewport state.</param>
  /// <param name="to">Ending viewport state.</param>
  /// <param name="progress">Interpolation progress (0.0 to 1.0).</param>
  /// <returns>Interpolated viewport snapshot.</returns>
  public static ViewportSnapshot Lerp(ViewportSnapshot from, ViewportSnapshot to, float progress)
  {
    // Clamp progress to [0, 1]
    progress = Math.Clamp(progress, 0f, 1f);

    // Interpolate matrix components directly
    var interpolatedMatrix = new SKMatrix
    {
      ScaleX = from.ViewMatrix.ScaleX + (to.ViewMatrix.ScaleX - from.ViewMatrix.ScaleX) * progress,
      SkewX = from.ViewMatrix.SkewX + (to.ViewMatrix.SkewX - from.ViewMatrix.SkewX) * progress,
      TransX = from.ViewMatrix.TransX + (to.ViewMatrix.TransX - from.ViewMatrix.TransX) * progress,
      SkewY = from.ViewMatrix.SkewY + (to.ViewMatrix.SkewY - from.ViewMatrix.SkewY) * progress,
      ScaleY = from.ViewMatrix.ScaleY + (to.ViewMatrix.ScaleY - from.ViewMatrix.ScaleY) * progress,
      TransY = from.ViewMatrix.TransY + (to.ViewMatrix.TransY - from.ViewMatrix.TransY) * progress,
      Persp0 = from.ViewMatrix.Persp0 + (to.ViewMatrix.Persp0 - from.ViewMatrix.Persp0) * progress,
      Persp1 = from.ViewMatrix.Persp1 + (to.ViewMatrix.Persp1 - from.ViewMatrix.Persp1) * progress,
      Persp2 = from.ViewMatrix.Persp2 + (to.ViewMatrix.Persp2 - from.ViewMatrix.Persp2) * progress
    };

    return new ViewportSnapshot
    {
      ViewMatrix = interpolatedMatrix,
      Timestamp = DateTimeOffset.UtcNow
    };
  }

}
