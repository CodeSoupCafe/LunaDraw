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
using LunaDraw.Logic.Handlers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

namespace LunaDraw.Logic.Utils;

public class LayerFacade : ReactiveObject, ILayerFacade
{
  public ObservableCollection<Layer> Layers { get; } = [];
  public HistoryMemento HistoryMemento { get; } = new HistoryMemento();

  private Layer? currentLayer;
  public Layer? CurrentLayer
  {
    get => currentLayer;
    set => this.RaiseAndSetIfChanged(ref currentLayer, value);
  }

  private readonly IMessageBus messageBus;
  private readonly IRecordingHandler recordingHandler;

  public LayerFacade(IMessageBus messageBus, IRecordingHandler recordingHandler)
  {
    this.messageBus = messageBus;
    this.recordingHandler = recordingHandler;

    // Initialize with a default layer
    var initialLayer = new Layer { Name = "Layer 1" };
    SetupLayerMonitoring(initialLayer);
    Layers.Add(initialLayer);
    CurrentLayer = initialLayer;

    Layers.CollectionChanged += OnLayersCollectionChanged;

    this.messageBus.Listen<DrawingStateChangedMessage>().Subscribe(_ => SaveState());

    SaveState();
  }

  private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
  {
    if (e.NewItems != null)
    {
      foreach (Layer layer in e.NewItems)
      {
        SetupLayerMonitoring(layer);
      }
    }
  }

  private void SetupLayerMonitoring(Layer layer)
  {
      layer.Elements.CollectionChanged += (s, args) =>
      {
          if (args.NewItems != null)
          {
              foreach (IDrawableElement element in args.NewItems)
              {
                  recordingHandler.RecordCreation(element);
              }
          }
      };
  }

  public void AddLayer()
  {
    var newLayer = new Layer { Name = $"Layer {Layers.Count + 1}" };
    // SetupLayerMonitoring is handled by CollectionChanged
    Layers.Add(newLayer);
    CurrentLayer = newLayer;
    SaveState();
    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  public void RemoveLayer(Layer layer)
  {
    if (Layers.Count > 1)
    {
      // Select a different layer before removing the current one to avoid UI selection issues
      var nextLayer = Layers.FirstOrDefault(l => l != layer);
      if (nextLayer != null)
      {
        CurrentLayer = nextLayer;
      }

      Layers.Remove(layer);
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  public void MoveLayerForward(Layer layer)
  {
    int index = Layers.IndexOf(layer);
    if (index >= 0 && index < Layers.Count - 1)
    {
      Layers.Move(index, index + 1);
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  public void MoveLayerBackward(Layer layer)
  {
    int index = Layers.IndexOf(layer);
    if (index > 0)
    {
      Layers.Move(index, index - 1);
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  public void MoveLayer(int oldIndex, int newIndex)
  {
    if (oldIndex >= 0 && oldIndex < Layers.Count && newIndex >= 0 && newIndex < Layers.Count)
    {
      Layers.Move(oldIndex, newIndex);
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  public void MoveElementsToLayer(IEnumerable<IDrawableElement> elements, Layer targetLayer)
  {
    if (!Layers.Contains(targetLayer)) return;

    bool changed = false;
    foreach (var element in elements.ToList()) // ToList to avoid modification during enumeration
    {
      // Find the layer containing this element
      var sourceLayer = Layers.FirstOrDefault(l => l.Elements.Contains(element));
      if (sourceLayer != null && sourceLayer != targetLayer)
      {
        sourceLayer.Elements.Remove(element);
        targetLayer.Elements.Add(element);
        changed = true;
      }
    }

    if (changed)
    {
      SaveState();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }
  }

  public void SaveState()
  {
    HistoryMemento.SaveState(Layers);
  }
}