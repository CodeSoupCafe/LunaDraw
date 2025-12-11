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

using System;
using System.Linq;
using System.Reactive.Subjects;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services; // Keep this for ToolStateManager
using LunaDraw.Logic.Tools;
using Moq;
using ReactiveUI; // ADDED: Required for IMessageBus
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
    public class ToolStateManagerTests
    {
        private readonly Mock<IMessageBus> mockBus;
        private readonly Subject<BrushSettingsChangedMessage> brushSettingsSubject;
        private readonly Subject<BrushShapeChangedMessage> brushShapeSubject;
        private readonly ToolStateManager toolStateManager;

        public ToolStateManagerTests()
        {
            mockBus = new Mock<IMessageBus>();
            brushSettingsSubject = new Subject<BrushSettingsChangedMessage>();
            brushShapeSubject = new Subject<BrushShapeChangedMessage>();

            mockBus.Setup(x => x.Listen<BrushSettingsChangedMessage>())
                .Returns(brushSettingsSubject);
            mockBus.Setup(x => x.Listen<BrushShapeChangedMessage>())
                .Returns(brushShapeSubject);

            toolStateManager = new ToolStateManager(mockBus.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var strokeWidth = toolStateManager.StrokeWidth;

            // Assert
            Assert.Equal(40, strokeWidth);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultStrokeColor()
        {
            // Act
            var strokeColor = toolStateManager.StrokeColor;

            // Assert
            Assert.Equal(SKColors.MediumPurple, strokeColor);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultFillColor()
        {
            // Act
            var fillColor = toolStateManager.FillColor;

            // Assert
            Assert.Equal(SKColors.SteelBlue, fillColor);
        }

        [Fact]
        public void Constructor_ShouldInitializeActiveToolToFreehand()
        {
            // Act
            var activeTool = toolStateManager.ActiveTool;

            // Assert
            Assert.IsType<FreehandTool>(activeTool);
        }

        [Fact]
        public void ActiveTool_Set_ShouldRaisePropertyChanged()
        {
            // Arrange
            var newTool = new RectangleTool(mockBus.Object);

            
            // Act
            toolStateManager.ActiveTool = newTool;
            
            // Assert
            // TODO: Replace with ReactiveUI way to test property changes.
            // monitoredSubject.Should().RaisePropertyChangeFor(x => x.ActiveTool);
        }

        [Fact]
        public void ActiveTool_Set_ShouldSendMessage()
        {
            // Arrange
            var newTool = new RectangleTool(mockBus.Object);
            
            // Act
            toolStateManager.ActiveTool = newTool;

            // Assert
            mockBus.Verify(x => x.SendMessage(It.Is<ToolChangedMessage>(msg => msg.NewTool == newTool)), Times.Once); 
        }

        [Fact]
        public void Receive_BrushSettingsChangedMessage_ShouldUpdateStrokeColor()
        {
            // Arrange
            var expectedColor = SKColors.Red;
            
            // Act
            brushSettingsSubject.OnNext(new BrushSettingsChangedMessage(strokeColor: expectedColor)); // FIX HERE

            // Assert
            Assert.Equal(expectedColor, toolStateManager.StrokeColor);
        }

        [Fact]
        public void Receive_BrushSettingsChangedMessage_ShouldUpdateStrokeWidth()
        {
            // Arrange
            var expectedWidth = 15.5f;

            // Act
            brushSettingsSubject.OnNext(new BrushSettingsChangedMessage(strokeWidth: expectedWidth)); // FIX HERE

            // Assert
            Assert.Equal(expectedWidth, toolStateManager.StrokeWidth);
        }

        [Fact]
        public void Receive_BrushSettingsChangedMessage_ShouldUpdateOpacity()
        {
            // Arrange
            byte expectedOpacity = 128;

            // Act
            brushSettingsSubject.OnNext(new BrushSettingsChangedMessage(transparency: expectedOpacity)); // FIX HERE

            // Assert
            Assert.Equal(expectedOpacity, toolStateManager.Opacity);
        }

        [Fact]
        public void Receive_BrushShapeChangedMessage_ShouldUpdateCurrentBrushShape()
        {
            // Arrange
            var expectedShape = BrushShape.Star();

            // Act
            brushShapeSubject.OnNext(new BrushShapeChangedMessage(expectedShape));

            // Assert
            Assert.Equal(expectedShape.Name, toolStateManager.CurrentBrushShape.Name);
            Assert.Equal(expectedShape.Type, toolStateManager.CurrentBrushShape.Type);
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Receive_BrushSettingsChangedMessage_ShouldUpdateGlowEnabled(bool isEnabled)
        {
            // Act
             brushSettingsSubject.OnNext(new BrushSettingsChangedMessage(isGlowEnabled: isEnabled)); // FIX HERE
             
            // Assert
             Assert.Equal(isEnabled, toolStateManager.IsGlowEnabled);
        }
    }
}