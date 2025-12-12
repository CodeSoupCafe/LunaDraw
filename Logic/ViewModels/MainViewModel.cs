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
  public class MainViewModel(
      ToolbarViewModel toolbarViewModel,
      ILayerFacade layerFacade,
      ICanvasInputHandler canvasInputHandler,
      NavigationModel navigationModel,
      SelectionObserver selectionObserver,
      IMessageBus messageBus,
      LayerPanelViewModel layerPanelVM,
      SelectionViewModel selectionVM,
      HistoryViewModel historyVM) : ReactiveObject
  {
    // Dependencies
    public ToolbarViewModel ToolbarViewModel { get; } = toolbarViewModel;
    public ILayerFacade LayerFacade { get; } = layerFacade;
    public ICanvasInputHandler CanvasInputHandler { get; } = canvasInputHandler;
    public NavigationModel NavigationModel { get; } = navigationModel;
    public SelectionObserver SelectionObserver { get; } = selectionObserver;
    private readonly IMessageBus messageBus = messageBus;

    // Sub-ViewModels
    public LayerPanelViewModel LayerPanelVM { get; } = layerPanelVM;
    public SelectionViewModel SelectionVM { get; } = selectionVM;
    public HistoryViewModel HistoryVM { get; } = historyVM;

    public SKRect CanvasSize { get; set; }

    // Facades for View/CodeBehind access
    public ObservableCollection<Layer> Layers => LayerFacade.Layers;

    public Layer? CurrentLayer
    {
      get => LayerFacade.CurrentLayer;
      set => LayerFacade.CurrentLayer = value;
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
  }
}