using LunaDraw.Logic.Tools;
using ReactiveUI;
using SkiaSharp;
using System.Collections.Generic;
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

        public SKColor StrokeColor
        {
            get => _mainViewModel.StrokeColor;
            set => _mainViewModel.StrokeColor = value;
        }

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
        }
    }
}
