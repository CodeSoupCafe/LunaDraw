using LunaDraw.Logic.Commands;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using ReactiveUI;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace LunaDraw.Logic.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        // Current State
        public ObservableCollection<Layer> Layers { get; } = new ObservableCollection<Layer>();
        private Layer? _currentLayer;
        public Layer? CurrentLayer
        {
            get => _currentLayer;
            set => this.RaiseAndSetIfChanged(ref _currentLayer, value);
        }

        private IDrawingTool _activeTool = new FreehandTool();
        public IDrawingTool ActiveTool
        {
            get => _activeTool;
            set => this.RaiseAndSetIfChanged(ref _activeTool, value);
        }

        private SKColor _strokeColor = SKColors.Black;
        public SKColor StrokeColor
        {
            get => _strokeColor;
            set => this.RaiseAndSetIfChanged(ref _strokeColor, value);
        }

        private SKColor? _fillColor;
        public SKColor? FillColor
        {
            get => _fillColor;
            set => this.RaiseAndSetIfChanged(ref _fillColor, value);
        }

        private float _strokeWidth = 5;
        public float StrokeWidth
        {
            get => _strokeWidth;
            set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
        }

        private byte _opacity = 255;
        public byte Opacity
        {
            get => _opacity;
            set => this.RaiseAndSetIfChanged(ref _opacity, value);
        }

        // Selection State
        public ObservableCollection<IDrawableElement> SelectedElements { get; } = new ObservableCollection<IDrawableElement>();

        // Internal Clipboard
        private List<IDrawableElement> _internalClipboard = new List<IDrawableElement>();

        // History
        public CommandHistory History { get; } = new CommandHistory();

        // Commands
        public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> GroupSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> UngroupSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyCommand { get; }
        public ReactiveCommand<Unit, Unit> CutCommand { get; }
        public ReactiveCommand<Unit, Unit> PasteCommand { get; }
        public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
        public ReactiveCommand<Layer, Unit> RemoveLayerCommand { get; }
        public ReactiveCommand<Unit, Unit> UndoCommand { get; }
        public ReactiveCommand<Unit, Unit> RedoCommand { get; }

        public MainViewModel()
        {
            // Initialize with a default layer
            var initialLayer = new Layer { Name = "Layer 1" };
            Layers.Add(initialLayer);
            CurrentLayer = initialLayer;

            SelectToolCommand = ReactiveCommand.Create<IDrawingTool>(tool =>
            {
                ActiveTool = tool;
                MessageBus.Current.SendMessage(new ToolChangedMessage(tool));
            });

            var canDelete = this.WhenAnyValue(x => x.SelectedElements.Count)
                                 .Select(count => count > 0)
                                 .ObserveOn(RxApp.MainThreadScheduler);
            DeleteSelectedCommand = ReactiveCommand.Create(() =>
            {
                var command = new RemoveElementCommand(CurrentLayer, SelectedElements.First());
                History.Execute(command);
                SelectedElements.Clear();
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            }, canDelete);

            var canGroup = this.WhenAnyValue(x => x.SelectedElements.Count)
                                .Select(count => count > 1)
                                .ObserveOn(RxApp.MainThreadScheduler);
            GroupSelectedCommand = ReactiveCommand.Create(() =>
            {
                var command = new GroupElementsCommand(CurrentLayer, SelectedElements);
                History.Execute(command);
                SelectedElements.Clear();
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            }, canGroup);

            var canUngroup = this.WhenAnyValue(x => x.SelectedElements.Count)
                                 .Select(count => count == 1 && SelectedElements.FirstOrDefault() is DrawableGroup)
                                 .ObserveOn(RxApp.MainThreadScheduler);
            UngroupSelectedCommand = ReactiveCommand.Create(() =>
            {
                var group = SelectedElements.First() as DrawableGroup;
                if (group != null)
                {
                    var command = new UngroupElementsCommand(CurrentLayer, group);
                    History.Execute(command);
                    SelectedElements.Clear();
                    MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                }
            }, canUngroup);

            CopyCommand = ReactiveCommand.Create(() =>
            {
                _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
            }, canDelete);

            CutCommand = ReactiveCommand.Create(() =>
            {
                _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
                var command = new RemoveElementCommand(CurrentLayer, SelectedElements.First());
                History.Execute(command);
                SelectedElements.Clear();
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            }, canDelete);

            PasteCommand = ReactiveCommand.Create(() =>
            {
                foreach (var element in _internalClipboard)
                {
                    var clone = element.Clone();
                    clone.Translate(new SKPoint(10, 10)); // Offset pasted element
                    var command = new AddElementCommand(CurrentLayer, clone);
                    History.Execute(command);
                }
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            });

            AddLayerCommand = ReactiveCommand.Create(() =>
            {
                var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
                Layers.Add(newLayer);
                CurrentLayer = newLayer;
            });

            RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                if (Layers.Count > 1)
                {
                    Layers.Remove(layer);
                    CurrentLayer = Layers.First();
                }
            });

            UndoCommand = ReactiveCommand.Create(() => History.Undo(), History.WhenAnyValue(x => x.CanUndo).ObserveOn(RxApp.MainThreadScheduler));
            RedoCommand = ReactiveCommand.Create(() => History.Redo(), History.WhenAnyValue(x => x.CanRedo).ObserveOn(RxApp.MainThreadScheduler));

            // Message listeners
            MessageBus.Current.Listen<ElementAddedMessage>().Subscribe(msg =>
            {
                var command = new AddElementCommand(msg.TargetLayer, msg.Element);
                History.Execute(command);
            });

            MessageBus.Current.Listen<ElementRemovedMessage>().Subscribe(msg =>
            {
                var command = new RemoveElementCommand(msg.SourceLayer, msg.Element);
                History.Execute(command);
            });
        }

        public void ProcessTouch(SKTouchEventArgs e)
        {
            if (CurrentLayer == null) return;

            var context = new ToolContext
            {
                CurrentLayer = CurrentLayer,
                StrokeColor = StrokeColor,
                FillColor = FillColor,
                StrokeWidth = StrokeWidth,
                Opacity = Opacity,
                AllElements = Layers.SelectMany(l => l.Elements),
                SelectedElements = SelectedElements.ToList()
            };

            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    ActiveTool.OnTouchPressed(e.Location, context);
                    break;
                case SKTouchAction.Moved:
                    ActiveTool.OnTouchMoved(e.Location, context);
                    break;
                case SKTouchAction.Released:
                    ActiveTool.OnTouchReleased(e.Location, context);
                    break;
            }
        }
    }
}
