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
    private IDrawingTool? _currentTool;

    public ToolbarView()
    {
        InitializeComponent();
        // Set default tool to Freehand on initialization
        Loaded += OnToolbarLoaded;
    }

    private void OnToolbarLoaded(object sender, EventArgs e)
    {
        // Set default tool to Freehand and update visual state
        SelectFreehandTool();
        UpdateToolButtonStates();
    }


    #region Primary Toolbar Actions

    private void OnSelectButtonClicked(object sender, EventArgs e)
    {
        SelectTool<SelectTool>();
        UpdateToolButtonStates();
    }

    private void OnBrushButtonClicked(object sender, EventArgs e)
    {
        SelectFreehandTool();
        UpdateToolButtonStates();
    }

    private void OnEraserButtonClicked(object sender, EventArgs e)
    {
        SelectTool<EraserTool>();
        UpdateToolButtonStates();
    }

    private void OnShapesButtonClicked(object sender, EventArgs e)
    {
        // Toggle shapes flyout visibility
        ShapesFlyout.IsVisible = !ShapesFlyout.IsVisible;
    }

    private void OnFillButtonClicked(object sender, EventArgs e)
    {
        SelectTool<FillTool>();
        UpdateToolButtonStates();
    }

    #endregion

    #region Shape Tool Actions

    private void OnRectangleButtonClicked(object sender, EventArgs e)
    {
        SelectTool<RectangleTool>();
        ShapesFlyout.IsVisible = false;
        UpdateToolButtonStates();
    }

    private void OnCircleButtonClicked(object sender, EventArgs e)
    {
        SelectTool<EllipseTool>();
        ShapesFlyout.IsVisible = false;
        UpdateToolButtonStates();
    }

    private void OnLineButtonClicked(object sender, EventArgs e)
    {
        SelectTool<LineTool>();
        ShapesFlyout.IsVisible = false;
        UpdateToolButtonStates();
    }

    #endregion

    #region Tool Selection Logic

    private void SelectFreehandTool()
    {
        SelectTool<FreehandTool>();
        // Keep default stroke color (no forced color override)
    }

    private void SelectTool<T>() where T : IDrawingTool, new()
    {
        if (this.BindingContext is not ToolbarViewModel vm) return;

        var tool = vm.AvailableTools.OfType<T>().FirstOrDefault();
        if (tool == null)
        {
            tool = new T();
        }

        _currentTool = tool;
        
        if (vm.SelectToolCommand != null)
        {
            vm.SelectToolCommand.Execute(tool).Subscribe();
        }
    }

    private void UpdateToolButtonStates()
    {
        // Reset all tool button styles to default
        ResetButtonStyle(SelectButton);
        ResetButtonStyle(BrushButton);
        ResetButtonStyle(EraserButton);
        ResetButtonStyle(FillButton);
        ResetButtonStyle(RectangleButton);
        ResetButtonStyle(CircleButton);
        ResetButtonStyle(LineButton);

        // Highlight the active tool button
        if (_currentTool != null)
        {
            if (_currentTool is SelectTool)
                HighlightButton(SelectButton);
            else if (_currentTool is FreehandTool)
                HighlightButton(BrushButton);
            else if (_currentTool is EraserTool)
                HighlightButton(EraserButton);
            else if (_currentTool is FillTool)
                HighlightButton(FillButton);
            else if (_currentTool is RectangleTool)
                HighlightButton(RectangleButton);
            else if (_currentTool is EllipseTool)
                HighlightButton(CircleButton);
            else if (_currentTool is LineTool)
                HighlightButton(LineButton);
        }
    }

    private void HighlightButton(Button button)
    {
        button.BackgroundColor = Colors.Orange;
        button.TextColor = Colors.White;
        button.BorderColor = Colors.DarkOrange;
        button.BorderWidth = 3;
    }

    private void ResetButtonStyle(Button button)
    {
        // Reset to default style values
        button.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Light 
            ? Colors.LightGray 
            : Colors.DarkGray;
        button.TextColor = Application.Current?.RequestedTheme == AppTheme.Light 
            ? Colors.Black 
            : Colors.White;
        button.BorderWidth = 0;
    }

    #endregion

    #region Legacy Tool Selection (kept for compatibility)

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

    #endregion
}
