using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

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
    NewDrawingCommand = ReactiveCommand.Create(() => { }); // Handled by popup closing
    OpenDrawingCommand = ReactiveCommand.Create(() => SelectedDrawing); // Handled by popup closing
    DeleteDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(DeleteDrawingAsync);
    DuplicateDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(DuplicateDrawingAsync);
    RenameDrawingCommand = ReactiveCommand.CreateFromTask<External.Drawing>(RenameDrawingAsync);

    // Initial load
    LoadDrawingsCommand.Execute().Subscribe();
  }

  private async Task LoadDrawingsAsync()
  {
    IsLoading = true;
    System.Diagnostics.Debug.WriteLine("[DEBUG] GalleryViewModel.LoadDrawingsAsync started");

    // Rename any untitled drawings first
    await drawingStorageMomento.RenameUntitledDrawingsAsync();

    Drawings.Clear();
    var loadedDrawings = await drawingStorageMomento.LoadAllDrawingsAsync();
    System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {loadedDrawings.Count} drawings from storage");
    foreach (var drawing in loadedDrawings)
    {
      System.Diagnostics.Debug.WriteLine($"[DEBUG] Adding drawing: {drawing.Title} (ID: {drawing.Id})");
      Drawings.Add(drawing);
    }
    System.Diagnostics.Debug.WriteLine($"[DEBUG] GalleryViewModel.Drawings.Count: {Drawings.Count}");
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
    drawing.Name = newName; // Update local observable
                            // Force refresh if needed, but binding should update.
  }
}
