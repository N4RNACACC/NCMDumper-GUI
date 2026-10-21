using CommunityToolkit.Mvvm.ComponentModel;

namespace NCMDumperGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}