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

using System.Reactive.Subjects;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Handlers;

public class PlaybackHandler : IPlaybackHandler
{
  private readonly BehaviorSubject<PlaybackState> _currentState = new(PlaybackState.Stopped);
  private readonly ILayerFacade _layerFacade;
  private readonly IMessageBus _messageBus;
  private readonly IDispatcherTimer _timer;

  private List<IDrawableElement> _playbackQueue = new();
  private int _currentIndex = 0;

  // Playback configuration
  private float _playbackSpeedMultiplier = 1.0f;
  private const float BaseSpeedPixelsPerSecond = 200f; // Speed for paths (Reduced from 500)
  private const float FrameTimeSeconds = 0.016f; // ~60 FPS

  public IObservable<PlaybackState> CurrentState => _currentState;
  public bool IsPlaying => _currentState.Value == PlaybackState.Playing;

  public PlaybackHandler(ILayerFacade layerFacade, IMessageBus messageBus, IDispatcher? dispatcher = null)
  {
    _layerFacade = layerFacade;
    _messageBus = messageBus;

    var effectiveDispatcher = dispatcher ?? Dispatcher.GetForCurrentThread();
    _timer = effectiveDispatcher!.CreateTimer();
    _timer.Interval = TimeSpan.FromSeconds(FrameTimeSeconds);
    _timer.Tick += OnTimerTick;

    _messageBus.Listen<AppSleepingMessage>().Subscribe(async _ => await PauseAsync());
  }

  // Default constructor for tests if needed
  public PlaybackHandler() : this(null!, null!) { }

  public void Load(IEnumerable<Layer> layers)
  {
    if (layers == null) return;

    // Flatten all elements from all layers
    // Sort by CreatedAt, fallback to ZIndex/List Order for legacy support
    _playbackQueue = layers
        .SelectMany(l => l.Elements)
        .OrderBy(e => e.CreatedAt)
        .ThenBy(e => e.ZIndex)
        .ToList();

    _currentIndex = 0;
    _currentState.OnNext(PlaybackState.Stopped);
  }

  public async Task PlayAsync(PlaybackSpeed speed)
  {
    // If we are not paused (i.e., Stopped or Completed), we should treat this as a fresh start.
    // We reload the layers to ensure we have the latest drawing state (handling new drawings, undos, etc.)
    if (_currentState.Value != PlaybackState.Paused)
    {
      Load(_layerFacade.Layers);
      PrepareCanvasForPlayback();
    }

    if (_playbackQueue.Count == 0) return;

    SetPlaybackSpeed(speed);

    _timer.Start();

    _currentState.OnNext(PlaybackState.Playing);
    await Task.CompletedTask;
  }

  public async Task PauseAsync()
  {
    _timer.Stop();
    _currentState.OnNext(PlaybackState.Paused);
    await Task.CompletedTask;
  }

  public async Task StopAsync()
  {
    _timer.Stop();
    _currentIndex = 0;

    RestoreFullDrawing();

    _currentState.OnNext(PlaybackState.Stopped);
    await Task.CompletedTask;
  }

  private void SetPlaybackSpeed(PlaybackSpeed speed)
  {
    _playbackSpeedMultiplier = speed switch
    {
      PlaybackSpeed.Slow => 0.5f,
      PlaybackSpeed.Quick => 2.0f,
      PlaybackSpeed.Fast => 5.0f,
      _ => 1.0f
    };
  }

  private void PrepareCanvasForPlayback()
  {
    // Reset all elements to invisible/start state
    foreach (var element in _playbackQueue)
    {
      element.AnimationProgress = 0f;
    }

    _currentIndex = 0;
    _messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private void RestoreFullDrawing()
  {
    // Ensure all elements are fully visible
    foreach (var element in _playbackQueue)
    {
      element.AnimationProgress = 1.0f;
    }
    _messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private async void OnTimerTick(object? sender, EventArgs e)
  {
    if (_currentIndex >= _playbackQueue.Count)
    {
      await StopAsync();
      _currentState.OnNext(PlaybackState.Completed);
      return;
    }

    var currentElement = _playbackQueue[_currentIndex];

    // Identify if this element should be DRAWN or if it should just POP-IN
    // DRAWN: Paths, Stamps, Lines (User strokes)
    // POP-IN: Rectangles, Ellipses, Images, Groups
    bool shouldDraw = currentElement is DrawablePath ||
                      currentElement is DrawableStamps ||
                      currentElement is DrawableLine;

    if (shouldDraw)
    {
      float targetDuration = 0.5f; // Default minimum duration (seconds)

      if (currentElement is DrawablePath path && path.Path != null)
      {
        float length = 0;
        try
        {
          using var measure = new SKPathMeasure(path.Path, false, 1.0f);
          length = measure.Length;
        }
        catch { }

        if (length > 0)
        {
          float speedPxPerSec = BaseSpeedPixelsPerSecond * _playbackSpeedMultiplier;
          float calcDuration = length / speedPxPerSec;
          targetDuration = Math.Max(calcDuration, 0.1f);
        }
      }
      else if (currentElement is DrawableStamps stamps)
      {
        // Stamps also take time to draw
        targetDuration = 0.5f / _playbackSpeedMultiplier;
      }
      else if (currentElement is DrawableLine line)
      {
        // Lines also take time to draw
        targetDuration = 0.2f / _playbackSpeedMultiplier;
      }

      float increment = FrameTimeSeconds / targetDuration;
      currentElement.AnimationProgress += increment;

      if (currentElement.AnimationProgress >= 1.0f)
      {
        currentElement.AnimationProgress = 1.0f;
        _currentIndex++;
      }
    }
    else
    {
      // POP-IN: Just show it immediately and move to next item in queue
      currentElement.AnimationProgress = 1.0f;
      _currentIndex++;
    }

    _messageBus.SendMessage(new CanvasInvalidateMessage());
  }
}
