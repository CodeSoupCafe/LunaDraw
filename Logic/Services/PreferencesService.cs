using Microsoft.Maui.Storage;

namespace LunaDraw.Logic.Services;

public class PreferencesService : IPreferencesService
{
    public bool Get(string key, bool defaultValue) => Preferences.Get(key, defaultValue);
    public void Set(string key, bool value) => Preferences.Set(key, value);
}
