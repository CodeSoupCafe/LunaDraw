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
    Mica = 2,
    Acrylic = 3,
    MicaAlt = 4
  }

  public static class PlatformHelper
  {
    #region Win32 Imports
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
    #endregion

    #region Constants
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const uint LWA_ALPHA = 0x2;
    private const uint LWA_COLORKEY = 0x1;
    private const uint ULW_ALPHA = 0x2;
    #endregion

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
      public int cxLeftWidth;
      public int cxRightWidth;
      public int cyTopHeight;
      public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
      public int x;
      public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
      public int cx;
      public int cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BLENDFUNCTION
    {
      public byte BlendOp;
      public byte BlendFlags;
      public byte SourceConstantAlpha;
      public byte AlphaFormat;
    }
    #endregion

    /// <summary>
    /// Sets Windows 11 style backdrops (Mica, Acrylic, etc.)
    /// </summary>
    public static void SetAppBackdrop(BackdropType type)
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return;

      IntPtr windowHandle = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);
      int backdropValue = (int)type;
      DwmSetWindowAttribute(windowHandle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));
    }

    /// <summary>
    /// Enables 100% true transparency using layered window approach
    /// </summary>
    public static bool EnableTrueTransparency(byte opacity = 255)
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      // Get current window style
      int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
      
      // Add WS_EX_LAYERED flag
      SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

      // Set the window to use alpha transparency
      return SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
    }

    /// <summary>
    /// Extends glass frame into entire client area (for Glass/Aero effect)
    /// </summary>
    public static bool ExtendGlassFrame()
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      // Extend glass into entire window (-1 for all sides)
      MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
      
      return DwmExtendFrameIntoClientArea(hwnd, ref margins) == 0;
    }

    /// <summary>
    /// Enables click-through transparency (mouse events pass through transparent areas)
    /// </summary>
    public static bool EnableClickThrough(bool enable)
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);
      int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

      if (enable)
      {
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
      }
      else
      {
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT);
      }

      return true;
    }

    /// <summary>
    /// Advanced: Update layered window with per-pixel alpha
    /// Use this for maximum control over transparency
    /// </summary>
    public static bool UpdateLayeredWindowAlpha(IntPtr hdcSrc, int width, int height, byte alpha)
    {
      var mauiWindow = App.Current.Windows.FirstOrDefault();
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      POINT ptSrc = new POINT { x = 0, y = 0 };
      POINT ptDst = new POINT { x = 0, y = 0 };
      SIZE size = new SIZE { cx = width, cy = height };
      
      BLENDFUNCTION blend = new BLENDFUNCTION
      {
        BlendOp = 0,  // AC_SRC_OVER
        BlendFlags = 0,
        SourceConstantAlpha = alpha,
        AlphaFormat = 1  // AC_SRC_ALPHA
      };

      return UpdateLayeredWindow(hwnd, IntPtr.Zero, ref ptDst, ref size, hdcSrc, ref ptSrc, 0, ref blend, ULW_ALPHA);
    }

    /// <summary>
    /// Combination method: Enable full transparency with optional effects
    /// </summary>
    public static bool EnableFullTransparency(TransparencyMode mode, byte opacity = 255)
    {
      switch (mode)
      {
        case TransparencyMode.LayeredWindow:
          return EnableTrueTransparency(opacity);
        
        case TransparencyMode.GlassExtended:
          ExtendGlassFrame();
          return EnableTrueTransparency(opacity);
        
        case TransparencyMode.ClickThrough:
          EnableTrueTransparency(opacity);
          return EnableClickThrough(true);
        
        case TransparencyMode.PerPixelAlpha:
          // First enable layered window
          EnableTrueTransparency(255);
          // Then you can use UpdateLayeredWindowAlpha for custom rendering
          return true;
        
        default:
          return false;
      }
    }
  }

  public enum TransparencyMode
  {
    LayeredWindow,      // Standard transparency with opacity control
    GlassExtended,      // Glass effect extended into client area
    ClickThrough,       // Transparent areas don't receive mouse input
    PerPixelAlpha       // Advanced per-pixel alpha control
  }
}
#endif