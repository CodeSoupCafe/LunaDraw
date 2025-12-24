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

using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Messages;
using ReactiveUI;
using Moq;

using SkiaSharp;

namespace LunaDraw.Tests
{
  public class SelectToolInteractionTests
  {
    private readonly Mock<IMessageBus> mockBus = new Mock<IMessageBus>();

    private class MockDrawableElement : IDrawableElement
    {
      public Guid Id { get; init; } = Guid.NewGuid();
      public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
      public SKRect Bounds { get; set; } = new SKRect(0, 0, 100, 100);
      public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();
      public bool IsVisible { get; set; } = true;
      public bool IsSelected { get; set; }
      public int ZIndex { get; set; }
      public byte Opacity { get; set; } = 255;
      public SKColor? FillColor { get; set; } = SKColors.Blue;
      public SKColor StrokeColor { get; set; }
      public float StrokeWidth { get; set; }
      public bool IsGlowEnabled { get; set; }
      public SKColor GlowColor { get; set; }
      public float GlowRadius { get; set; }
      public float AnimationProgress { get; set; } = 1.0f;

      public void Draw(SKCanvas canvas) { }
      public bool HitTest(SKPoint point) => Bounds.Contains(point);

      public IDrawableElement Clone() => this; // Simple clone for testing

      public SKPoint LastTranslation { get; private set; }
      public void Translate(SKPoint offset)
      {
        LastTranslation = offset;
        // Apply translation to matrix to simulate movement for bounds calculation
        TransformMatrix = SKMatrix.Concat(TransformMatrix, SKMatrix.CreateTranslation(offset.X, offset.Y));
      }

      public void Transform(SKMatrix matrix)
      {
        TransformMatrix = matrix;
      }

      public SKPath GetPath() => new SKPath();
      public SKPath GetGeometryPath() => new SKPath();
    }

    [Fact]
    public void Dragging_MovesSelectedElement()
    {
      // Arrange
      var element = new MockDrawableElement();
      var elements = new List<IDrawableElement> { element };
      var selectionObserver = new SelectionObserver();
      var layer = new Layer();
      layer.Elements.Add(element);

      var context = new ToolContext
      {
        CurrentLayer = layer,
        AllElements = elements,
        Layers = new List<Layer> { layer },
        SelectionObserver = selectionObserver,
        BrushShape = BrushShape.Circle()
      };
      var tool = new SelectTool(mockBus.Object);

      // Act
      // 1. Press on the element (10,10 is inside 0,0,100,100)
      tool.OnTouchPressed(new SKPoint(10, 10), context);

      // 2. Move
      tool.OnTouchMoved(new SKPoint(20, 20), context);

      // Assert
      Assert.True(selectionObserver.Contains(element));
      Assert.Equal(new SKPoint(10, 10), element.LastTranslation);
    }

    [Fact]
    public void Resizing_BottomRight_ChangesTransformMatrix()
    {
      // Arrange
      var element = new MockDrawableElement();
      var elements = new List<IDrawableElement> { element };
      var selectionObserver = new SelectionObserver();
      // Pre-select the element so we can hit the handle
      selectionObserver.Add(element);

      var layer = new Layer();
      layer.Elements.Add(element);

      var context = new ToolContext
      {
        CurrentLayer = layer,
        AllElements = elements,
        Layers = new List<Layer> { layer },
        SelectionObserver = selectionObserver,
        BrushShape = BrushShape.Circle()
      };
      var tool = new SelectTool(mockBus.Object);

      // Initial bounds are 0,0 to 100,100. 
      // Bottom Right handle should be near 100,100.
      var handlePoint = new SKPoint(100, 100);

      // Act
      // 1. Press on the Bottom Right handle
      tool.OnTouchPressed(handlePoint, context);

      // 2. Drag it to 150, 150 (increase size by 1.5x)
      tool.OnTouchMoved(new SKPoint(150, 150), context);

      // Assert
      // We expect the matrix to be scaled.
      // Original width=100, New width=150. ScaleX = 1.5.
      // Original height=100, New height=150. ScaleY = 1.5.

      Assert.NotEqual(SKMatrix.CreateIdentity(), element.TransformMatrix);
      Assert.Equal(1.5f, element.TransformMatrix.ScaleX, 2);
      Assert.Equal(1.5f, element.TransformMatrix.ScaleY, 2);
    }
  }
}
