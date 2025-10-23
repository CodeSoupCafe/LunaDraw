using ReactiveUI;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reactive;
using LunaDraw.Logic.Models;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private SKPath? _currentPath;
        private SKColor _currentColor = SKColors.Black;
        private float _strokeWidth = 5;

        public ObservableCollection<DrawablePath> Paths { get; } = new ObservableCollection<DrawablePath>();
        public ReactiveCommand<string, Unit> SetColorCommand { get; }

        public MainViewModel()
        {
            SetColorCommand = ReactiveCommand.Create<string>(colorName =>
            {
                if (SKColor.TryParse(colorName, out var color))
                {
                    _currentColor = color;
                }
            });
        }

        public void ProcessTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    _currentPath = new SKPath();
                    _currentPath.MoveTo(e.Location);
                    break;
                case SKTouchAction.Moved:
                    if (_currentPath != null)
                    {
                        _currentPath.LineTo(e.Location);
                    }
                    break;
                case SKTouchAction.Released:
                    if (_currentPath != null)
                    {
                        Paths.Add(new DrawablePath { Path = _currentPath, Color = _currentColor, StrokeWidth = _strokeWidth });
                        _currentPath = null;
                    }
                    break;
            }
        }
    }
}
