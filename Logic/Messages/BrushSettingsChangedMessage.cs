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

namespace LunaDraw.Logic.Messages;

  /// <summary>
  /// Message sent when brush settings (color, transparency) change.
  /// </summary>
  public class BrushSettingsChangedMessage(
      SKColor? strokeColor = null,
      SKColor? fillColor = null,
      byte? transparency = null,
      byte? flow = null,
      float? spacing = null,
      float? strokeWidth = null,
      bool? isGlowEnabled = null,
      SKColor? glowColor = null,
      float? glowRadius = null,
      bool? isRainbowEnabled = null,
      float? scatterRadius = null,
      float? sizeJitter = null,
      float? angleJitter = null,
      float? hueJitter = null,
      bool shouldClearFillColor = false)
  {
      public SKColor? StrokeColor { get; } = strokeColor;
      public SKColor? FillColor { get; } = fillColor;
      public byte? Transparency { get; } = transparency;
      public byte? Flow { get; } = flow;
      public float? Spacing { get; } = spacing;
      public float? StrokeWidth { get; } = strokeWidth;
      public bool? IsGlowEnabled { get; } = isGlowEnabled;
      public SKColor? GlowColor { get; } = glowColor;
      public float? GlowRadius { get; } = glowRadius;
      public bool? IsRainbowEnabled { get; } = isRainbowEnabled;
      public float? ScatterRadius { get; } = scatterRadius;
      public float? SizeJitter { get; } = sizeJitter;
      public float? AngleJitter { get; } = angleJitter;
      public float? HueJitter { get; } = hueJitter;
      public bool ShouldClearFillColor { get; } = shouldClearFillColor;
  }
