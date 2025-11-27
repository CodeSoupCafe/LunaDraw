using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Utils;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        // Services
        private readonly IToolStateManager _toolStateManager;
        private readonly ILayerStateManager _layerStateManager;
        private readonly ICanvasInputHandler _canvasInputHandler;
        public NavigationModel NavigationModel { get; }
        public SelectionManager SelectionManager { get; }

        // Current State (Facade properties for View Binding)
        public ObservableCollection<Layer> Layers => _layerStateManager.Layers;
        public List<IDrawingTool> AvailableTools => _toolStateManager.AvailableTools;
        public List<BrushShape> AvailableBrushShapes => _toolStateManager.AvailableBrushShapes;
        
        public Layer? CurrentLayer
        {
            get => _layerStateManager.CurrentLayer;
            set => _layerStateManager.CurrentLayer = value;
        }

        public IDrawingTool ActiveTool
        {
            get => _toolStateManager.ActiveTool;
            set => _toolStateManager.ActiveTool = value;
        }

        public SKColor StrokeColor
        {
            get => _toolStateManager.StrokeColor;
            set => _toolStateManager.StrokeColor = value;
        }

        public SKColor? FillColor
        {
            get => _toolStateManager.FillColor;
            set => _toolStateManager.FillColor = value;
        }

        public float StrokeWidth
        {
            get => _toolStateManager.StrokeWidth;
            set => _toolStateManager.StrokeWidth = value;
        }

        public byte Opacity
        {
            get => _toolStateManager.Opacity;
            set => _toolStateManager.Opacity = value;
        }

        public byte Flow
        {
            get => _toolStateManager.Flow;
            set => _toolStateManager.Flow = value;
        }

        public float Spacing
        {
            get => _toolStateManager.Spacing;
            set => _toolStateManager.Spacing = value;
        }

        public BrushShape CurrentBrushShape
        {
            get => _toolStateManager.CurrentBrushShape;
            set => _toolStateManager.CurrentBrushShape = value;
        }

        public HistoryManager HistoryManager => _layerStateManager.HistoryManager;

        // Selection State
        public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => SelectionManager.Selected;

        // Internal Clipboard (Could be moved to a ClipboardService later)
        private IEnumerable<IDrawableElement> _internalClipboard = new List<IDrawableElement>();

        public SKRect CanvasSize { get; set; }

        // OAPH properties for command states
        private readonly ObservableAsPropertyHelper<bool> _canDelete;
        public bool CanDelete => _canDelete.Value;

        private readonly ObservableAsPropertyHelper<bool> _canGroup;
        public bool CanGroup => _canGroup.Value;

        private readonly ObservableAsPropertyHelper<bool> _canUngroup;
        public bool CanUngroup => _canUngroup.Value;

        private readonly ObservableAsPropertyHelper<bool> _canUndo;
        public bool CanUndo => _canUndo.Value;

        private readonly ObservableAsPropertyHelper<bool> _canRedo;
        public bool CanRedo => _canRedo.Value;

        private readonly ObservableAsPropertyHelper<bool> _canPaste;
        public bool CanPaste => _canPaste.Value;

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

        public MainViewModel(
            IToolStateManager toolStateManager,
            ILayerStateManager layerStateManager,
            ICanvasInputHandler canvasInputHandler,
            NavigationModel navigationModel,
            SelectionManager selectionManager)
        {
            _toolStateManager = toolStateManager;
            _layerStateManager = layerStateManager;
            _canvasInputHandler = canvasInputHandler;
            NavigationModel = navigationModel;
            SelectionManager = selectionManager;

            // Subscribe to service property changes to notify View
            _toolStateManager.WhenAnyValue(x => x.ActiveTool).Subscribe(_ => this.RaisePropertyChanged(nameof(ActiveTool)));
            _toolStateManager.WhenAnyValue(x => x.StrokeColor).Subscribe(_ => this.RaisePropertyChanged(nameof(StrokeColor)));
            _toolStateManager.WhenAnyValue(x => x.FillColor).Subscribe(_ => this.RaisePropertyChanged(nameof(FillColor)));
            _toolStateManager.WhenAnyValue(x => x.StrokeWidth).Subscribe(_ => this.RaisePropertyChanged(nameof(StrokeWidth)));
            _toolStateManager.WhenAnyValue(x => x.Opacity).Subscribe(_ => this.RaisePropertyChanged(nameof(Opacity)));
            _toolStateManager.WhenAnyValue(x => x.Flow).Subscribe(_ => this.RaisePropertyChanged(nameof(Flow)));
            _toolStateManager.WhenAnyValue(x => x.Spacing).Subscribe(_ => this.RaisePropertyChanged(nameof(Spacing)));
            _toolStateManager.WhenAnyValue(x => x.CurrentBrushShape).Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentBrushShape)));
            
            // Layer State subscriptions
            _layerStateManager.WhenAnyValue(x => x.CurrentLayer).Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentLayer)));


            // Initialize OAPH properties for command states
            _canDelete = this.WhenAnyValue(x => x.SelectedElements.Count)
              .Select(count => count > 0)
              .ToProperty(this, x => x.CanDelete);

            _canGroup = this.WhenAnyValue(x => x.SelectedElements.Count)
              .Select(count => count > 1)
              .ToProperty(this, x => x.CanGroup);

            _canUngroup = this.WhenAnyValue(x => x.SelectedElements.Count)
              .Select(count => count == 1 && SelectedElements.FirstOrDefault() is DrawableGroup)
              .ToProperty(this, x => x.CanUngroup);

            _canUndo = this.WhenAnyValue(x => x.HistoryManager.CanUndo)
              .ToProperty(this, x => x.CanUndo);

            _canRedo = this.WhenAnyValue(x => x.HistoryManager.CanRedo)
              .ToProperty(this, x => x.CanRedo);

            _canPaste = this.WhenAnyValue(x => x._internalClipboard)
              .Select(clipboard => clipboard?.Any() == true)
              .ToProperty(this, x => x.CanPaste);

            // Initialize commands
            SelectToolCommand = ReactiveCommand.Create<IDrawingTool>(tool =>
            {
                ActiveTool = tool; // This sets it in the Service via property setter
            }, outputScheduler: RxApp.MainThreadScheduler);

            DeleteSelectedCommand = ReactiveCommand.Create(() =>
            {
                if (CurrentLayer is null || !SelectedElements.Any()) return;

                var elementsToRemove = SelectedElements.ToList();
                foreach (var element in elementsToRemove)
                {
                    CurrentLayer.Elements.Remove(element);
                }
                SelectionManager.Clear();
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                _layerStateManager.SaveState();
            }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

            GroupSelectedCommand = ReactiveCommand.Create(() =>
            {
                if (CurrentLayer is null || !SelectedElements.Any()) return;

                var elementsToGroup = SelectedElements.ToList();
                var group = new DrawableGroup();

                foreach (var element in elementsToGroup)
                {
                    CurrentLayer.Elements.Remove(element);
                    group.Children.Add(element);
                }
                CurrentLayer.Elements.Add(group);
                SelectionManager.Clear();
                SelectionManager.Add(group);
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                _layerStateManager.SaveState();
            }, this.WhenAnyValue(x => x.CanGroup), RxApp.MainThreadScheduler);

            UngroupSelectedCommand = ReactiveCommand.Create(() =>
            {
                if (CurrentLayer is null) return;
                var group = SelectedElements.First() as DrawableGroup;
                if (group != null)
                {
                    CurrentLayer.Elements.Remove(group);
                    foreach (var child in group.Children)
                    {
                        CurrentLayer.Elements.Add(child);
                    }
                    SelectionManager.Clear();
                    MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                    _layerStateManager.SaveState();
                }
            }, this.WhenAnyValue(x => x.CanUngroup), RxApp.MainThreadScheduler);

            CopyCommand = ReactiveCommand.Create(() =>
            {
                _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
            }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

            CutCommand = ReactiveCommand.Create(() =>
            {
                if (CurrentLayer is null || !SelectedElements.Any()) return;
                _internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
                var elementsToRemove = SelectedElements.ToList();
                foreach (var element in elementsToRemove)
                {
                    CurrentLayer.Elements.Remove(element);
                }
                SelectionManager.Clear();
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                _layerStateManager.SaveState();
            }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

            PasteCommand = ReactiveCommand.Create(() =>
            {
                if (CurrentLayer is null || !_internalClipboard.Any()) return;
                foreach (var element in _internalClipboard)
                {
                    var clone = element.Clone();
                    clone.Translate(new SKPoint(10, 10)); // Offset pasted element
                    CurrentLayer.Elements.Add(clone);
                }
                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                _layerStateManager.SaveState();
            }, this.WhenAnyValue(x => x.CanPaste), RxApp.MainThreadScheduler);

            AddLayerCommand = ReactiveCommand.Create(() =>
            {
                _layerStateManager.AddLayer();
            }, outputScheduler: RxApp.MainThreadScheduler);

            RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
            {
                _layerStateManager.RemoveLayer(layer);
            }, outputScheduler: RxApp.MainThreadScheduler);

            UndoCommand = ReactiveCommand.Create(() =>
            {
                var state = _layerStateManager.HistoryManager.Undo();
                if (state != null)
                {
                    RestoreState(state);
                }
            }, this.WhenAnyValue(x => x.CanUndo), RxApp.MainThreadScheduler);

            RedoCommand = ReactiveCommand.Create(() =>
            {
                var state = _layerStateManager.HistoryManager.Redo();
                if (state != null)
                {
                    RestoreState(state);
                }
            }, this.WhenAnyValue(x => x.CanRedo), RxApp.MainThreadScheduler);
        }

        private void RestoreState(List<Layer> state)
        {
            Layers.Clear();
            foreach (var layer in state)
            {
                Layers.Add(layer);
            }

            // Try to find the previously selected layer by ID, or default to first
            var currentLayerId = CurrentLayer?.Id;
            CurrentLayer = Layers.FirstOrDefault(l => l.Id == currentLayerId) ?? Layers.FirstOrDefault();

            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void ProcessTouch(SKTouchEventArgs e)
        {
            _canvasInputHandler.ProcessTouch(e);
        }
    }
}