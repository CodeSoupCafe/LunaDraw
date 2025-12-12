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

using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components;

  public partial class LayerControlView : ContentView
  {
      public static readonly BindableProperty IsLayerPanelExpandedProperty =
          BindableProperty.Create(nameof(IsLayerPanelExpanded), typeof(bool), typeof(LayerControlView), false, propertyChanged: OnIsLayerPanelExpandedChanged);

      public bool IsLayerPanelExpanded
      {
          get => (bool)GetValue(IsLayerPanelExpandedProperty);
          set => SetValue(IsLayerPanelExpandedProperty, value);
      }

      public List<MaskingMode> MaskingModes { get; } = Enum.GetValues<MaskingMode>().Cast<MaskingMode>().ToList();

      public LayerControlView()
      {
          InitializeComponent();
      }

      private static void OnIsLayerPanelExpandedChanged(BindableObject bindable, object oldValue, object newValue)
      {
          var control = (LayerControlView)bindable;
          control.ContentGrid.IsVisible = (bool)newValue;
          control.CollapseButton.Text = (bool)newValue ? "▼" : "▶";
      }

      private void OnCollapseClicked(object sender, EventArgs e)
      {
          IsLayerPanelExpanded = !IsLayerPanelExpanded;
      }

      private void OnDragStarting(object sender, DragStartingEventArgs e)
      {
          if (sender is Element element && element.BindingContext is Layer layer)
          {
              e.Data.Properties["SourceLayer"] = layer;
              // Ensure the dragged layer is selected
              if (this.BindingContext is MainViewModel viewModel)
              {
                  viewModel.CurrentLayer = layer;
              }
          }
      }

      private void OnDragOver(object sender, DragEventArgs e)
      {
          e.AcceptedOperation = DataPackageOperation.Copy;
      }

      private void OnDrop(object sender, DropEventArgs e)
      {
          if (e.Data.Properties.TryGetValue("SourceLayer", out var sourceObj) && sourceObj is Layer sourceLayer)
          {
              if (sender is Element element && element.BindingContext is Layer targetLayer)
              {
                  if (this.BindingContext is MainViewModel viewModel)
                  {
                       viewModel.ReorderLayer(sourceLayer, targetLayer);
                  }
              }
          }
      }
  }
