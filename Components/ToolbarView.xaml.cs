using LunaDraw.Logic.ViewModels;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Views;

public partial class ToolbarView : ContentView
{
    public ToolbarView()
    {
        InitializeComponent();
    }

    private void OnRectangleButtonClicked(object sender, EventArgs e)
    {
        if (BindingContext is ToolbarViewModel vm)
        {
            var tool = vm.AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool();
            vm.SelectToolCommand.Execute(tool).Subscribe();
        }
        var shapesFlyout = this.FindByName<VisualElement>("ShapesFlyout");
        if (shapesFlyout != null)
            shapesFlyout.IsVisible = false;
    }

    private void OnCircleButtonClicked(object sender, EventArgs e)
    {
        if (BindingContext is ToolbarViewModel vm)
        {
            var tool = vm.AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool();
            vm.SelectToolCommand.Execute(tool).Subscribe();
        }
        var shapesFlyout = this.FindByName<VisualElement>("ShapesFlyout");
        if (shapesFlyout != null)
            shapesFlyout.IsVisible = false;
    }

    private void OnLineButtonClicked(object sender, EventArgs e)
    {
        if (BindingContext is ToolbarViewModel vm)
        {
            var tool = vm.AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool();
            vm.SelectToolCommand.Execute(tool).Subscribe();
        }
        var shapesFlyout = this.FindByName<VisualElement>("ShapesFlyout");
        if (shapesFlyout != null)
            shapesFlyout.IsVisible = false;
    }
}