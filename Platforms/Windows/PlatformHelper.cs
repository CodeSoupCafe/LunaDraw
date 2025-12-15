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

#if WINDOWS
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace LunaDraw
{
  /// <summary>
  /// Defines the available Windows 11 backdrop effects that can be applied to the application window.
  /// These effects modify the visual appearance of the window background.
  /// </summary>
  public enum BackdropType
  {
    /// <summary>
    /// Automatically selects the appropriate backdrop based on system settings and theme.
    /// Windows will choose between Mica and Acrylic depending on system preferences.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Disables all backdrop effects, resulting in a solid color background.
    /// Use this for standard opaque windows without transparency effects.
    /// </summary>
    None = 1,

    /// <summary>
    /// Applies the Mica material effect - a subtle, dynamic backdrop that samples the desktop wallpaper.
    /// Provides a soft, translucent appearance that changes with the wallpaper.
    /// Best for primary application windows.
    /// </summary>
    Mica = 2,

    /// <summary>
    /// Applies the Acrylic material effect - a semi-transparent blur effect.
    /// Creates a frosted glass appearance with more pronounced transparency than Mica.
    /// Best for transient UI surfaces like popups and context menus.
    /// </summary>
    Acrylic = 3,

    /// <summary>
    /// Alternative Mica variant with different visual characteristics.
    /// Provides a slightly different aesthetic than standard Mica.
    /// </summary>
    MicaAlt = 4
  }

  /// <summary>
  /// Platform-specific helper class for controlling Windows window transparency and visual effects.
  /// Provides methods to enable various transparency modes, backdrop effects, and advanced rendering options
  /// using Windows API calls. Only available on Windows platform (conditional compilation with #if WINDOWS).
  /// </summary>
  public static class PlatformHelper
  {
    #region Win32 Imports
    /// <summary>
    /// Sets attributes for Desktop Window Manager (DWM) window effects.
    /// Used internally to configure backdrop materials like Mica and Acrylic.
    /// </summary>
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>
    /// Extends the glass frame (Aero effect) into the client area of the window.
    /// Used internally to create glass/blur effects across the entire window.
    /// </summary>
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    /// <summary>
    /// Retrieves window style flags. Used to read current window extended styles.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Sets window style flags. Used to modify window extended styles like WS_EX_LAYERED.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>
    /// Sets transparency and color key attributes for layered windows.
    /// Provides basic alpha blending and color key transparency.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    /// <summary>
    /// Updates a layered window with advanced per-pixel alpha blending.
    /// Allows complete control over each pixel's transparency value.
    /// </summary>
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
    /// Applies Windows 11 system backdrop effects (Mica, Acrylic, etc.) to the application window.
    /// This method modifies the visual appearance of the window background using modern Windows materials.
    /// </summary>
    /// <param name="type">The backdrop effect to apply. Use BackdropType.Mica for desktop wallpaper-aware backgrounds,
    /// BackdropType.Acrylic for frosted glass blur effects, or BackdropType.None to disable effects.</param>
    /// <remarks>
    /// Operation: Retrieves the main MAUI window handle and calls DwmSetWindowAttribute with the 
    /// DWMWA_SYSTEMBACKDROP_TYPE attribute to set the backdrop material.
    /// 
    /// Requirements: Windows 11 or later. Effects may not work on older Windows versions.
    /// The window must have appropriate transparency settings in its visual tree.
    /// 
    /// Usage Example:
    /// <code>
    /// // Apply Mica material for a modern, wallpaper-aware background
    /// PlatformHelper.SetAppBackdrop(BackdropType.Mica);
    /// 
    /// // Apply Acrylic for frosted glass effect
    /// PlatformHelper.SetAppBackdrop(BackdropType.Acrylic);
    /// 
    /// // Disable backdrop effects
    /// PlatformHelper.SetAppBackdrop(BackdropType.None);
    /// </code>
    /// </remarks>
    public static void SetAppBackdrop(BackdropType type)
    {
      var mauiWindow = App.Current?.Windows[0];
      if (mauiWindow == null) return;

      IntPtr windowHandle = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);
      int backdropValue = (int)type;
      _ = DwmSetWindowAttribute(windowHandle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));
    }

    /// <summary>
    /// Enables true alpha channel transparency for the entire window using the layered window technique.
    /// This allows the window to have uniform transparency, making it semi-transparent or fully transparent.
    /// </summary>
    /// <param name="opacity">The opacity level for the entire window (0-255). 
    /// 0 = fully transparent (invisible), 255 = fully opaque (default), 128 = 50% transparent.</param>
    /// <returns>True if transparency was successfully enabled, false if the operation failed or window handle was unavailable.</returns>
    /// <remarks>
    /// Operation: Sets the WS_EX_LAYERED extended window style flag, which enables layered window rendering.
    /// Then calls SetLayeredWindowAttributes to apply uniform alpha transparency to the entire window.
    /// This affects the window as a whole, not individual pixels.
    /// 
    /// Key Characteristics:
    /// - Applies uniform transparency across the entire window
    /// - Fast and simple to implement
    /// - Good for fade effects or ghost windows
    /// - Does NOT provide per-pixel alpha control (use PerPixelAlpha mode for that)
    /// - Window remains interactive (receives mouse and keyboard input)
    /// 
    /// Usage Example:
    /// <code>
    /// // Make window 50% transparent
    /// PlatformHelper.EnableTrueTransparency(128);
    /// 
    /// // Make window 80% transparent for a subtle ghost effect
    /// PlatformHelper.EnableTrueTransparency(51); // 51/255 ≈ 20% opaque
    /// 
    /// // Restore full opacity
    /// PlatformHelper.EnableTrueTransparency(255);
    /// </code>
    /// </remarks>
    public static bool EnableTrueTransparency(byte opacity = 255)
    {
      var mauiWindow = App.Current?.Windows[0];
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      // Get current window style
      int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

      // Add WS_EX_LAYERED flag
      _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

      // Set the window to use alpha transparency
      return SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
    }

    /// <summary>
    /// Extends the glass/Aero frame effect into the entire client area of the window.
    /// Creates the classic Windows Vista/7 Aero Glass appearance with blur behind transparent areas.
    /// </summary>
    /// <returns>True if the glass frame was successfully extended, false otherwise.</returns>
    /// <remarks>
    /// Operation: Calls DwmExtendFrameIntoClientArea with negative margin values (-1 for all sides),
    /// which tells Windows to extend the glass effect across the entire window surface.
    /// Transparent or semi-transparent UI elements will show the blur effect behind them.
    /// 
    /// Key Characteristics:
    /// - Creates the classic "Aero Glass" blur effect
    /// - Requires UI elements with transparency to see the effect
    /// - Often combined with EnableTrueTransparency for best results
    /// - The blur samples content behind the window (desktop, other windows)
    /// - Performance impact due to blur rendering
    /// 
    /// Usage Example:
    /// <code>
    /// // Enable glass frame for classic Aero effect
    /// PlatformHelper.ExtendGlassFrame();
    /// 
    /// // Combine with transparency for full effect
    /// PlatformHelper.ExtendGlassFrame();
    /// PlatformHelper.EnableTrueTransparency(200);
    /// </code>
    /// 
    /// Visual Requirements:
    /// Your XAML UI must have transparent or semi-transparent backgrounds to see the effect:
    /// <code>
    /// &lt;ContentPage BackgroundColor="Transparent"&gt;
    ///     &lt;!-- Content here will show glass effect behind it --&gt;
    /// &lt;/ContentPage&gt;
    /// </code>
    /// </remarks>
    public static bool ExtendGlassFrame()
    {
      var mauiWindow = App.Current?.Windows.FirstOrDefault();
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      // Extend glass into entire window (-1 for all sides)
      MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };

      return DwmExtendFrameIntoClientArea(hwnd, ref margins) == 0;
    }

    /// <summary>
    /// Enables or disables click-through behavior, allowing mouse events to pass through transparent 
    /// areas of the window to windows beneath it. Useful for overlay windows and on-screen displays.
    /// </summary>
    /// <param name="enable">True to enable click-through (transparent areas ignore mouse input), 
    /// false to restore normal mouse interaction.</param>
    /// <returns>True if the operation succeeded, false if the window handle was unavailable.</returns>
    /// <remarks>
    /// Operation: Adds or removes the WS_EX_TRANSPARENT extended window style flag.
    /// When enabled, mouse clicks and movements over transparent parts of the window will pass through 
    /// to whatever window or desktop element is behind it. Opaque areas still receive input.
    /// 
    /// Key Characteristics:
    /// - Mouse events pass through transparent/translucent areas
    /// - Opaque UI elements still receive mouse input
    /// - Essential for overlay windows (HUDs, on-screen displays, drawing overlays)
    /// - Can be toggled on/off dynamically during runtime
    /// - Must be combined with WS_EX_LAYERED (automatically added by this method)
    /// 
    /// Important: This affects ALL transparency - both uniform (from EnableTrueTransparency) 
    /// and per-pixel alpha. Only truly opaque pixels (alpha = 255) will capture mouse input.
    /// 
    /// Usage Example:
    /// <code>
    /// // Create a drawing overlay that only captures clicks on drawn content
    /// PlatformHelper.EnableTrueTransparency(0); // Fully transparent background
    /// PlatformHelper.EnableClickThrough(true);
    /// 
    /// // Later, restore normal interaction (e.g., for settings dialog)
    /// PlatformHelper.EnableClickThrough(false);
    /// 
    /// // Practical use: On-screen FPS counter that doesn't block clicks
    /// PlatformHelper.EnableTrueTransparency(0);
    /// PlatformHelper.EnableClickThrough(true);
    /// // Now display FPS text - text will receive clicks, transparent areas won't
    /// </code>
    /// 
    /// Common Use Cases:
    /// - Screen annotation tools (only capture clicks on drawing strokes)
    /// - Overlay HUDs for games or apps
    /// - Always-on-top information displays
    /// - Custom screen cursors or effects
    /// </remarks>
    public static bool EnableClickThrough(bool enable)
    {
      var mauiWindow = App.Current?.Windows[0];
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);
      int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

      if (enable)
      {
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
      }
      else
      {
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT);
      }

      return true;
    }

    /// <summary>
    /// Advanced method for updating a layered window with per-pixel alpha blending from a custom bitmap.
    /// Provides maximum control over transparency by allowing each individual pixel to have its own 
    /// alpha value. This is the most powerful transparency option but requires manual bitmap management.
    /// </summary>
    /// <param name="hdcSrc">Handle to a device context (HDC) containing the source bitmap with alpha channel.
    /// This bitmap must be 32-bit ARGB format with pre-multiplied alpha values.</param>
    /// <param name="width">Width of the source bitmap in pixels. Should match your window dimensions.</param>
    /// <param name="height">Height of the source bitmap in pixels. Should match your window dimensions.</param>
    /// <param name="alpha">Global alpha multiplier (0-255) applied to all pixels. 255 = use bitmap's alpha values as-is,
    /// 128 = make entire image 50% more transparent, 0 = fully transparent regardless of bitmap alpha.</param>
    /// <returns>True if the window was successfully updated with the new bitmap, false otherwise.</returns>
    /// <remarks>
    /// Operation: Calls UpdateLayeredWindow with the provided bitmap's device context to completely 
    /// replace the window's visual content with custom-rendered graphics. Each pixel in the bitmap 
    /// can have its own transparency value (0-255 alpha), allowing for complex transparency effects 
    /// like anti-aliased edges, gradients, and irregular shapes.
    /// 
    /// Key Characteristics:
    /// - Complete per-pixel control over transparency and color
    /// - Requires manual bitmap creation and rendering
    /// - Best performance when bitmap is pre-rendered
    /// - No built-in UI framework integration - you handle all rendering
    /// - Ideal for custom-drawn interfaces, irregular window shapes, or advanced effects
    /// 
    /// Technical Details:
    /// - Bitmap must be 32-bit ARGB format
    /// - Alpha values should be pre-multiplied: RGB values must be pre-multiplied by alpha (Color.A * Color.R / 255, etc.)
    /// - BlendOp = 0 (AC_SRC_OVER) for standard alpha blending
    /// - AlphaFormat = 1 (AC_SRC_ALPHA) to use per-pixel alpha from bitmap
    /// 
    /// Usage Example:
    /// <code>
    /// // First, enable layered window mode
    /// PlatformHelper.EnableTrueTransparency(255);
    /// 
    /// // Create a 32-bit ARGB bitmap (using GDI+ or SkiaSharp)
    /// IntPtr screenDC = GetDC(IntPtr.Zero);
    /// IntPtr memDC = CreateCompatibleDC(screenDC);
    /// 
    /// // Create a 32-bit DIB section bitmap (code not shown - requires BITMAPINFO setup)
    /// // ... create bitmap, select into memDC, draw your content ...
    /// 
    /// // Update the window with your custom rendered content
    /// PlatformHelper.UpdateLayeredWindowAlpha(memDC, 800, 600, 255);
    /// 
    /// // Clean up
    /// DeleteDC(memDC);
    /// ReleaseDC(IntPtr.Zero, screenDC);
    /// </code>
    /// 
    /// Advanced Use Cases:
    /// - Irregularly shaped windows (circles, custom shapes)
    /// - Smooth gradients and anti-aliased transparency
    /// - Custom window chrome with translucent effects
    /// - Real-time video overlay with alpha channel
    /// - Complex UI with varying transparency regions
    /// 
    /// Performance Notes:
    /// - Call this method sparingly (only when visual content changes)
    /// - Pre-render bitmaps when possible
    /// - Consider caching rendered frames for animations
    /// - Large windows with frequent updates may impact performance
    /// 
    /// Integration with Rendering Frameworks:
    /// Works well with SkiaSharp, Direct2D, or custom GDI+ rendering:
    /// - SkiaSharp: Render to SKBitmap, get HDC from native handle
    /// - Direct2D: Render to compatible bitmap, obtain HDC
    /// - GDI+: Draw to Graphics object backed by compatible bitmap
    /// </remarks>
    public static bool UpdateLayeredWindowAlpha(IntPtr hdcSrc, int width, int height, byte alpha)
    {
      var mauiWindow = App.Current?.Windows[0];
      if (mauiWindow == null) return false;

      IntPtr hwnd = WindowNative.GetWindowHandle(mauiWindow.Handler.PlatformView);

      POINT ptSrc = new() { x = 0, y = 0 };
      POINT ptDst = new() { x = 0, y = 0 };
      SIZE size = new() { cx = width, cy = height };

      BLENDFUNCTION blend = new()
      {
        BlendOp = 0,  // AC_SRC_OVER
        BlendFlags = 0,
        SourceConstantAlpha = alpha,
        AlphaFormat = 1  // AC_SRC_ALPHA
      };

      return UpdateLayeredWindow(hwnd, IntPtr.Zero, ref ptDst, ref size, hdcSrc, ref ptSrc, 0, ref blend, ULW_ALPHA);
    }

    /// <summary>
    /// High-level convenience method that enables transparency with different modes and optional effects.
    /// Provides a simple interface to the most common transparency configurations without requiring 
    /// multiple method calls or understanding of low-level details.
    /// </summary>
    /// <param name="mode">The transparency mode to enable. Each mode offers different capabilities and use cases.</param>
    /// <param name="opacity">The opacity level (0-255) for modes that support uniform transparency. 
    /// Ignored for PerPixelAlpha mode which requires custom rendering.</param>
    /// <returns>True if the requested transparency mode was successfully enabled, false otherwise.</returns>
    /// <remarks>
    /// Operation: Acts as a facade that configures the appropriate combination of transparency settings 
    /// based on the selected mode. Internally calls the appropriate specialized methods in the correct order.
    /// 
    /// Transparency Modes Explained:
    /// 
    /// 1. LayeredWindow (Simple uniform transparency):
    ///    - Window has same transparency level everywhere
    ///    - Fast and simple to use
    ///    - Good for: fade effects, ghost windows, semi-transparent overlays
    ///    - Use case: Notification popup that's 80% opaque
    /// 
    /// 2. GlassExtended (Aero Glass effect):
    ///    - Classic Windows Vista/7 glass blur effect
    ///    - Blurs content behind transparent areas
    ///    - Requires transparent UI elements to see effect
    ///    - Good for: modern glass-style interfaces, blur backgrounds
    ///    - Use case: Settings window with frosted glass background
    /// 
    /// 3. ClickThrough (Interactive overlay):
    ///    - Mouse clicks pass through transparent areas
    ///    - Only opaque content receives input
    ///    - Essential for overlay applications
    ///    - Good for: screen annotations, HUDs, drawing tools
    ///    - Use case: Drawing overlay that only captures clicks on ink strokes
    /// 
    /// 4. PerPixelAlpha (Advanced custom rendering):
    ///    - Enables per-pixel alpha mode (preparation step)
    ///    - Requires using UpdateLayeredWindowAlpha() to provide bitmap
    ///    - Maximum flexibility and control
    ///    - Good for: irregular windows, complex transparency, custom chrome
    ///    - Use case: Circular window or custom-shaped application
    /// 
    /// Usage Examples:
    /// <code>
    /// // Simple semi-transparent window
    /// PlatformHelper.EnableFullTransparency(TransparencyMode.LayeredWindow, 200);
    /// 
    /// // Glass effect window (combine with transparent XAML backgrounds)
    /// PlatformHelper.EnableFullTransparency(TransparencyMode.GlassExtended, 220);
    /// 
    /// // Screen overlay for annotations
    /// PlatformHelper.EnableFullTransparency(TransparencyMode.ClickThrough, 0);
    /// // Now draw your annotations - only they will capture clicks
    /// 
    /// // Custom rendering with per-pixel alpha
    /// PlatformHelper.EnableFullTransparency(TransparencyMode.PerPixelAlpha);
    /// // Now use UpdateLayeredWindowAlpha() with your custom bitmap
    /// </code>
    /// 
    /// Mode Selection Guide:
    /// - Need uniform transparency? → LayeredWindow
    /// - Want blur effect? → GlassExtended  
    /// - Building an overlay? → ClickThrough
    /// - Need complex shapes/transparency? → PerPixelAlpha
    /// 
    /// Note: PerPixelAlpha mode only sets up the window - you must call UpdateLayeredWindowAlpha() 
    /// separately with your rendered bitmap to actually display content.
    /// </remarks>
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

  /// <summary>
  /// Defines the available transparency modes for window rendering.
  /// Each mode offers different capabilities and trade-offs for transparency effects.
  /// </summary>
  public enum TransparencyMode
  {
    /// <summary>
    /// Standard layered window with uniform opacity control across the entire window.
    /// Simple and performant. Use for fade effects and basic semi-transparent windows.
    /// Transparency level is controlled by the opacity parameter.
    /// </summary>
    LayeredWindow,

    /// <summary>
    /// Extends the Aero Glass effect into the client area with blur behind transparent regions.
    /// Creates the classic Windows Vista/7 glass appearance. Requires transparent XAML backgrounds
    /// to see the blur effect. Combines glass frame extension with layered window transparency.
    /// </summary>
    GlassExtended,

    /// <summary>
    /// Enables click-through behavior where mouse events pass through transparent areas to windows beneath.
    /// Essential for overlay applications like screen annotations, HUDs, or drawing tools.
    /// Only opaque UI elements will receive mouse input; transparent areas are ignored.
    /// </summary>
    ClickThrough,

    /// <summary>
    /// Advanced mode enabling per-pixel alpha control through custom bitmap rendering.
    /// Requires manual bitmap creation and use of UpdateLayeredWindowAlpha() method.
    /// Provides maximum flexibility for irregular window shapes, anti-aliased transparency,
    /// gradients, and custom-rendered interfaces. Most complex but most powerful option.
    /// </summary>
    PerPixelAlpha
  }
}
#endif