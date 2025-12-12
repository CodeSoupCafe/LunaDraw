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

using SkiaSharp;

namespace LunaDraw.Logic.Models;

/// <summary>
/// Base interface for all drawable elements on the canvas.
/// Supports selection, visibility, layering, and manipulation.
/// </summary>
public interface IDrawableElement
{
  /// <summary>
  /// Unique identifier for this element.
  /// </summary>
  Guid Id { get; }

  /// <summary>
  /// Bounding rectangle of the element in world coordinates.
  /// </summary>
  SKRect Bounds { get; }

  /// <summary>
  /// The transformation matrix applied to the element.
  /// </summary>
  SKMatrix TransformMatrix { get; set; }

  /// <summary>
  /// Whether the element is visible on the canvas.
  /// </summary>
  bool IsVisible { get; set; }

  /// <summary>
  /// Whether the element is currently selected.
  /// </summary>
  bool IsSelected { get; set; }

  /// <summary>
  /// Z-index for layering (higher values drawn on top).
  /// </summary>
  int ZIndex { get; set; }

  /// <summary>
  /// Opacity of the element (0-255).
  /// </summary>
  byte Opacity { get; set; }

  /// <summary>
  /// Fill color for the element (null for no fill).
  /// </summary>
  SKColor? FillColor { get; set; }

  /// <summary>
  /// Stroke/border color for the element.
  /// </summary>
  SKColor StrokeColor { get; set; }

  /// <summary>
  /// Width of the stroke/border.
  /// </summary>
  float StrokeWidth { get; set; }
  bool IsGlowEnabled { get; set; }
  SKColor GlowColor { get; set; }
  float GlowRadius { get; set; }

  /// <summary>
  /// Draws the element on the provided canvas.
  /// </summary>
  /// <param name="canvas">The SKCanvas to draw on.</param>
  void Draw(SKCanvas canvas);

  /// <summary>
  /// Tests if a point hits this element.
  /// </summary>
  /// <param name="point">The point to test in world coordinates.</param>
  /// <returns>True if the point intersects with the element.</returns>
  bool HitTest(SKPoint point);

  /// <summary>
  /// Creates a deep copy of this element.
  /// </summary>
  /// <returns>A cloned instance of the element.</returns>
  IDrawableElement Clone();

  /// <summary>
  /// Translates the element by the specified offset.
  /// </summary>
  /// <param name="offset">The offset to move by.</param>
  void Translate(SKPoint offset);

  /// <summary>
  /// Transforms the element using the provided matrix.
  /// </summary>
  /// <param name="matrix">The transformation matrix.</param>
  void Transform(SKMatrix matrix);

  /// <summary>
  /// Gets the geometric path of the element in world coordinates.
  /// </summary>
  /// <returns>The SKPath representing the element.</returns>
  SKPath GetPath();

  /// <summary>
  /// Gets the underlying geometry path without stroke expansion.
  /// Used for operations that need the base shape.
  /// </summary>
  /// <returns>The SKPath representing the base geometry.</returns>
  SKPath GetGeometryPath();
}

}
