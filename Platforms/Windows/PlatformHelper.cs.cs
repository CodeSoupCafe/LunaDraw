#if WINDOWS
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Microsoft.Maui.ApplicationModel;

namespace LunaDraw
{
  public enum BackdropType
  {
    Auto = 0,
    None = 1,
    Mica = 2,           // Default main window material
    Acrylic = 3,        // Transient window material (more blur)
    MicaAlt = 4         // Alt mica material
  }

  public static class PlatformHelper
  {
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Constant for Windows 11 Backdrop types attribute
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public static void SetAppBackdrop(BackdropType type)
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return;

      IntPtr windowHandle = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      // Set the desired backdrop type
      int backdropValue = (int)type;
      DwmSetWindowAttribute(windowHandle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));
    }
  }
}
#endif
