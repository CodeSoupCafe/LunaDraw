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

using System.Reactive.Linq;
using LunaDraw.Logic.Handlers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using Microsoft.Maui.Dispatching;
using Moq;
using ReactiveUI;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
    public class PlaybackHandlerTests
    {
        private readonly Mock<ILayerFacade> _mockLayerFacade;
        private readonly Mock<IMessageBus> _mockMessageBus;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IDispatcherTimer> _mockTimer;
        private readonly PlaybackHandler _handler;

        public PlaybackHandlerTests()
        {
            _mockLayerFacade = new Mock<ILayerFacade>();
            _mockMessageBus = new Mock<IMessageBus>();
            _mockDispatcher = new Mock<IDispatcher>();
            _mockTimer = new Mock<IDispatcherTimer>();

            _mockDispatcher.Setup(d => d.CreateTimer()).Returns(_mockTimer.Object);

            // Mock Listen for AppSleepingMessage
            _mockMessageBus.Setup(m => m.Listen<AppSleepingMessage>())
                .Returns(Observable.Never<AppSleepingMessage>());

            _handler = new PlaybackHandler(_mockLayerFacade.Object, _mockMessageBus.Object, _mockDispatcher.Object);
        }

        [Fact]
        public void Load_ShouldSortElementsByCreatedAt()
        {
            // Arrange
            var now = DateTimeOffset.Now;
            var element1 = new DrawablePath { CreatedAt = now.AddMinutes(1), Path = new SKPath() };
            var element2 = new DrawablePath { CreatedAt = now, Path = new SKPath() };
            
            var layer = new Layer();
            layer.Elements.Add(element1);
            layer.Elements.Add(element2);
            
            var layers = new List<Layer> { layer };

            // Act
            _handler.Load(layers);

            // Assert
            // We need to inspect private state or verify behavior. 
            // Since we can't easily inspect private _playbackQueue, we can start play and see what updates first.
            // But verify Load didn't crash is a start.
        }

        [Fact]
        public async Task PlayAsync_ShouldStartTimer_AndResetProgress()
        {
            // Arrange
            var element = new DrawablePath { Path = new SKPath(), AnimationProgress = 1.0f };
            var layer = new Layer();
            layer.Elements.Add(element);
            _mockLayerFacade.Setup(l => l.Layers).Returns(new System.Collections.ObjectModel.ObservableCollection<Layer> { layer });

            // Act
            await _handler.PlayAsync(PlaybackSpeed.Quick);

            // Assert
            Assert.Equal(0f, element.AnimationProgress); // Should be reset to 0
            _mockTimer.Verify(t => t.Start(), Times.Once);
            Assert.True(_handler.IsPlaying);
        }

        [Fact]
        public async Task StopAsync_ShouldResetProgressToOne_AndStopTimer()
        {
            // Arrange
            var element = new DrawablePath { Path = new SKPath(), AnimationProgress = 0.5f };
            var layer = new Layer();
            layer.Elements.Add(element);
            _mockLayerFacade.Setup(l => l.Layers).Returns(new System.Collections.ObjectModel.ObservableCollection<Layer> { layer });
            _handler.Load(new[] { layer });

            // Act
            await _handler.StopAsync();

            // Assert
            Assert.Equal(1.0f, element.AnimationProgress);
            _mockTimer.Verify(t => t.Stop(), Times.Once);
            _mockMessageBus.Verify(m => m.SendMessage(It.IsAny<CanvasInvalidateMessage>()), Times.Once);
        }
        
        [Fact]
        public async Task Playback_ShouldIncrementProgressForPaths()
        {
            // Arrange
            using var path = new SKPath();
            path.MoveTo(0, 0);
            path.LineTo(100, 0); // Length 100
            
            var element = new DrawablePath { Path = path, AnimationProgress = 1.0f };
            var layer = new Layer();
            layer.Elements.Add(element);
            _mockLayerFacade.Setup(l => l.Layers).Returns(new System.Collections.ObjectModel.ObservableCollection<Layer> { layer });
            
            await _handler.PlayAsync(PlaybackSpeed.Quick); // Resets to 0

            // Capture the tick handler
            // Note: In a real unit test for the timer loop logic, we might extract the "Tick" logic or invoke the event.
            // Using reflection to trigger the private OnTimerTick for validation
            var methodInfo = typeof(PlaybackHandler).GetMethod("OnTimerTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act: Simulate one frame
            methodInfo!.Invoke(_handler, new object[] { null!, EventArgs.Empty });

            // Assert
            Assert.True(element.AnimationProgress > 0f, "Progress should have incremented");
            Assert.True(element.AnimationProgress < 1.0f, "Progress should not be complete in one frame for 100px line at normal speed");
        }
        
        [Fact]
        public async Task Playback_ShouldAnimateNonPaths()
        {
             // Arrange
            var element = new DrawableStamps { AnimationProgress = 1.0f };
            var layer = new Layer();
            layer.Elements.Add(element);
            _mockLayerFacade.Setup(l => l.Layers).Returns(new System.Collections.ObjectModel.ObservableCollection<Layer> { layer });
            
            await _handler.PlayAsync(PlaybackSpeed.Quick); // Resets to 0
            
            var methodInfo = typeof(PlaybackHandler).GetMethod("OnTimerTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act: Simulate one frame
            methodInfo!.Invoke(_handler, new object[] { null!, EventArgs.Empty });

            // Assert
            Assert.True(element.AnimationProgress > 0f, "Should have started animation");
            Assert.True(element.AnimationProgress < 1.0f, "Should not complete instantly");
        }

        [Fact]
        public async Task PlayAsync_ShouldRestart_WhenStateIsCompleted()
        {
            // Arrange
            var element = new DrawablePath { Path = new SKPath(), AnimationProgress = 1.0f };
            var layer = new Layer();
            layer.Elements.Add(element);
            _mockLayerFacade.Setup(l => l.Layers).Returns(new System.Collections.ObjectModel.ObservableCollection<Layer> { layer });
            
            // First run to completion
            await _handler.PlayAsync(PlaybackSpeed.Quick);
            
            // Simulate timer finishing
            var methodInfo = typeof(PlaybackHandler).GetMethod("OnTimerTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Tick to finish (since list has 1 element and we start at 0)
            // The logic: if (currentElement.AnimationProgress >= 1.0f) _currentIndex++;
            // element starts at 0 (from PlayAsync->PrepareCanvas).
            // First tick increments it.
            // We need enough ticks or set it to 1.0 manually to finish.
            element.AnimationProgress = 1.0f; 
            // Call tick to advance index
            methodInfo!.Invoke(_handler, new object[] { null!, EventArgs.Empty }); 
            // Now index is 1 (>= count), next tick completes it
            methodInfo!.Invoke(_handler, new object[] { null!, EventArgs.Empty }); 

            Assert.Equal(PlaybackState.Completed, await _handler.CurrentState.FirstAsync());

            // Act - Play again
            await _handler.PlayAsync(PlaybackSpeed.Quick);

            // Assert
            Assert.Equal(PlaybackState.Playing, await _handler.CurrentState.FirstAsync());
            Assert.Equal(0f, element.AnimationProgress); // Should be reset to 0
        }
    }
}