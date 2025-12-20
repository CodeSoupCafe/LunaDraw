namespace LunaDraw.Logic.Utils;

public interface IDrawingThumbnailFacade
{
  Task<ImageSource?> GetThumbnailAsync(Guid drawingId, int width, int height);
  Task<ImageSource?> GenerateThumbnailAsync(Logic.Models.External.Drawing drawing, int width, int height);
  void InvalidateThumbnail(Guid drawingId);
  void ClearCache();
}
