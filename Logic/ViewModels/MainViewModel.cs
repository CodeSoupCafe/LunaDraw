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

using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Reactive.Linq;
using System.Reactive;

using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Constants;
using LunaDraw.Components.Carousel;
using CommunityToolkit.Maui.Views;

using ReactiveUI;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using CommunityToolkit.Maui.Extensions;

namespace LunaDraw.Logic.ViewModels;

public class MainViewModel : ReactiveObject
{
  // Dependencies
  public ToolbarViewModel ToolbarViewModel { get; }
  public ILayerFacade LayerFacade { get; }
  public ICanvasInputHandler CanvasInputHandler { get; }
  public NavigationModel NavigationModel { get; }
  public SelectionObserver SelectionObserver { get; }
  private readonly IMessageBus messageBus;
  private readonly IPreferencesFacade preferencesFacade;
  private readonly IDrawingStorageMomento drawingStorageMomento;

  // Properties for current drawing state
  private Guid currentDrawingId = Guid.Empty;
  public Guid CurrentDrawingId
  {
    get => currentDrawingId;
    private set => this.RaiseAndSetIfChanged(ref currentDrawingId, value);
  }

  private string _currentDrawingName = AppConstants.Defaults.UntitledDrawingName;
  public string CurrentDrawingName
  {
    get => _currentDrawingName;
    set => this.RaiseAndSetIfChanged(ref _currentDrawingName, value);
  }

  // Sub-ViewModels
  public LayerPanelViewModel LayerPanelVM { get; }
  public SelectionViewModel SelectionVM { get; }
  public HistoryViewModel HistoryVM { get; }
  private readonly GalleryViewModel galleryViewModel;

  // Commands
  public ICommand ZoomInCommand { get; }
  public ICommand ZoomOutCommand { get; }
  public ICommand ResetZoomCommand { get; }

  public ReactiveCommand<Unit, Unit> ShowGalleryCommand { get; }
  public ReactiveCommand<External.Drawing, Unit> LoadDrawingCommand { get; }
  public ReactiveCommand<string?, Unit> ExternaDrawingCommand { get; }
  public ReactiveCommand<Unit, Unit> NewDrawingCommand { get; }

  public SKRect CanvasSize { get; set; }

  // UI State
  public List<string> AvailableThemes { get; } = new List<string>
  {
      AppConstants.Themes.Automatic,
      AppConstants.Themes.Light,
      AppConstants.Themes.Dark
  };

  private string selectedTheme = AppConstants.Themes.Automatic;
  public string SelectedTheme
  {
    get => selectedTheme;
    set
    {
      this.RaiseAndSetIfChanged(ref selectedTheme, value);
      preferencesFacade.Set(AppPreference.AppTheme, value);
      UpdateAppTheme(value);
    }
  }

  private bool showButtonLabels;

  public bool ShowButtonLabels
  {
    get => showButtonLabels;
    set
    {
      this.RaiseAndSetIfChanged(ref showButtonLabels, value);
      preferencesFacade.Set(AppPreference.ShowButtonLabels, value);
      messageBus.SendMessage(new ViewOptionsChangedMessage(value, ShowLayersPanel));
    }
  }

  private bool showLayersPanel;
  public bool ShowLayersPanel
  {
    get => showLayersPanel;
    set
    {
      this.RaiseAndSetIfChanged(ref showLayersPanel, value);
      preferencesFacade.Set(AppPreference.ShowLayersPanel, value);
      messageBus.SendMessage(new ViewOptionsChangedMessage(ShowButtonLabels, value));
    }
  }

  // Facades for View/CodeBehind access
  public ObservableCollection<Layer> Layers => LayerFacade.Layers;

  public Layer? CurrentLayer
  {
    get => LayerFacade.CurrentLayer;
    set => LayerFacade.CurrentLayer = value;
  }

  public MainViewModel(
    ToolbarViewModel toolbarViewModel,
    ILayerFacade layerFacade,
    ICanvasInputHandler canvasInputHandler,
    NavigationModel navigationModel,
    SelectionObserver selectionObserver,
    IMessageBus messageBus,
    IPreferencesFacade preferencesFacade,
    IDrawingStorageMomento drawingStorageMomento,
    LayerPanelViewModel layerPanelVM,
    SelectionViewModel selectionVM,
    HistoryViewModel historyVM,
    GalleryViewModel galleryViewModel)
  {
    ToolbarViewModel = toolbarViewModel;
    LayerFacade = layerFacade;
    CanvasInputHandler = canvasInputHandler;
    NavigationModel = navigationModel;
    SelectionObserver = selectionObserver;
    this.messageBus = messageBus;
    this.preferencesFacade = preferencesFacade;
    this.drawingStorageMomento = drawingStorageMomento;
    LayerPanelVM = layerPanelVM;
    SelectionVM = selectionVM;
    HistoryVM = historyVM;
    this.galleryViewModel = galleryViewModel;

    // Initialize Drawing Commands
    LoadDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(LoadDrawingAsync);
    ExternaDrawingCommand = ReactiveCommand.CreateFromTask<string?>(ExternalDrawingAsync);
    NewDrawingCommand = ReactiveCommand.CreateFromTask(NewDrawingAsync);
    ShowGalleryCommand = ReactiveCommand.CreateFromTask(async () =>
    {
      var galleryPopup = new Components.Carousel.RenderCanvasList(drawingStorageMomento, preferencesFacade, messageBus);
      var page = Application.Current?.Windows[0]?.Page;
      if (page != null)
      {
         page.ShowPopup(galleryPopup);
      }
    });

    // Listen for OpenDrawingMessage
    this.messageBus.Listen<OpenDrawingMessage>()
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(msg =>
        {
            LoadDrawingCommand.Execute(msg.Drawing).Subscribe();
        });

    // Initial drawing state
    NewDrawingCommand.Execute().Subscribe();
    
    // Use Property setters to trigger ViewOptionsChangedMessage so ToolbarViewModel syncs up
    ShowButtonLabels = this.preferencesFacade.Get<bool>(AppPreference.ShowButtonLabels);
    ShowLayersPanel = this.preferencesFacade.Get<bool>(AppPreference.ShowLayersPanel);
    var externalTheme = this.preferencesFacade.Get(AppPreference.AppTheme);
    SelectedTheme = AvailableThemes.FirstOrDefault(t => t == externalTheme) ?? AvailableThemes[0];

    ZoomInCommand = ReactiveCommand.Create(ZoomIn);
    ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
    ResetZoomCommand = ReactiveCommand.Create(ResetZoom);

    // Listen for ShowAdvancedSettingsMessage
    this.messageBus.Listen<ShowAdvancedSettingsMessage>().Subscribe(async _ =>
    {
      var popup = new Components.AdvancedSettingsPopup(this);
      var page = Application.Current?.Windows[0]?.Page;
      if (page != null)
      {
        await page.ShowPopupAsync(popup);
      }
    });

    // Auto-save on changes
    this.messageBus.Listen<CanvasInvalidateMessage>()
        .Throttle(TimeSpan.FromSeconds(2))
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(_ =>
        {
          ExternaDrawingCommand.Execute(null).Subscribe();
        });
  }

  public IDrawingTool ActiveTool
  {
    get => ToolbarViewModel.ActiveTool;
    set => ToolbarViewModel.ActiveTool = value;
  }

  public ReadOnlyObservableCollection<IDrawableElement> SelectedElements => SelectionObserver.Selected;

  public void ReorderLayer(Layer source, Layer target)
  {
    if (source == null || target == null || source == target) return;
    int oldIndex = Layers.IndexOf(source);
    int newIndex = Layers.IndexOf(target);
    if (oldIndex >= 0 && newIndex >= 0)
    {
      LayerFacade.MoveLayer(oldIndex, newIndex);
      CurrentLayer = source;
    }
  }

  public void ProcessTouch(SKTouchEventArgs e)
  {
    CanvasInputHandler.ProcessTouch(e, CanvasSize);
  }

  public ToolContext CreateToolContext()
  {
    return new ToolContext
    {
      CurrentLayer = LayerFacade.CurrentLayer!,
      StrokeColor = ToolbarViewModel.StrokeColor,
      FillColor = ToolbarViewModel.FillColor,
      StrokeWidth = ToolbarViewModel.StrokeWidth,
      Opacity = ToolbarViewModel.Opacity,
      Flow = ToolbarViewModel.Flow,
      Spacing = ToolbarViewModel.Spacing,
      BrushShape = ToolbarViewModel.CurrentBrushShape,
      AllElements = LayerFacade.Layers.SelectMany(l => l.Elements),
      Layers = LayerFacade.Layers,
      SelectionObserver = SelectionObserver,
      Scale = NavigationModel.ViewMatrix.ScaleX,
      IsGlowEnabled = ToolbarViewModel.IsGlowEnabled,
      GlowColor = ToolbarViewModel.GlowColor,
      GlowRadius = ToolbarViewModel.GlowRadius,
      IsRainbowEnabled = ToolbarViewModel.IsRainbowEnabled,
      ScatterRadius = ToolbarViewModel.ScatterRadius,
      SizeJitter = ToolbarViewModel.SizeJitter,
      AngleJitter = ToolbarViewModel.AngleJitter,
      HueJitter = ToolbarViewModel.HueJitter,
      CanvasMatrix = NavigationModel.ViewMatrix
    };
  }

  private void UpdateAppTheme(string theme)
  {
    if (Application.Current != null)
    {
      Application.Current.UserAppTheme = theme switch
      {
        AppConstants.Themes.Light => AppTheme.Light,
        AppConstants.Themes.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified
      };
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private void ZoomIn() => Zoom(1.2f);
  private void ZoomOut() => Zoom(1f / 1.2f);

  private void ResetZoom()
  {
    NavigationModel.Reset();
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private void Zoom(float scaleFactor)
  {
    if (CanvasSize.Width <= 0 || CanvasSize.Height <= 0) return;

    var currentScale = NavigationModel.ViewMatrix.ScaleX;
    var newScale = currentScale * scaleFactor;

    // Clamp scale
    if (newScale < 0.1f) scaleFactor = 0.1f / currentScale;
    if (newScale > 20.0f) scaleFactor = 20.0f / currentScale;

    var center = new SKPoint(CanvasSize.Width / 2, CanvasSize.Height / 2);

    // Scale around center
    var zoomMatrix = SKMatrix.CreateScale(scaleFactor, scaleFactor, center.X, center.Y);

    // Apply to existing view matrix
    NavigationModel.ViewMatrix = SKMatrix.Concat(zoomMatrix, NavigationModel.ViewMatrix);

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private async Task LoadDrawingAsync(External.Drawing externalDrawing)
  {
    if (externalDrawing == null) return;

    // Load full details
    var fullDrawing = await drawingStorageMomento.LoadDrawingAsync(externalDrawing.Id);
    if (fullDrawing == null) return;

    CurrentDrawingId = fullDrawing.Id;
    CurrentDrawingName = fullDrawing.Name;

    // Restore layers
    var restoredLayers = drawingStorageMomento.RestoreLayers(fullDrawing);
    LayerFacade.Layers.Clear();
    foreach (var layer in restoredLayers)
    {
      LayerFacade.Layers.Add(layer);
    }
    LayerFacade.CurrentLayer = LayerFacade.Layers.FirstOrDefault();

    // Reset view
    NavigationModel.Reset();
    NavigationModel.CanvasWidth = fullDrawing.CanvasWidth;
    NavigationModel.CanvasHeight = fullDrawing.CanvasHeight;

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private async Task ExternalDrawingAsync(string? nameOverride = null)
  {
    if (CurrentDrawingId == Guid.Empty)
    {
      CurrentDrawingId = Guid.NewGuid();
    }

    if (!string.IsNullOrEmpty(nameOverride))
    {
      CurrentDrawingName = nameOverride;
    }

    var externalDrawing = drawingStorageMomento.CreateExternalDrawingFromCurrent(
        Layers,
        NavigationModel.CanvasWidth,
        NavigationModel.CanvasHeight,
        CurrentDrawingName,
        CurrentDrawingId);

    await drawingStorageMomento.ExternalDrawingAsync(externalDrawing);
  }

  private async Task NewDrawingAsync()
  {
    CurrentDrawingId = Guid.NewGuid();
    CurrentDrawingName = await drawingStorageMomento.GetNextDefaultNameAsync();

    LayerFacade.Layers.Clear();
    LayerFacade.AddLayer(); // Adds default layer

    NavigationModel.Reset();
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }
}
