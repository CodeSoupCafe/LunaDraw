using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Tools;
using System.Reactive;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace LunaDraw.Views;

public partial class ToolbarView : ContentView
{
    public ToolbarView()
    {
        InitializeComponent();
    }

    private void OnToolButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is IDrawingTool tool)
        {
            if (this.BindingContext is ToolbarViewModel vm && vm.SelectToolCommand != null)
            {
                // Execute the ReactiveCommand and subscribe to ensure execution
                vm.SelectToolCommand.Execute(tool).Subscribe();
            }
        }
    }

    private async void OnStrokeColorButtonClicked(object sender, EventArgs e)
    {
        if (this.BindingContext is not ToolbarViewModel vm) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var action = await page.DisplayActionSheet(
            "Select Stroke Color", 
            "Cancel", 
            null, 
            "Black", "White", "Red", "Green", "Blue", "Yellow", "Purple", "Orange", "Custom...");

        if (action == "Cancel") return;

        SKColor? skColor = action switch
        {
            "Black" => SKColors.Black,
            "White" => SKColors.White,
            "Red" => SKColors.Red,
            "Green" => SKColors.Green,
            "Blue" => SKColors.Blue,
            "Yellow" => SKColors.Yellow,
            "Purple" => SKColors.Purple,
            "Orange" => SKColors.Orange,
            "Custom..." => await GetCustomSKColorAsync(),
            _ => null
        };

        if (skColor.HasValue)
        {
            vm.StrokeColor = skColor.Value;
        }
    }

    private async void OnStrokeWidthButtonClicked(object sender, EventArgs e)
    {
        if (this.BindingContext is not ToolbarViewModel vm) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var action = await page.DisplayActionSheet(
            "Select Stroke Width", 
            "Cancel", 
            null, 
            "1", "2", "4", "8", "12", "16", "24", "32", "Custom...");

        if (action == "Cancel") return;

        if (action == "Custom...")
        {
            var customWidth = await page.DisplayPromptAsync(
                "Custom Width", 
                "Enter stroke width (1-50):", 
                "OK", 
                "Cancel", 
                vm.StrokeWidth.ToString(), 
                keyboard: Keyboard.Numeric);

            if (customWidth != null && double.TryParse(customWidth, out var width) && width >= 1 && width <= 50)
            {
                vm.StrokeWidth = (float)width;
            }
        }
        else if (double.TryParse(action, out var selectedWidth))
        {
            vm.StrokeWidth = (float)selectedWidth;
        }
    }

    private async void OnFillColorButtonClicked(object sender, EventArgs e)
    {
        if (this.BindingContext is not ToolbarViewModel vm) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var action = await page.DisplayActionSheet(
            "Select Fill Color", 
            "Cancel", 
            null, 
            "None", "Black", "White", "Red", "Green", "Blue", "Yellow", "Purple", "Orange", "Custom...");

        if (action == "Cancel") return;

        SKColor? skColor = action switch
        {
            "None" => SKColors.Transparent,
            "Black" => SKColors.Black,
            "White" => SKColors.White,
            "Red" => SKColors.Red,
            "Green" => SKColors.Green,
            "Blue" => SKColors.Blue,
            "Yellow" => SKColors.Yellow,
            "Purple" => SKColors.Purple,
            "Orange" => SKColors.Orange,
            "Custom..." => await GetCustomSKColorAsync(),
            _ => null
        };

        if (skColor.HasValue)
        {
            vm.FillColor = skColor.Value;
        }
    }

    private async void OnOpacityButtonClicked(object sender, EventArgs e)
    {
        if (this.BindingContext is not ToolbarViewModel vm) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var action = await page.DisplayActionSheet(
            "Select Opacity", 
            "Cancel", 
            null, 
            "0% (Transparent)", "25%", "50%", "75%", "100% (Opaque)", "Custom...");

        if (action == "Cancel") return;

        double opacity = action switch
        {
            "0% (Transparent)" => 0,
            "25%" => 64,
            "50%" => 128,
            "75%" => 191,
            "100% (Opaque)" => 255,
            "Custom..." => await GetCustomOpacityAsync(vm.Opacity),
            _ => vm.Opacity
        };

        vm.Opacity = (byte)opacity;
    }

    private async Task<SKColor?> GetCustomSKColorAsync()
    {
        var page = GetCurrentPage();
        if (page == null) return null;

        var hexColor = await page.DisplayPromptAsync(
            "Custom Color", 
            "Enter hex color (e.g., #FF0000):", 
            "OK", 
            "Cancel", 
            "#000000");

        if (hexColor != null && TryParseHexSKColor(hexColor, out var skColor))
        {
            return skColor;
        }

        return null;
    }

    private async Task<double> GetCustomOpacityAsync(double currentOpacity)
    {
        var page = GetCurrentPage();
        if (page == null) return currentOpacity;

        var opacityStr = await page.DisplayPromptAsync(
            "Custom Opacity", 
            "Enter opacity (0-255):", 
            "OK", 
            "Cancel", 
            currentOpacity.ToString(), 
            keyboard: Keyboard.Numeric);

        if (opacityStr != null && double.TryParse(opacityStr, out var opacity) && opacity >= 0 && opacity <= 255)
        {
            return opacity;
        }

        return currentOpacity;
    }

    private Page? GetCurrentPage()
    {
        // Walk up the visual tree to find the nearest Page
        Element? el = this;
        while (el != null)
        {
            if (el is Page p)
                return p;
            el = el.Parent;
        }

        // Fallback: return the first window's page if available
        try
        {
            return Application.Current?.Windows?.FirstOrDefault()?.Page;
        }
        catch
        {
            return null;
        }
    }

    private bool TryParseHexSKColor(string hex, out SKColor skColor)
    {
        skColor = SKColors.Empty;

        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.Trim();
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        try
        {
            if (hex.Length == 7) // #RRGGBB
            {
                skColor = SKColor.Parse(hex);
                return true;
            }
            else if (hex.Length == 9) // #AARRGGBB
            {
                skColor = SKColor.Parse(hex);
                return true;
            }
        }
        catch
        {
            // Invalid hex format
        }

        return false;
    }
}
