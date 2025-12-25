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

using System.Reactive;
using LunaDraw.Logic.Handlers;
using LunaDraw.Logic.Models;
using ReactiveUI;
using Plugin.Maui.ScreenRecording;

namespace LunaDraw.Logic.ViewModels;

public class PlaybackViewModel : ReactiveObject
{
  private readonly IPlaybackHandler playbackHandler;

  private PlaybackSpeed selectedSpeed = PlaybackSpeed.Quick;
  public PlaybackSpeed SelectedSpeed
  {
    get => selectedSpeed;
    set => this.RaiseAndSetIfChanged(ref selectedSpeed, value);
  }

  private PlaybackState currentState;
  public PlaybackState CurrentState
  {
    get => currentState;
    private set => this.RaiseAndSetIfChanged(ref currentState, value);
  }

  public ReactiveCommand<Unit, Unit> PlayCommand { get; }
  public ReactiveCommand<Unit, Unit> PauseCommand { get; }
  public ReactiveCommand<Unit, Unit> StopCommand { get; }
  public ReactiveCommand<Unit, Unit> ExportVideoCommand { get; }

  private readonly IScreenRecording screenRecording;

  public PlaybackViewModel(IPlaybackHandler playbackHandler, IScreenRecording screenRecording)
  {
    this.playbackHandler = playbackHandler;
    this.screenRecording = screenRecording;

    playbackHandler.CurrentState
        .Subscribe(state => CurrentState = state);

    PlayCommand = ReactiveCommand.CreateFromTask(() => playbackHandler.PlayAsync(SelectedSpeed));
    PauseCommand = ReactiveCommand.CreateFromTask(() => playbackHandler.PauseAsync());
    StopCommand = ReactiveCommand.CreateFromTask(() => playbackHandler.StopAsync());
    ExportVideoCommand = ReactiveCommand.CreateFromTask(ExportVideoAsync);
  }

  private async Task ExportVideoAsync()
  {
    try
    {
      // Navigate to PlaybackPage to hide UI
      await Shell.Current.GoToAsync(nameof(Pages.PlaybackPage));
      
      // Give it a moment to render the initial state (blank canvas)
      await Task.Delay(1000);

      // Start screen recording
      var started = await screenRecording.StartRecording();
      if (!started)
      {
        await Shell.Current.GoToAsync(".."); // Go back
        await Shell.Current.CurrentPage.DisplayAlert(
            "Error",
            "Failed to start screen recording. Please check permissions.",
            "OK");
        return;
      }

      // Play the animation
      await playbackHandler.PlayAsync(SelectedSpeed);

      // Wait for playback to complete
      while (CurrentState == PlaybackState.Playing)
      {
        await Task.Delay(100);
      }
      
      // Allow a moment for the final frame to be recorded
      await Task.Delay(1000);

      // Stop recording and save
      var result = await screenRecording.StopRecording();

      // Go back to main page
      await Shell.Current.GoToAsync("..");

      if (result != null && !string.IsNullOrEmpty(result.FullPath))
      {
        await Shell.Current.CurrentPage.DisplayAlert(
            "Success",
            $"Video exported successfully to:\n{result.FullPath}",
            "OK");
      }
      else
      {
        await Shell.Current.CurrentPage.DisplayAlert(
            "Error",
            "Failed to export video.",
            "OK");
      }
    }
    catch (Exception ex)
    {
      // Attempt to go back if we stuck
      try { await Shell.Current.GoToAsync(".."); } catch {}

      await Shell.Current.CurrentPage.DisplayAlert(
          "Error",
          $"An error occurred: {ex.Message}",
          "OK");
    }
  }
}
