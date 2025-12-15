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

using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using CommunityToolkit.Maui.Extensions;
using System.Reactive;

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

  // Sub-ViewModels
  public LayerPanelViewModel LayerPanelVM { get; }
  public SelectionViewModel SelectionVM { get; }
  public HistoryViewModel HistoryVM { get; }

  // Commands
  public ICommand ZoomInCommand { get; }
  public ICommand ZoomOutCommand { get; }
  public ICommand ResetZoomCommand { get; }
  public ICommand ToggleTraceModeCommand { get; }

  public SKRect CanvasSize { get; set; }

  // UI State
  public List<string> AvailableThemes { get; } = new List<string> { "Automatic", "Light", "Dark" };

  private string selectedTheme = "Automatic";
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
    LayerPanelViewModel layerPanelVM,
    SelectionViewModel selectionVM,
    HistoryViewModel historyVM)
  {
    ToolbarViewModel = toolbarViewModel;
    LayerFacade = layerFacade;
    CanvasInputHandler = canvasInputHandler;
    NavigationModel = navigationModel;
    SelectionObserver = selectionObserver;
    this.messageBus = messageBus;
    this.preferencesFacade = preferencesFacade;
    LayerPanelVM = layerPanelVM;
    SelectionVM = selectionVM;
    HistoryVM = historyVM;

    // Use Property setters to trigger ViewOptionsChangedMessage so ToolbarViewModel syncs up
    ShowButtonLabels = this.preferencesFacade.Get<bool>(AppPreference.ShowButtonLabels);
    ShowLayersPanel = this.preferencesFacade.Get<bool>(AppPreference.ShowLayersPanel);
    var savedTheme = this.preferencesFacade.Get(AppPreference.AppTheme);
    SelectedTheme = AvailableThemes.FirstOrDefault(t => t == savedTheme) ?? AvailableThemes[0];

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

    ToggleTraceModeCommand = ReactiveCommand.Create(() =>
    {
      if (IsTransparentBackground)
      {
        WindowTransparency = 255;
      }
      else
      {
        WindowTransparency = 125;
      }

      messageBus.SendMessage(new CanvasInvalidateMessage());
    }, outputScheduler: RxApp.MainThreadScheduler);

    // Initialize state from Preferences
    IsTransparentBackground = preferencesFacade.Get<bool>(AppPreference.IsTransparentBackgroundEnabled);
    if (!IsTransparentBackground)
    {
      windowTransparency = 255;
    }
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
        "Light" => AppTheme.Light,
        "Dark" => AppTheme.Dark,
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

  private bool isTransparentBackground = false;
  public bool IsTransparentBackground
  {
    get => isTransparentBackground;
    set
    {
      this.RaiseAndSetIfChanged(ref isTransparentBackground, value);
      preferencesFacade.Set(AppPreference.IsTransparentBackgroundEnabled, value);

      if (!isTransparentBackground)
      {
        WindowTransparency = 255;
        UpdateWindowTransparency();
      }

      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  private byte windowTransparency = 180;
  public virtual byte WindowTransparency
  {
    get => windowTransparency;
    set
    {
      this.RaiseAndSetIfChanged(ref windowTransparency, value);
      if (value == 255 && isTransparentBackground)
      {
        IsTransparentBackground = false;
        UpdateWindowTransparency();
      }
      else if (value < 255 && !isTransparentBackground)
      {
        IsTransparentBackground = true;
      }

      if (IsTransparentBackground)
      {
        UpdateWindowTransparency();
      }
    }
  }

  private void UpdateWindowTransparency()
  {
#if WINDOWS
    if (IsTransparentBackground)
    {
      LunaDraw.PlatformHelper.EnableTrueTransparency(WindowTransparency);
    }
    else
    {
      LunaDraw.PlatformHelper.EnableTrueTransparency(255);
    }
#endif
  }
}