using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Reactive;

namespace LocalPlayer.ViewModels;

public class MainViewModel : ViewModelBase, IScreen
{
    public string Greeting => "Welcome to Avalonia!";

    public ReactiveCommand<Unit, IRoutableViewModel?> GoBack => Router.NavigateBack;
    public ReactiveCommand<Unit, IRoutableViewModel> GotoPlayerView { get; }
    public ReactiveCommand<Unit, Unit> LoadedCommand { get; }


    public static MainViewModel? CurrentInstance { get; private set; } = null;

    public RoutingState Router { get; } = new RoutingState();

    public MainViewModel()
    {
        CurrentInstance = this;
        LoadedCommand = ReactiveCommand.Create(() =>
        {
            GotoPlayerView!.Execute();
            //Logger.Info("Moving to Login View...");
        });
        GotoPlayerView = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(x => Router.Navigate.Execute(new PlayerViewModel(this)));
    }

}
