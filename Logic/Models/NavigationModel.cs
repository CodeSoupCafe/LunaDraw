/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Models;

public class NavigationModel : ReactiveObject
{
  private SKMatrix viewMatrix = SKMatrix.CreateIdentity();

  // Single source of truth - this is what gets applied to the canvas
  public SKMatrix ViewMatrix
  {
    get => viewMatrix;
    set => this.RaiseAndSetIfChanged(ref viewMatrix, value);
  }

  private int canvasWidth;
  public int CanvasWidth
  {
    get => canvasWidth;
    set => this.RaiseAndSetIfChanged(ref canvasWidth, value);
  }

  private int canvasHeight;
  public int CanvasHeight
  {
    get => canvasHeight;
    set => this.RaiseAndSetIfChanged(ref canvasHeight, value);
  }

  /// <summary>
  /// Gets the current scale (zoom level) from the ViewMatrix.
  /// A value of 1.0 means 100% (no zoom), 2.0 means 200% (zoomed in 2x), etc.
  /// </summary>
  public float Scale => viewMatrix.ScaleX;

  /// <summary>
  /// Zooms in by increasing the scale by 25%.
  /// </summary>
  public void ZoomIn()
  {
    var newScale = viewMatrix.ScaleX * 1.25f;
    newScale = Math.Min(newScale, 10.0f); // Max 1000% zoom
    ApplyScale(newScale);
  }

  /// <summary>
  /// Zooms out by decreasing the scale by 20%.
  /// </summary>
  public void ZoomOut()
  {
    var newScale = viewMatrix.ScaleX * 0.8f;
    newScale = Math.Max(newScale, 0.1f); // Min 10% zoom
    ApplyScale(newScale);
  }

  /// <summary>
  /// Resets zoom to 100% (scale = 1.0) while preserving pan offset.
  /// </summary>
  public void ResetZoom()
  {
    ApplyScale(1.0f);
  }

  /// <summary>
  /// Applies a new scale to the ViewMatrix while preserving translation.
  /// </summary>
  private void ApplyScale(float newScale)
  {
    var currentTranslateX = viewMatrix.TransX;
    var currentTranslateY = viewMatrix.TransY;

    ViewMatrix = SKMatrix.CreateScaleTranslation(newScale, newScale, currentTranslateX, currentTranslateY);
  }

  public void Reset()
  {
    ViewMatrix = SKMatrix.CreateIdentity();
  }
}
