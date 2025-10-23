using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class NavigationModel : ReactiveObject, IActivatableViewModel
{
    public NavigationModel()
    {
        // Configuration of CounterButtonText and ButtonClickedCommand
    }
    
    // [Reactive] public int Count { get; set; }
    // [ObservableAsProperty] public string CounterButtonText { get; }
    // public ReactiveCommand<Unit,Unit> ButtonClickedCommand { get; }

    public ViewModelActivator Activator => throw new NotImplementedException();

    // ...
}