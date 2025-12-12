using System.Collections.Generic;
using System.Linq;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
using ReactiveUI;
using Xunit;
using SkiaSharp;
using Moq;

namespace LunaDraw.Tests
{
    public class EraserBrushToolStampsTests
    {
        private readonly Mock<IMessageBus> mockBus;

        public EraserBrushToolStampsTests()
        {
            mockBus = new Mock<IMessageBus>();
        }

        [Fact]
        public void ErasingPartofStamps_PreservesHueJitterAndType()
        {
            // Arrange
            var stamps = new DrawableStamps
            {
                Points = new List<SKPoint> 
                { 
                    new SKPoint(100, 100), // To be erased
                    new SKPoint(200, 100)  // To remain
                },
                Size = 20,
                HueJitter = 0.5f,
                IsVisible = true,
                Shape = BrushShape.Circle()
            };

            var layer = new Layer();
            layer.Elements.Add(stamps);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { stamps },
                StrokeWidth = 30, // Eraser larger than stamp (20)
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Circle()
            };

            var tool = new EraserBrushTool(mockBus.Object);

            // Act
            // Erase over the first point (100, 100)
            tool.OnTouchPressed(new SKPoint(100, 100), context);
            tool.OnTouchReleased(new SKPoint(100, 100), context);

            // Assert
            Assert.Single(layer.Elements); // Should still have one element (the modified stamps)

            var result = layer.Elements.First();
            
            // Check type preservation
            Assert.IsType<DrawableStamps>(result);
            var resultStamps = (DrawableStamps)result;

            // Check content
            Assert.Single(resultStamps.Points);
            Assert.Equal(200f, resultStamps.Points[0].X); // The second point should remain
            
            // Check property preservation
            Assert.Equal(0.5f, resultStamps.HueJitter);
        }

        [Fact]
        public void ErasingPartOfSingleStamp_CreatesFragmentWithCorrectColor()
        {
            // Arrange
            // Create a single stamp that will be partially erased.
            // We need to force a known jitter or disable random to be deterministic, 
            // but since we can't easily inject the seed into DrawableStamps without refactoring,
            // we will use IsRainbowEnabled which is deterministic (based on index).
            
            var stamps = new DrawableStamps
            {
                Points = new List<SKPoint> { new SKPoint(100, 100) }, // One point
                Size = 50,
                IsRainbowEnabled = true, // Deterministic color: Index 0 -> Hue 0 (Red)
                IsVisible = true,
                Shape = BrushShape.Square() // Square is easier to intersect
            };

            // Expected color for Index 0 with Rainbow: Hue 0, Sat 100, Light 50 -> Red
            var expectedColor = SKColor.FromHsl(0, 100, 50);

            var layer = new Layer();
            layer.Elements.Add(stamps);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { stamps },
                StrokeWidth = 10,
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Square()
            };

            var tool = new EraserBrushTool(mockBus.Object);

            // Act
            // Erase a corner of the square (100,100 is center. Size 50 -> 75 to 125.
            // Let's erase at 75, 75 (top left corner area)
            tool.OnTouchPressed(new SKPoint(75, 75), context);
            tool.OnTouchReleased(new SKPoint(75, 75), context);

            // Assert
            Assert.Single(layer.Elements); // Should have one element (the fragment)

            var result = layer.Elements.First();
            
            // It should be converted to a DrawablePath because it was modified
            Assert.IsType<DrawablePath>(result);
            var resultPath = (DrawablePath)result;

            // Check that the color is preserved (Red)
            // Note: In our implementation, we set StrokeColor. 
            Assert.Equal(expectedColor, resultPath.StrokeColor);
            
            // Verify it's filled (as per our logic for eroded strokes/stamps)
            Assert.True(resultPath.IsFilled);
        }

        [Fact]
        public void ErasingStampWithFlow_CreatesFragmentWithCombinedOpacity()
        {
            // Arrange
            var stamps = new DrawableStamps
            {
                Points = new List<SKPoint> { new SKPoint(100, 100) },
                Size = 50,
                Opacity = 255,
                Flow = 128, // ~50%
                IsVisible = true,
                Shape = BrushShape.Square()
            };

            var layer = new Layer();
            layer.Elements.Add(stamps);

            var context = new ToolContext
            {
                CurrentLayer = layer,
                AllElements = new List<IDrawableElement> { stamps },
                StrokeWidth = 10,
                SelectionObserver = new SelectionObserver(),
                BrushShape = BrushShape.Square()
            };

            var tool = new EraserBrushTool(mockBus.Object);

            // Act
            tool.OnTouchPressed(new SKPoint(75, 75), context);
            tool.OnTouchReleased(new SKPoint(75, 75), context);

            // Assert
            var result = layer.Elements.First() as DrawablePath;
            Assert.NotNull(result);
            
            // Expected: 255 * 128 / 255 = 128
            Assert.Equal(128, result.Opacity);
        }
    }
}
