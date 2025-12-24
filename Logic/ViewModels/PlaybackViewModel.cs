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

namespace LunaDraw.Logic.ViewModels;

public class PlaybackViewModel : ReactiveObject
{
    private readonly IPlaybackHandler _playbackHandler;
    
    private PlaybackSpeed _selectedSpeed = PlaybackSpeed.Quick;
    public PlaybackSpeed SelectedSpeed
    {
        get => _selectedSpeed;
        set => this.RaiseAndSetIfChanged(ref _selectedSpeed, value);
    }

    private PlaybackState _currentState;
    public PlaybackState CurrentState
    {
        get => _currentState;
        private set => this.RaiseAndSetIfChanged(ref _currentState, value);
    }

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public PlaybackViewModel(IPlaybackHandler playbackHandler)
    {
        _playbackHandler = playbackHandler;

        _playbackHandler.CurrentState
            .Subscribe(state => CurrentState = state);

        PlayCommand = ReactiveCommand.CreateFromTask(() => _playbackHandler.PlayAsync(SelectedSpeed));
        PauseCommand = ReactiveCommand.CreateFromTask(() => _playbackHandler.PauseAsync());
        StopCommand = ReactiveCommand.CreateFromTask(() => _playbackHandler.StopAsync());
    }
}
