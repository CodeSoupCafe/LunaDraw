using CommunityToolkit.Maui;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Services;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Pages;
using Microsoft.Extensions.Logging;

using ReactiveUI;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Splat;

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
			});

        // Register Core State Managers
        builder.Services.AddSingleton<IMessageBus>(MessageBus.Current);
        builder.Services.AddSingleton<NavigationModel>();
        builder.Services.AddSingleton<SelectionManager>();
        builder.Services.AddSingleton<IToolStateManager, ToolStateManager>();
        builder.Services.AddSingleton<ILayerStateManager, LayerStateManager>();
        
        // Register Logic Services
        builder.Services.AddSingleton<ICanvasInputHandler, CanvasInputHandler>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ToolbarViewModel>();

        // Register Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
