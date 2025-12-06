using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.ViewModels
{
  public class MainViewModel : ReactiveObject
  {
    // Services
    private readonly IToolStateManager toolStateManager;
    private readonly ILayerStateManager layerStateManager;
    private readonly ICanvasInputHandler canvasInputHandler;
    private readonly IMessageBus messageBus;
    public NavigationModel NavigationModel { get; }
    public SelectionManager SelectionManager { get; }

    // Current State (Facade properties for View Binding)
    public ObservableCollection<Layer> Layers => layerStateManager.Layers;
    public List<IDrawingTool> AvailableTools => toolStateManager.AvailableTools;
    public List<BrushShape> AvailableBrushShapes => toolStateManager.AvailableBrushShapes;

    public Layer? CurrentLayer
    {
      get => layerStateManager.CurrentLayer;
      set => layerStateManager.CurrentLayer = value;
    }

    public IDrawingTool ActiveTool
    {
      get => toolStateManager.ActiveTool;
      set => toolStateManager.ActiveTool = value;
    }

    public SKColor StrokeColor
    {
      get => toolStateManager.StrokeColor;
      set => toolStateManager.StrokeColor = value;
    }

    public SKColor? FillColor
    {
      get => toolStateManager.FillColor;
      set => toolStateManager.FillColor = value;
    }

    public float StrokeWidth
    {
      get => toolStateManager.StrokeWidth;
      set => toolStateManager.StrokeWidth = value;
    }

    public byte Opacity
    {
      get => toolStateManager.Opacity;
      set => toolStateManager.Opacity = value;
    }

    public byte Flow
    {
      get => toolStateManager.Flow;
      set => toolStateManager.Flow = value;
    }

    public float Spacing
    {
      get => toolStateManager.Spacing;
      set => toolStateManager.Spacing = value;
    }

    public BrushShape CurrentBrushShape
    {
      get => toolStateManager.CurrentBrushShape;
      set => toolStateManager.CurrentBrushShape = value;
    }

    public bool IsGlowEnabled
    {
      get => toolStateManager.IsGlowEnabled;
      set => toolStateManager.IsGlowEnabled = value;
    }

    public SKColor GlowColor
    {
      get => toolStateManager.GlowColor;
      set => toolStateManager.GlowColor = value;
    }

    public float GlowRadius
    {
      get => toolStateManager.GlowRadius;
      set => toolStateManager.GlowRadius = value;
    }

    public bool IsRainbowEnabled
    {
        get => toolStateManager.IsRainbowEnabled;
        set => toolStateManager.IsRainbowEnabled = value;
    }

    public float ScatterRadius
    {
        get => toolStateManager.ScatterRadius;
        set => toolStateManager.ScatterRadius = value;
    }

    public float SizeJitter
    {
        get => toolStateManager.SizeJitter;
        set => toolStateManager.SizeJitter = value;
    }

    public float AngleJitter
    {
        get => toolStateManager.AngleJitter;
        set => toolStateManager.AngleJitter = value;
    }

    public float HueJitter
    {
        get => toolStateManager.HueJitter;
        set => toolStateManager.HueJitter = value;
    }

    public HistoryManager HistoryManager => layerStateManager.HistoryManager;

    // Selection State
    public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => SelectionManager.Selected;

    // Internal Clipboard (Could be moved to a ClipboardService later)
    private IEnumerable<IDrawableElement> internalClipboard = new List<IDrawableElement>();

    public SKRect CanvasSize { get; set; }

    // OAPH properties for command states
    private readonly ObservableAsPropertyHelper<bool> canDelete;
    public bool CanDelete => canDelete.Value;

    private readonly ObservableAsPropertyHelper<bool> canGroup;
    public bool CanGroup => canGroup.Value;

    private readonly ObservableAsPropertyHelper<bool> canUngroup;
    public bool CanUngroup => canUngroup.Value;

    private readonly ObservableAsPropertyHelper<bool> canUndo;
    public bool CanUndo => canUndo.Value;

    private readonly ObservableAsPropertyHelper<bool> canRedo;
    public bool CanRedo => canRedo.Value;

    private readonly ObservableAsPropertyHelper<bool> canPaste;
    public bool CanPaste => canPaste.Value;

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
    public ReactiveCommand<Layer, Unit> MoveLayerForwardCommand { get; }
    public ReactiveCommand<Layer, Unit> MoveLayerBackwardCommand { get; }
    public ReactiveCommand<Layer, Unit> MoveSelectionToLayerCommand { get; }
    public ReactiveCommand<Layer, Unit> ToggleLayerVisibilityCommand { get; }
    public ReactiveCommand<Layer, Unit> ToggleLayerLockCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }

    public MainViewModel(
        IToolStateManager toolStateManager,
        ILayerStateManager layerStateManager,
        ICanvasInputHandler canvasInputHandler,
        NavigationModel navigationModel,
        SelectionManager selectionManager,
        IMessageBus messageBus)
    {
      this.toolStateManager = toolStateManager;
      this.layerStateManager = layerStateManager;
      this.canvasInputHandler = canvasInputHandler;
      NavigationModel = navigationModel;
      SelectionManager = selectionManager;
      this.messageBus = messageBus;

      // Subscribe to service property changes to notify View
      this.toolStateManager.WhenAnyValue(x => x.ActiveTool).Subscribe(_ => this.RaisePropertyChanged(nameof(ActiveTool)));
      this.toolStateManager.WhenAnyValue(x => x.StrokeColor).Subscribe(_ => this.RaisePropertyChanged(nameof(StrokeColor)));
      this.toolStateManager.WhenAnyValue(x => x.FillColor).Subscribe(_ => this.RaisePropertyChanged(nameof(FillColor)));
      this.toolStateManager.WhenAnyValue(x => x.StrokeWidth).Subscribe(_ => this.RaisePropertyChanged(nameof(StrokeWidth)));
      this.toolStateManager.WhenAnyValue(x => x.Opacity).Subscribe(_ => this.RaisePropertyChanged(nameof(Opacity)));
      this.toolStateManager.WhenAnyValue(x => x.Flow).Subscribe(_ => this.RaisePropertyChanged(nameof(Flow)));
      this.toolStateManager.WhenAnyValue(x => x.Spacing).Subscribe(_ => this.RaisePropertyChanged(nameof(Spacing)));
      this.toolStateManager.WhenAnyValue(x => x.CurrentBrushShape).Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentBrushShape)));
      this.toolStateManager.WhenAnyValue(x => x.IsGlowEnabled).Subscribe(_ => this.RaisePropertyChanged(nameof(IsGlowEnabled)));
      this.toolStateManager.WhenAnyValue(x => x.GlowColor).Subscribe(_ => this.RaisePropertyChanged(nameof(GlowColor)));
      this.toolStateManager.WhenAnyValue(x => x.GlowRadius).Subscribe(_ => this.RaisePropertyChanged(nameof(GlowRadius)));
      this.toolStateManager.WhenAnyValue(x => x.IsRainbowEnabled).Subscribe(_ => this.RaisePropertyChanged(nameof(IsRainbowEnabled)));
      this.toolStateManager.WhenAnyValue(x => x.ScatterRadius).Subscribe(_ => this.RaisePropertyChanged(nameof(ScatterRadius)));
      this.toolStateManager.WhenAnyValue(x => x.SizeJitter).Subscribe(_ => this.RaisePropertyChanged(nameof(SizeJitter)));
      this.toolStateManager.WhenAnyValue(x => x.AngleJitter).Subscribe(_ => this.RaisePropertyChanged(nameof(AngleJitter)));
      this.toolStateManager.WhenAnyValue(x => x.HueJitter).Subscribe(_ => this.RaisePropertyChanged(nameof(HueJitter)));

      // Layer State subscriptions
      this.layerStateManager.WhenAnyValue(x => x.CurrentLayer).Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentLayer)));

      // Invalidate all layers when selection changes to ensure proper cached/live transition
      SelectionManager.SelectionChanged += (s, e) =>
      {
          foreach (var layer in Layers)
          {
              layer.InvalidateCache();
          }
      };

      // Initialize OAPH properties for command states
      canDelete = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count > 0)
        .ToProperty(this, x => x.CanDelete);

      canGroup = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count > 1)
        .ToProperty(this, x => x.CanGroup);

      canUngroup = this.WhenAnyValue(x => x.SelectedElements.Count)
        .Select(count => count == 1 && SelectedElements.FirstOrDefault() is DrawableGroup)
        .ToProperty(this, x => x.CanUngroup);

      canUndo = this.WhenAnyValue(x => x.HistoryManager.CanUndo)
        .ToProperty(this, x => x.CanUndo);

      canRedo = this.WhenAnyValue(x => x.HistoryManager.CanRedo)
        .ToProperty(this, x => x.CanRedo);

      canPaste = this.WhenAnyValue(x => x.internalClipboard)
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
                messageBus.SendMessage(new CanvasInvalidateMessage());
        this.layerStateManager.SaveState();
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
                messageBus.SendMessage(new CanvasInvalidateMessage());
        this.layerStateManager.SaveState();
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
                  messageBus.SendMessage(new CanvasInvalidateMessage());
          this.layerStateManager.SaveState();
        }
      }, this.WhenAnyValue(x => x.CanUngroup), RxApp.MainThreadScheduler);

      CopyCommand = ReactiveCommand.Create(() =>
      {
        internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      CutCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !SelectedElements.Any()) return;
        internalClipboard = SelectedElements.Select(e => e.Clone()).ToList();
        var elementsToRemove = SelectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
          CurrentLayer.Elements.Remove(element);
        }
        SelectionManager.Clear();
                messageBus.SendMessage(new CanvasInvalidateMessage());
        this.layerStateManager.SaveState();
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler);

      PasteCommand = ReactiveCommand.Create(() =>
      {
        if (CurrentLayer is null || !internalClipboard.Any()) return;
        foreach (var element in internalClipboard)
        {
          var clone = element.Clone();
          clone.Translate(new SKPoint(10, 10)); // Offset pasted element
          CurrentLayer.Elements.Add(clone);
        }
                messageBus.SendMessage(new CanvasInvalidateMessage());
        this.layerStateManager.SaveState();
      }, this.WhenAnyValue(x => x.CanPaste), RxApp.MainThreadScheduler);

      AddLayerCommand = ReactiveCommand.Create(() =>
      {
        this.layerStateManager.AddLayer();
      }, outputScheduler: RxApp.MainThreadScheduler);

      RemoveLayerCommand = ReactiveCommand.Create<Layer>(layer =>
      {
        this.layerStateManager.RemoveLayer(layer);
      }, outputScheduler: RxApp.MainThreadScheduler);

      MoveLayerForwardCommand = ReactiveCommand.Create<Layer>(layer =>
      {
        this.layerStateManager.MoveLayerForward(layer);
      }, outputScheduler: RxApp.MainThreadScheduler);

      MoveLayerBackwardCommand = ReactiveCommand.Create<Layer>(layer =>
      {
        this.layerStateManager.MoveLayerBackward(layer);
      }, outputScheduler: RxApp.MainThreadScheduler);

      MoveSelectionToLayerCommand = ReactiveCommand.Create<Layer>(targetLayer =>
      {
          if (targetLayer == null || !SelectedElements.Any()) return;
          this.layerStateManager.MoveElementsToLayer(SelectedElements, targetLayer);
          // We do not clear selection here, so user can see where it went (if visible).
      }, this.WhenAnyValue(x => x.CanDelete), RxApp.MainThreadScheduler); // Reusing CanDelete as proxy for "HasSelection"

      // New Commands for Send Backward / Bring Forward
      // "Send Backward" -> Move down in the stack (decrease ZIndex)
      var sendBackwardCommand = ReactiveCommand.Create(() =>
      {
          if (CurrentLayer == null || !SelectedElements.Any()) return;
          
          // Handling single selection for simplicity in this iteration
          var selected = SelectedElements.First();
          var sortedElements = CurrentLayer.Elements.OrderBy(e => e.ZIndex).ToList();
          int index = sortedElements.IndexOf(selected);

          if (index > 0)
          {
              // Swap with element below
              var elementBelow = sortedElements[index - 1];
              sortedElements[index - 1] = selected;
              sortedElements[index] = elementBelow;

              // Re-assign ZIndices
              for (int i = 0; i < sortedElements.Count; i++)
              {
                  sortedElements[i].ZIndex = i;
              }
              
              messageBus.SendMessage(new CanvasInvalidateMessage());
              this.layerStateManager.SaveState();
          }

      }, this.WhenAnyValue(x => x.CanDelete)); // Has Selection

      // "Bring Forward" -> Move up in the stack (increase ZIndex)
      var bringForwardCommand = ReactiveCommand.Create(() =>
      {
          if (CurrentLayer == null || !SelectedElements.Any()) return;
          
          var selected = SelectedElements.First();
          var sortedElements = CurrentLayer.Elements.OrderBy(e => e.ZIndex).ToList();
          int index = sortedElements.IndexOf(selected);

          if (index < sortedElements.Count - 1)
          {
              // Swap with element above
              var elementAbove = sortedElements[index + 1];
              sortedElements[index + 1] = selected;
              sortedElements[index] = elementAbove;

              // Re-assign ZIndices
              for (int i = 0; i < sortedElements.Count; i++)
              {
                  sortedElements[i].ZIndex = i;
              }

              messageBus.SendMessage(new CanvasInvalidateMessage());
              this.layerStateManager.SaveState();
          }

      }, this.WhenAnyValue(x => x.CanDelete));

      // Expose commands via properties if needed or add to a composite command?
      // For now, I'll add them as public properties.
      SendBackwardCommand = sendBackwardCommand;
      BringForwardCommand = bringForwardCommand;

      // New Commands for Send Element to Back / Bring Element to Front
      SendElementToBackCommand = ReactiveCommand.Create(() =>
      {
          if (CurrentLayer == null || !SelectedElements.Any()) return;

          var selected = SelectedElements.First(); // Assuming single selection for simplicity
          var elements = CurrentLayer.Elements.ToList(); // Get a mutable list
          
          if (elements.Remove(selected)) // Remove the selected element
          {
              elements.Insert(0, selected); // Insert it at the beginning

              // Re-assign ZIndices based on new order
              for (int i = 0; i < elements.Count; i++)
              {
                  elements[i].ZIndex = i;
              }
              // Update the observable collection (this will trigger UI update)
              CurrentLayer.Elements.Clear();
              foreach (var el in elements)
              {
                  CurrentLayer.Elements.Add(el);
              }

              messageBus.SendMessage(new CanvasInvalidateMessage());
              this.layerStateManager.SaveState();
          }
      }, this.WhenAnyValue(x => x.CanDelete)); // CanExecute if there's a selection

      BringElementToFrontCommand = ReactiveCommand.Create(() =>
      {
          if (CurrentLayer == null || !SelectedElements.Any()) return;

          var selected = SelectedElements.First(); // Assuming single selection for simplicity
          var elements = CurrentLayer.Elements.ToList(); // Get a mutable list

          if (elements.Remove(selected)) // Remove the selected element
          {
              elements.Add(selected); // Add it to the end

              // Re-assign ZIndices based on new order
              for (int i = 0; i < elements.Count; i++)
              {
                  elements[i].ZIndex = i;
              }
              // Update the observable collection (this will trigger UI update)
              CurrentLayer.Elements.Clear();
              foreach (var el in elements)
              {
                  CurrentLayer.Elements.Add(el);
              }

              messageBus.SendMessage(new CanvasInvalidateMessage());
              this.layerStateManager.SaveState();
          }
      }, this.WhenAnyValue(x => x.CanDelete)); // CanExecute if there's a selection

      ToggleLayerVisibilityCommand = ReactiveCommand.Create<Layer>(layer =>
      {
          if (layer != null)
          {
              layer.IsVisible = !layer.IsVisible;
              messageBus.SendMessage(new CanvasInvalidateMessage());
          }
      }, outputScheduler: RxApp.MainThreadScheduler);

      ToggleLayerLockCommand = ReactiveCommand.Create<Layer>(layer =>
      {
          if (layer != null)
          {
              layer.IsLocked = !layer.IsLocked;
          }
      }, outputScheduler: RxApp.MainThreadScheduler);

      UndoCommand = ReactiveCommand.Create(() =>
      {
        var state = this.layerStateManager.HistoryManager.Undo();
        if (state != null)
        {
          RestoreState(state);
        }
      }, this.WhenAnyValue(x => x.CanUndo), RxApp.MainThreadScheduler);

      RedoCommand = ReactiveCommand.Create(() =>
      {
        var state = this.layerStateManager.HistoryManager.Redo();
        if (state != null)
        {
          RestoreState(state);
        }
      }, this.WhenAnyValue(x => x.CanRedo), RxApp.MainThreadScheduler);
    }

    public ReactiveCommand<Unit, Unit> SendBackwardCommand { get; }
    public ReactiveCommand<Unit, Unit> BringForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> SendElementToBackCommand { get; }
    public ReactiveCommand<Unit, Unit> BringElementToFrontCommand { get; }

    public void SelectElementAt(SKPoint point)
    {
        // Hit test elements across all layers from Top to Bottom
        IDrawableElement? hitElement = null;
        Layer? hitLayer = null;

        // Iterate layers from Top (Last) to Bottom (First)
        foreach (var layer in Layers.Reverse())
        {
            if (!layer.IsVisible || layer.IsLocked) continue;

            // Hit test elements in this layer, sorted by ZIndex Descending (Topmost first)
            var hit = layer.Elements
                           .Where(e => e.IsVisible)
                           .OrderByDescending(e => e.ZIndex)
                           .FirstOrDefault(e => e.HitTest(point));
            
            if (hit != null)
            {
                hitElement = hit;
                hitLayer = layer;
                break; // Found the top-most element
            }
        }

        if (hitElement != null)
        {
            if (!SelectionManager.Contains(hitElement))
            {
                SelectionManager.Clear();
                SelectionManager.Add(hitElement);
            }
            if (hitLayer != null)
            {
                CurrentLayer = hitLayer;
            }
        }
        else
        {
            SelectionManager.Clear();
        }
        messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void ReorderLayer(Layer source, Layer target)
    {
        if (source == null || target == null || source == target) return;
        int oldIndex = Layers.IndexOf(source);
        int newIndex = Layers.IndexOf(target);
        if (oldIndex >= 0 && newIndex >= 0)
        {
            layerStateManager.MoveLayer(oldIndex, newIndex);
            // Ensure the dragged layer stays selected
            CurrentLayer = source;
        }
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

              messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void ProcessTouch(SKTouchEventArgs e)
    {
      this.canvasInputHandler.ProcessTouch(e, CanvasSize);
    }
  }
}