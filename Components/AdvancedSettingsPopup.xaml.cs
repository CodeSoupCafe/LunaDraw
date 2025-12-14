using CommunityToolkit.Maui.Views;
using LunaDraw.Logic.ViewModels;

namespace LunaDraw.Components;

public partial class AdvancedSettingsPopup : Popup
{
  public AdvancedSettingsPopup(MainViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
  }

  private void OnCloseClicked(object sender, EventArgs e)
  {
    this.CloseAsync();
  }
}
