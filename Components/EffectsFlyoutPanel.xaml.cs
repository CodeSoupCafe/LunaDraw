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

using LunaDraw.Logic.ViewModels;
using SkiaSharp;

namespace LunaDraw.Components;

/// <summary>
/// Popover panel for brush effects including scatter, jitter, and magic effects (Rainbow, Glow).
/// </summary>
public partial class EffectsFlyoutPanel : ContentView
{
  public EffectsFlyoutPanel()
  {
    InitializeComponent();
  }

  protected override void OnBindingContextChanged()
  {
    base.OnBindingContextChanged();

    if (BindingContext is ToolbarViewModel viewModel)
    {
      ScatterRadiusSlider.Value = viewModel.ScatterRadius;
      SizeJitterSlider.Value = viewModel.SizeJitter;
      AngleJitterSlider.Value = viewModel.AngleJitter;
      HueJitterSlider.Value = viewModel.HueJitter;
      RainbowSwitch.IsToggled = viewModel.IsRainbowEnabled;
      GlowSwitch.IsToggled = viewModel.IsGlowEnabled;
      GlowRadiusSlider.Value = viewModel.GlowRadius;
    }
  }

  private void OnRainbowSwitchToggled(object? sender, ToggledEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.IsRainbowEnabled = e.Value;
    }
  }

  private void OnGlowSwitchToggled(object? sender, ToggledEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.IsGlowEnabled = e.Value;
    }
  }

  private void OnGlowRadiusChanged(object? sender, ValueChangedEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.GlowRadius = (float)e.NewValue;
    }
  }

  private void OnGlowColorTapped(object? sender, TappedEventArgs e)
  {
    if (e.Parameter is string colorHex && BindingContext is ToolbarViewModel viewModel)
    {
      if (SKColor.TryParse(colorHex, out var skColor))
      {
        viewModel.GlowColor = skColor;
      }
    }
  }

  private void OnScatterRadiusChanged(object? sender, ValueChangedEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.ScatterRadius = (float)e.NewValue;
    }
  }

  private void OnSizeJitterChanged(object? sender, ValueChangedEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.SizeJitter = (float)e.NewValue;
    }
  }

  private void OnAngleJitterChanged(object? sender, ValueChangedEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.AngleJitter = (float)e.NewValue;
    }
  }

  private void OnHueJitterChanged(object? sender, ValueChangedEventArgs e)
  {
    if (BindingContext is ToolbarViewModel viewModel)
    {
      viewModel.HueJitter = (float)e.NewValue;
    }
  }
}
