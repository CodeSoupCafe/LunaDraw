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

using FluentAssertions;
using LunaDraw.Logic.Handlers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using Moq;
using ReactiveUI;
using Xunit;

namespace LunaDraw.Tests.Features.MovieMode;

public class PlaybackPerformanceTests
{
  private readonly PlaybackHandler handler;
  private readonly Mock<ILayerFacade> mockLayerFacade;
  private readonly Mock<IMessageBus> mockMessageBus;
  private readonly Mock<IDispatcher> mockDispatcher;
  private readonly Mock<IDispatcherTimer> mockTimer;

  public PlaybackPerformanceTests()
  {
    mockLayerFacade = new Mock<ILayerFacade>();
    mockMessageBus = new Mock<IMessageBus>();
    mockDispatcher = new Mock<IDispatcher>();
    mockTimer = new Mock<IDispatcherTimer>();

    mockMessageBus.Setup(m => m.Listen<AppSleepingMessage>()).Returns(System.Reactive.Linq.Observable.Empty<AppSleepingMessage>());
    mockDispatcher.Setup(d => d.CreateTimer()).Returns(mockTimer.Object);

    handler = new PlaybackHandler(mockLayerFacade.Object, mockMessageBus.Object, mockDispatcher.Object);
  }

  [Fact]
  public void Load_With_Zero_Elements_Should_Not_Throw()
  {
    // Arrange
    var layer = new Layer();
    var layers = new[] { layer };

    // Act
    Action act = () => handler.Load(layers);

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void Load_With_10000_Elements_Should_Be_Fast()
  {
    // Arrange
    var layer = new Layer();
    for (int i = 0; i < 10000; i++)
    {
      layer.Elements.Add(new DrawablePath { CreatedAt = DateTimeOffset.Now.AddMilliseconds(i), Path = null! });
    }
    var layers = new[] { layer };

    // Act
    var watch = System.Diagnostics.Stopwatch.StartNew();
    handler.Load(layers);
    watch.Stop();

    // Assert
    watch.ElapsedMilliseconds.Should().BeLessThan(500); // Sorting 10k items should be sub-500ms
  }
}
