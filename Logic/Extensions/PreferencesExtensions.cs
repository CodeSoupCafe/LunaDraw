using LunaDraw.Logic.Utils;
using SkiaSharp;

namespace LunaDraw.Logic.Extensions;

public static class PreferencesExtensions
{
  public static SKColor GetCanvasBackgroundColor(this IPreferencesFacade _)
  {
    return GetCanvasBackgroundColor(Preferences.Default);
  }

  public static SKColor GetCanvasBackgroundColor(this IPreferences preferences)
  {
    var isTransparentBackground = preferences.Get(AppPreference.IsTransparentBackgroundEnabled.ToString(), PreferencesFacade.Defaults[AppPreference.IsTransparentBackgroundEnabled]);

    if (isTransparentBackground) return SKColors.Transparent;

    var selectedTheme = Application.Current?.RequestedTheme;
    var settingTheme = preferences.Get(AppPreference.AppTheme.ToString(), PreferencesFacade.Defaults[AppPreference.AppTheme]);

    if (settingTheme != PreferencesFacade.Defaults[AppPreference.AppTheme])
    {
      selectedTheme = settingTheme == AppTheme.Dark.ToString() ? AppTheme.Dark : AppTheme.Light;
    }

    return selectedTheme == AppTheme.Dark ? SKColors.Black : SKColors.White;
  }
}
