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

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.LifecycleEvents;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Pages;
using Microsoft.Extensions.Logging;

using ReactiveUI;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Splat;

#if WINDOWS
using Microsoft.UI.Xaml.Media;
#endif

namespace LunaDraw;

public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    var builder = MauiApp.CreateBuilder();

    // Initialize Splat and ReactiveUI
    Locator.CurrentMutable.InitializeSplat();
    Locator.CurrentMutable.InitializeReactiveUI();

    builder
        .UseMauiApp<App>()
        .UseSkiaSharp()
        .UseMauiCommunityToolkit()
        .ConfigureFonts(fonts =>
        {
          fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
          fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        })
        .ConfigureLifecycleEvents(events =>
        {
#if WINDOWS
          events.AddWindows(wndLifeCycleBuilder =>
          {
            wndLifeCycleBuilder.OnWindowCreated(window =>
              {
                window.SystemBackdrop = new DesktopAcrylicBackdrop();
                if (Preferences.Get(AppPreference.IsTransparentBackgroundEnabled.ToString(), false))
                {
                  PlatformHelper.EnableTrueTransparency(180);   // Fully transparent
                }
              });
          });
#endif
        });

    // Register Core State Managers
    builder.Services.AddSingleton<IMessageBus>(new MessageBus());
    builder.Services.AddSingleton<NavigationModel>();
    builder.Services.AddSingleton<SelectionObserver>();
    builder.Services.AddSingleton<ILayerFacade, LayerFacade>();

    // Register Logic Services
    builder.Services.AddSingleton<ICanvasInputHandler, CanvasInputHandler>();
    builder.Services.AddSingleton<ClipboardMemento>();
    builder.Services.AddSingleton<IBitmapCache, LunaDraw.Logic.Utils.BitmapCache>();
    builder.Services.AddSingleton<IPreferencesFacade, PreferencesFacade>();
    builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
    builder.Services.AddSingleton<IDrawingStorageMomento, DrawingStorageMomento>();
    builder.Services.AddSingleton<LunaDraw.Logic.Services.IThumbnailCacheFacade, LunaDraw.Logic.Services.ThumbnailCacheFacade>();
    builder.Services.AddSingleton<IDrawingThumbnailFacade, DrawingThumbnailFacade>();

    // Register ViewModels
    builder.Services.AddSingleton<LayerPanelViewModel>();
    builder.Services.AddSingleton<SelectionViewModel>();
    builder.Services.AddSingleton<HistoryViewModel>();
    builder.Services.AddSingleton<GalleryViewModel>();
    builder.Services.AddTransient<DrawingGalleryPopupViewModel>();

    builder.Services.AddTransient<MainViewModel>();
    builder.Services.AddSingleton<ToolbarViewModel>();

    // Register Pages
    builder.Services.AddTransient<MainPage>();

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
