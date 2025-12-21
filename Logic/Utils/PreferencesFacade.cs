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

using CodeSoupCafe.Maui.Models;

namespace LunaDraw.Logic.Utils;

public enum AppPreference
{
  AppTheme,
  ShowButtonLabels,
  ShowLayersPanel,
  IsTransparentBackgroundEnabled,
  ListSortProperty,
  ListSortOrder,
  IsListGridView,
}

public class AppPreferenceDefault
{
  public dynamic this[AppPreference appPreference]
  {
    get
    {
      return appPreference switch
      {
        AppPreference.AppTheme => "Automatic",
        AppPreference.ShowButtonLabels => false,
        AppPreference.ShowLayersPanel => false,
        AppPreference.ListSortOrder => SortOrder.Descending,
        AppPreference.ListSortProperty => SortProperty.DateUpdated,
        AppPreference.IsListGridView => true,
        AppPreference.IsTransparentBackgroundEnabled => false,
        _ => ""
      };
    }
  }
}

public class PreferencesFacade : IPreferencesFacade
{
  public static AppPreferenceDefault Defaults => new();

  public string Get(AppPreference key) => Preferences.Get(key.ToString(), Defaults[key]);

  public T Get<T>(AppPreference key) => Preferences.Get(key.ToString(), Defaults[key]);

  public void Set(AppPreference key, bool value) => Preferences.Set(key.ToString(), value);

  public void Set<T>(AppPreference key, T? value) => Preferences.Set(key.ToString(), value?.ToString());
}
