using ReactiveUI;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using LunaDraw.Logic.Models;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private SKPath? _currentPath;
        public SKPath? CurrentPath
        {
            get => _currentPath;
            set => this.RaiseAndSetIfChanged(ref _currentPath, value);
        }

        private SKColor _currentColor = SKColors.Black;
        public SKColor CurrentColor
        {
            get => _currentColor;
            set => this.RaiseAndSetIfChanged(ref _currentColor, value);
        }

        private float _strokeWidth = 5;
        public float StrokeWidth
        {
            get => _strokeWidth;
            set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
        }

        public ObservableCollection<DrawablePath> Paths { get; } = new ObservableCollection<DrawablePath>();
        public ReactiveCommand<string, Unit> SetColorCommand { get; }

        public MainViewModel()
        {
            SetColorCommand = ReactiveCommand.Create<string>(colorName =>
            {
                if (SKColor.TryParse(colorName, out var color))
                {
                    CurrentColor = color;
                }
            },
            Observable.Return(true)
            .ObserveOn(RxApp.MainThreadScheduler));
        }

        public void ProcessTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    CurrentPath = new SKPath();
                    CurrentPath.MoveTo(e.Location);
                    break;
                case SKTouchAction.Moved:
                    if (CurrentPath != null)
                    {
                        CurrentPath.LineTo(e.Location);
                    }
                    break;
                case SKTouchAction.Released:
                    if (CurrentPath != null)
                    {
                        Paths.Add(new DrawablePath { Path = CurrentPath, Color = CurrentColor, StrokeWidth = StrokeWidth });
                        CurrentPath = null;
                    }
                    break;
            }
        }
    }
}
