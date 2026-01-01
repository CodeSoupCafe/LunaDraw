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

namespace LunaDraw.Logic.Models;

/// <summary>
/// Represents a preset configuration for drawing tools.
/// Used to differentiate between Brush (smooth painting) and Stamps (discrete placement)
/// while both use the same underlying FreehandTool implementation.
/// </summary>
public class ToolPreset
{
    /// <summary>
    /// Spacing between consecutive stamps/brush dabs (0.1-0.5 for smooth brush, 1.0+ for discrete stamps).
    /// </summary>
    public float Spacing { get; set; }

    /// <summary>
    /// Array of allowed brush shapes for this preset (filtered for Brush, all shapes for Stamps).
    /// </summary>
    public BrushShapeType[] AllowedShapes { get; set; } = Array.Empty<BrushShapeType>();
}
