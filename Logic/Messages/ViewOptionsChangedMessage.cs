namespace LunaDraw.Logic.Messages;

public class ViewOptionsChangedMessage
{
  public bool ShowButtonLabels { get; }
  public bool ShowLayersPanel { get; }

  public ViewOptionsChangedMessage(bool showButtonLabels, bool showLayersPanel)
  {
    ShowButtonLabels = showButtonLabels;
    ShowLayersPanel = showLayersPanel;
  }
}
