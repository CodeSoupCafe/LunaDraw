using LunaDraw.Logic.Services;
using SkiaSharp;

namespace LunaDraw.Logic.Extensions;

public static class PreferencesExtensions
{
    public static SKColor GetCanvasBackgroundColor(this IPreferencesFacade preferences, bool isTransparentBackground)
    {
        if (isTransparentBackground) return SKColors.Transparent;

        var selectedTheme = Application.Current?.RequestedTheme;
        var settingTheme = preferences.Get(AppPreference.AppTheme);

        if (settingTheme != PreferencesFacade.Defaults[AppPreference.AppTheme])
        {
            selectedTheme = settingTheme == AppTheme.Dark.ToString() ? AppTheme.Dark : AppTheme.Light;
        }

        return selectedTheme == AppTheme.Dark ? SKColors.Black : SKColors.White;
    }
}
