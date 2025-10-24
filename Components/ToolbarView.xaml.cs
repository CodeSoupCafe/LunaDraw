using System;
using Microsoft.Maui.Controls;
using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Tools;
using System.Reactive;

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
}
