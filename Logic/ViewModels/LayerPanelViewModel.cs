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
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.ViewModels;

public class LayerPanelViewModel : ReactiveObject
{
  private readonly ILayerFacade layerFacade;
  private readonly IMessageBus messageBus;
  private readonly IPreferencesService preferencesService;

  public LayerPanelViewModel(ILayerFacade layerFacade, IMessageBus messageBus, IPreferencesService preferencesService)
  {
    this.layerFacade = layerFacade;
    this.messageBus = messageBus;
    this.preferencesService = preferencesService;

    layerFacade.WhenAnyValue(x => x.CurrentLayer)
        .Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentLayer)));

    // Commands
    AddLayerCommand = ReactiveCommand.Create(() =>
    {
      layerFacade.AddLayer();
    }, outputScheduler: RxApp.MainThreadScheduler);

    var layersChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
        h => layerFacade.Layers.CollectionChanged += h,
        h => layerFacade.Layers.CollectionChanged -= h)
        .Select(_ => Unit.Default)
        .StartWith(Unit.Default);

    var currentLayerChanged = layerFacade.WhenAnyValue(x => x.CurrentLayer)
        .Select(_ => Unit.Default);

    var canRemoveLayer = Observable.Merge(layersChanged, currentLayerChanged)
        .Select(_ => layerFacade.CurrentLayer != null && layerFacade.Layers.Count > 1)
        .ObserveOn(RxApp.MainThreadScheduler);

    RemoveLayerCommand = ReactiveCommand.Create(() =>
    {
      if (layerFacade.CurrentLayer != null)
      {
        layerFacade.RemoveLayer(layerFacade.CurrentLayer);
      }
    },
    canExecute: canRemoveLayer,
    outputScheduler: RxApp.MainThreadScheduler);

    MoveLayerForwardCommand = ReactiveCommand.Create<Layer>(layer =>
    {
      layerFacade.MoveLayerForward(layer);
    }, outputScheduler: RxApp.MainThreadScheduler);

    MoveLayerBackwardCommand = ReactiveCommand.Create<Layer>(layer =>
    {
      layerFacade.MoveLayerBackward(layer);
    }, outputScheduler: RxApp.MainThreadScheduler);

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

    ToggleTraceModeCommand = ReactiveCommand.Create(() =>
    {
      if (IsTransparentBackground)
      {
        WindowTransparency = 255;
      }
      else
      {
        WindowTransparency = 150;
      }
    }, outputScheduler: RxApp.MainThreadScheduler);

    // Initialize state from Preferences
    IsTransparentBackground = preferencesService.Get("IsTransparentBackgroundEnabled", false);
    if (!IsTransparentBackground)
    {
      windowTransparency = 255;
    }
  }

  public ObservableCollection<Layer> Layers => layerFacade.Layers;

  public Layer? CurrentLayer
  {
    get => layerFacade.CurrentLayer;
    set => layerFacade.CurrentLayer = value;
  }

  private bool isTransparentBackground = false;
  public bool IsTransparentBackground
  {
    get => isTransparentBackground;
    set
    {
      this.RaiseAndSetIfChanged(ref isTransparentBackground, value);
      preferencesService.Set("IsTransparentBackgroundEnabled", value);

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

  public bool IsTransparentBackgroundVisible => Config.FeatureFlags.EnableTransparentBackground;

  public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
  public ReactiveCommand<Unit, Unit> RemoveLayerCommand { get; }
  public ReactiveCommand<Layer, Unit> MoveLayerForwardCommand { get; }
  public ReactiveCommand<Layer, Unit> MoveLayerBackwardCommand { get; }
  public ReactiveCommand<Layer, Unit> ToggleLayerVisibilityCommand { get; }
  public ReactiveCommand<Layer, Unit> ToggleLayerLockCommand { get; }
  public ReactiveCommand<Unit, Unit> ToggleTraceModeCommand { get; }
}
