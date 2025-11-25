using LunaDraw.Logic.Tools;
using ReactiveUI;
using SkiaSharp;
using System.Reactive;

namespace LunaDraw.Logic.ViewModels
{
    public class ToolbarViewModel : ReactiveObject
    {
        private readonly MainViewModel _mainViewModel;

        public List<IDrawingTool> AvailableTools { get; }

        public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand => _mainViewModel.SelectToolCommand;
        public ReactiveCommand<Unit, Unit> UndoCommand => _mainViewModel.UndoCommand;
        public ReactiveCommand<Unit, Unit> RedoCommand => _mainViewModel.RedoCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => _mainViewModel.CopyCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand => _mainViewModel.PasteCommand;
        public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => _mainViewModel.DeleteSelectedCommand;
        public ReactiveCommand<Unit, Unit> GroupSelectedCommand => _mainViewModel.GroupSelectedCommand;
        public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => _mainViewModel.UngroupSelectedCommand;
        public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }

        public SKColor StrokeColor
        {
            get => _mainViewModel.StrokeColor;
            set => _mainViewModel.StrokeColor = value;
        }

        public bool IsSettingsOpen
        {
            get => _mainViewModel.IsSettingsOpen;
            set => _mainViewModel.IsSettingsOpen = value;
        }

        private bool _isShapesFlyoutOpen = false;
        public bool IsShapesFlyoutOpen
        {
            get => _isShapesFlyoutOpen;
            set => this.RaiseAndSetIfChanged(ref _isShapesFlyoutOpen, value);
        }

        public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }

        public SKColor? FillColor
        {
            get => _mainViewModel.FillColor;
            set => _mainViewModel.FillColor = value;
        }

        public float StrokeWidth
        {
            get => _mainViewModel.StrokeWidth;
            set => _mainViewModel.StrokeWidth = value;
        }

        public byte Opacity
        {
            get => _mainViewModel.Opacity;
            set => _mainViewModel.Opacity = value;
        }

        public ToolbarViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            AvailableTools = new List<IDrawingTool>
            {
                new SelectTool(),
                new FreehandTool(),
                new RectangleTool(),
                new EllipseTool(),
                new LineTool(),
                new FillTool(),
                new EraserTool()
            };

            ShowShapesFlyoutCommand = ReactiveCommand.Create(() =>
            {
                // Close settings if open, then toggle shapes
                _mainViewModel.IsSettingsOpen = false;
                IsShapesFlyoutOpen = !IsShapesFlyoutOpen;
            });

            // Settings command — toggle settings and ensure shapes panel closed
            ShowSettingsCommand = ReactiveCommand.Create(() =>
            {
                _mainViewModel.IsSettingsOpen = !_mainViewModel.IsSettingsOpen;
                IsShapesFlyoutOpen = false;
            });

            SelectRectangleCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool();
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectCircleCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool();
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });

            SelectLineCommand = ReactiveCommand.Create(() =>
            {
                var tool = AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool();
                SelectToolCommand.Execute(tool).Subscribe();
                IsShapesFlyoutOpen = false;
            });
        }
    }
}
