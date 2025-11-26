using LunaDraw.Logic.Models;

using SkiaSharp;

namespace LunaDraw.Logic.Managers
{
  public class HistoryManager
  {
    private readonly List<SKPicture> _history = new();
    private int _historyIndex = -1;

    public bool CanUndo => _historyIndex > 0;
    public bool CanRedo => _historyIndex < _history.Count - 1;

    public void SaveSnapshot(IEnumerable<Layer> layers, SKRect canvasBounds)
    {
      if (canvasBounds.Width <= 0 || canvasBounds.Height <= 0) return;

      // If we have undone, and then make a new action, we clear the 'redo' history
      if (_historyIndex < _history.Count - 1)
      {
        // Dispose pictures that are being cleared
        for (int i = _historyIndex + 1; i < _history.Count; i++)
        {
          _history[i].Dispose();
        }
        _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
      }

      using var recorder = new SKPictureRecorder();
      var canvas = recorder.BeginRecording(canvasBounds);

      // Draw a background
      using var backgroundPaint = new SKPaint { Color = SKColors.White };
      canvas.DrawRect(canvasBounds, backgroundPaint);

      foreach (var layer in layers.Where(l => l.IsVisible))
      {
        foreach (var element in layer.Elements.Where(e => e.IsVisible))
        {
          element.Draw(canvas);
        }
      }

      var snapshot = recorder.EndRecording();
      _history.Add(snapshot);
      _historyIndex++;
    }

    public SKPicture? Undo()
    {
      if (!CanUndo) return null;
      _historyIndex--;
      return _history[_historyIndex];
    }

    public SKPicture? Redo()
    {
      if (!CanRedo) return null;
      _historyIndex++;
      return _history[_historyIndex];
    }

    public void Clear()
    {
      foreach (var picture in _history)
      {
        picture.Dispose();
      }
      _history.Clear();
      _historyIndex = -1;
    }

    public void SaveInitialState(SKRect canvasBounds)
    {
      if (_history.Any() || canvasBounds.Width <= 0 || canvasBounds.Height <= 0) return;

      using var recorder = new SKPictureRecorder();
      var canvas = recorder.BeginRecording(canvasBounds);
      using var backgroundPaint = new SKPaint { Color = SKColors.White };
      canvas.DrawRect(canvasBounds, backgroundPaint);
      var snapshot = recorder.EndRecording();
      _history.Add(snapshot);
      _historyIndex = 0;
    }
  }
}
