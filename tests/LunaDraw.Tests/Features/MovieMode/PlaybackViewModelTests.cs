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
using LunaDraw.Logic.Playback;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using Moq;
using Plugin.Maui.ScreenRecording;
using System.Reactive.Subjects;
using Xunit;

namespace LunaDraw.Tests.Features.MovieMode;

public class PlaybackViewModelTests
{
  private readonly Mock<IPlaybackHandler> mockHandler;
  private readonly Mock<IScreenRecording> mockScreenRecording;
  private readonly PlaybackViewModel vm;

  public PlaybackViewModelTests()
  {
    mockHandler = new Mock<IPlaybackHandler>();
    mockScreenRecording = new Mock<IScreenRecording>();
    mockHandler.SetupGet(h => h.CurrentState).Returns(new BehaviorSubject<PlaybackState>(PlaybackState.Stopped));
    vm = new PlaybackViewModel(mockHandler.Object, mockScreenRecording.Object);
  }

  [Fact]
  public void PlayCommand_Should_Call_Handler_Play()
  {
    // Act
    vm.PlayCommand.Execute(System.Reactive.Unit.Default);

    // Assert
    mockHandler.Verify(h => h.PlayAsync(It.IsAny<PlaybackSpeed>()), Times.Once);
  }

  [Fact]
  public void StopCommand_Should_Call_Handler_Stop()
  {
    // Act
    vm.StopCommand.Execute(System.Reactive.Unit.Default);

    // Assert
    mockHandler.Verify(h => h.StopAsync(), Times.Once);
  }
}
