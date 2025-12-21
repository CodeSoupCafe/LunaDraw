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

using CommunityToolkit.Maui.Views;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components;

public partial class DrawingGalleryPopup : Popup
{
  private readonly DrawingGalleryPopupViewModel viewModel;

  public DrawingGalleryPopup(DrawingGalleryPopupViewModel viewModel)
  {
    this.viewModel = viewModel;
    InitializeComponent();
    BindingContext = viewModel;

    viewModel.RequestClose += OnRequestClose;
  }

  private void OnDrawingItemTapped(object? sender, EventArgs e)
  {
    if (sender is Grid grid && grid.BindingContext is DrawingItemViewModel item)
    {
      viewModel.OpenDrawingCommand.Execute(item).Subscribe();
    }
  }

  private async void OnRequestClose(object? sender, EventArgs e)
  {
    await this.CloseAsync();
  }

  protected override void OnHandlerChanged()
  {
    base.OnHandlerChanged();

    // Unsubscribe when handler is removed to prevent memory leaks
    if (Handler == null && BindingContext is DrawingGalleryPopupViewModel vm)
    {
      vm.RequestClose -= OnRequestClose;

      if (vm is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }
}
