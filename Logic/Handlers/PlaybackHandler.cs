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
  private readonly BehaviorSubject<PlaybackState> currentState = new(PlaybackState.Stopped);
  private readonly ILayerFacade layerFacade;
  private readonly IMessageBus messageBus;
  private readonly IDispatcherTimer timer;

  private List<IDrawableElement> playbackQueue = new();
  private int currentIndex = 0;

  // Playback configuration
  private float playbackSpeedMultiplier = 1.0f;
  private const float BaseSpeedPixelsPerSecond = 200f; // Speed for paths (Reduced from 500)
  private const float FrameTimeSeconds = 0.016f; // ~60 FPS

  public IObservable<PlaybackState> CurrentState => currentState;
  public bool IsPlaying => currentState.Value == PlaybackState.Playing;

  public PlaybackHandler(ILayerFacade layerFacade, IMessageBus messageBus, IDispatcher? dispatcher = null)
  {
    this.layerFacade = layerFacade;
    this.messageBus = messageBus;

    var effectiveDispatcher = dispatcher ?? Dispatcher.GetForCurrentThread();
    timer = effectiveDispatcher!.CreateTimer();
    timer.Interval = TimeSpan.FromSeconds(FrameTimeSeconds);
    timer.Tick += OnTimerTick;

    messageBus.Listen<AppSleepingMessage>().Subscribe(async _ => await PauseAsync());
  }


  public void Load(IEnumerable<Layer> layers)
  {
    if (layers == null) return;

    // Flatten all elements from all layers
    // Sort by CreatedAt, fallback to ZIndex/List Order for legacy support
    playbackQueue = layers
        .SelectMany(l => l.Elements)
        .OrderBy(e => e.CreatedAt)
        .ThenBy(e => e.ZIndex)
        .ToList();

    currentIndex = 0;
    currentState.OnNext(PlaybackState.Stopped);
  }

  public async Task PlayAsync(PlaybackSpeed speed)
  {
    // If we are not paused (i.e., Stopped or Completed), we should treat this as a fresh start.
    // We reload the layers to ensure we have the latest drawing state (handling new drawings, undos, etc.)
    if (currentState.Value != PlaybackState.Paused)
    {
      Load(layerFacade.Layers);
      PrepareCanvasForPlayback();
    }

    if (playbackQueue.Count == 0) return;

    SetPlaybackSpeed(speed);

    timer.Start();

    currentState.OnNext(PlaybackState.Playing);
    await Task.CompletedTask;
  }

  public async Task PauseAsync()
  {
    timer.Stop();
    currentState.OnNext(PlaybackState.Paused);
    await Task.CompletedTask;
  }

  public async Task StopAsync()
  {
    timer.Stop();
    currentIndex = 0;

    RestoreFullDrawing();

    currentState.OnNext(PlaybackState.Stopped);
    await Task.CompletedTask;
  }

  private void SetPlaybackSpeed(PlaybackSpeed speed)
  {
    playbackSpeedMultiplier = speed switch
    {
      PlaybackSpeed.Slow => 0.5f,
      PlaybackSpeed.Quick => 2.0f,
      PlaybackSpeed.Fast => 5.0f,
      _ => 1.0f
    };
  }

  private void PrepareCanvasForPlayback()
  {
    // Reset elements to invisible/start state
    // All elements start invisible and either animate in or pop in
    foreach (var element in playbackQueue)
    {
      element.AnimationProgress = 0f;
    }

    currentIndex = 0;
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private void RestoreFullDrawing()
  {
    // Ensure all elements are fully visible
    foreach (var element in playbackQueue)
    {
      element.AnimationProgress = 1.0f;
    }
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private async void OnTimerTick(object? sender, EventArgs e)
  {
    if (currentIndex >= playbackQueue.Count)
    {
      await StopAsync();
      currentState.OnNext(PlaybackState.Completed);
      return;
    }

    var currentElement = playbackQueue[currentIndex];

    // Identify if this element should be DRAWN or if it should just POP-IN
    // DRAWN: Paths, Stamps (User strokes)
    // POP-IN: Rectangles, Ellipses, Images, Groups, Lines
    bool shouldDraw = currentElement is DrawablePath ||
                      currentElement is DrawableStamps;

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
          float speedPxPerSec = BaseSpeedPixelsPerSecond * playbackSpeedMultiplier;
          float calcDuration = length / speedPxPerSec;
          targetDuration = Math.Max(calcDuration, 0.1f);
        }
      }
      else if (currentElement is DrawableStamps stamps)
      {
        // Stamps also take time to draw
        targetDuration = 0.5f / playbackSpeedMultiplier;
      }

      float increment = FrameTimeSeconds / targetDuration;
      currentElement.AnimationProgress += increment;

      if (currentElement.AnimationProgress >= 1.0f)
      {
        currentElement.AnimationProgress = 1.0f;
        currentIndex++;
      }
    }
    else
    {
      // POP-IN: Just show it immediately and move to next item in queue
      currentElement.AnimationProgress = 1.0f;
      currentIndex++;
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }
}
