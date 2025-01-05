using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using LocalPlayer.ViewModels;

namespace LocalPlayer.Views;

public partial class MainView :  ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        
    }



}
