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
using LunaDraw.Logic.Utils;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace LunaDraw.Logic.ViewModels;

public class GalleryViewModel : ReactiveObject
{
  private readonly IDrawingStorageMomento drawingStorageMomento;
  private ObservableCollection<External.Drawing> _drawings = new();
  private External.Drawing? _selectedDrawing;
  private bool _isLoading;

  public ObservableCollection<External.Drawing> Drawings
  {
    get => _drawings;
    set => this.RaiseAndSetIfChanged(ref _drawings, value);
  }

  private bool _isNewDrawingRequested;
  public bool IsNewDrawingRequested
  {
    get => _isNewDrawingRequested;
    set => this.RaiseAndSetIfChanged(ref _isNewDrawingRequested, value);
  }

  public External.Drawing? SelectedDrawing
  {
    get => _selectedDrawing;
    set => this.RaiseAndSetIfChanged(ref _selectedDrawing, value);
  }

  public bool IsLoading
  {
    get => _isLoading;
    set => this.RaiseAndSetIfChanged(ref _isLoading, value);
  }

  public ReactiveCommand<Unit, Unit> LoadDrawingsCommand { get; }
  public ReactiveCommand<Unit, Unit> NewDrawingCommand { get; }
  public ReactiveCommand<Unit, External.Drawing?> OpenDrawingCommand { get; }
  public ReactiveCommand<External.Drawing, Unit> DeleteDrawingCommand { get; }
  public ReactiveCommand<External.Drawing, Unit> DuplicateDrawingCommand { get; }
  public ReactiveCommand<External.Drawing, Unit> RenameDrawingCommand { get; }
  // Export logic will be handled in the ViewCell due to UI dependency for Snapshot, 
  // but we can have a command to trigger the flow if needed. 
  // For now, let's keep Export logic local to the ViewCell or pass a Func provider.

  public GalleryViewModel(IDrawingStorageMomento drawingStorageMomento)
  {
    this.drawingStorageMomento = drawingStorageMomento;

    LoadDrawingsCommand = ReactiveCommand.CreateFromTask(LoadDrawingsAsync);
    NewDrawingCommand = ReactiveCommand.Create(() => { });
    OpenDrawingCommand = ReactiveCommand.Create(() => SelectedDrawing);
    DeleteDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(DeleteDrawingAsync);
    DuplicateDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(DuplicateDrawingAsync);
    RenameDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(RenameDrawingAsync);
  }

  private async Task LoadDrawingsAsync()
  {
    IsLoading = true;

    await drawingStorageMomento.RenameUntitledDrawingsAsync();

    Drawings.Clear();
    var loadedDrawings = await drawingStorageMomento.LoadAllDrawingsAsync();
    foreach (var drawing in loadedDrawings)
    {
      Drawings.Add(drawing);
    }

    IsLoading = false;
  }

  private async Task DeleteDrawingAsync(External.Drawing drawing)
  {
    await drawingStorageMomento.DeleteDrawingAsync(drawing.Id);
    Drawings.Remove(drawing);
  }

  private async Task DuplicateDrawingAsync(External.Drawing drawing)
  {
    await drawingStorageMomento.DuplicateDrawingAsync(drawing.Id);
    await LoadDrawingsAsync(); // Reload to show new item
  }

  private async Task RenameDrawingAsync(External.Drawing drawing)
  {
    // This command expects the View to have prompted for the name and updated the 'drawing' object locally?
    // Or we prompt here. But VM shouldn't do UI.
    // Better pattern: View invokes command with a Tuple or similar, or we use an Interaction.
    // For simplicity in this CLI context:
    // We will assume the View prompts and passes the NEW NAME. 
    // But the command parameter is currently External.Drawing. 
    // Let's refactor Rename to take a tuple or specific arg, OR rely on the View to handle the prompt 
    // and call a specific public method on VM.

    // Let's change RenameDrawingCommand to take (External.Drawing, string) via a helper class or Tuple.
    // Actually, let's just make a method RenameDrawing(External.Drawing, string) that the View calls.
  }

  public async Task RenameDrawing(External.Drawing drawing, string newName)
  {
    await drawingStorageMomento.RenameDrawingAsync(drawing.Id, newName);
    drawing.Name = newName;
  }

  public async Task<External.Drawing?> ReloadDrawingMetadataAsync(Guid drawingId)
  {
    return await drawingStorageMomento.LoadDrawingAsync(drawingId);
  }
}
