using LunaDraw.Logic.Utils;
using SkiaSharp;

namespace LunaDraw.Logic.Extensions;

public static class PreferencesExtensions
{
  public static SKColor GetCanvasBackgroundColor(this IPreferencesFacade preferencesFacade)
  {
    var isTransparentBackground = preferencesFacade.Get<bool>(AppPreference.IsTransparentBackgroundEnabled);

    if (isTransparentBackground) return SKColors.Transparent;

    var selectedTheme = Application.Current?.RequestedTheme;
    var settingTheme = preferencesFacade.Get(AppPreference.AppTheme);

    if (settingTheme != PreferencesFacade.Defaults[AppPreference.AppTheme])
    {
      selectedTheme = settingTheme == AppTheme.Dark.ToString() ? AppTheme.Dark : AppTheme.Light;
    }

    return selectedTheme == AppTheme.Dark ? SKColors.Black : SKColors.White;
  }
}
