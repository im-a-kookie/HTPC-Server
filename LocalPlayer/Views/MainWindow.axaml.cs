using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using LocalPlayer.ViewModels;
using System.Reactive;

namespace LocalPlayer.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        this.Loaded += MainWindowLoaded;
        InitializeComponent();
        AvaloniaXamlLoader.Load(this);
    }

    private void MainWindowLoaded(object? sender, object e)
    {
        if (this.DataContext is MainViewModel model)
        {
            model.LoadedCommand.Execute().Subscribe(Observer.Create<Unit>((x) => { }));

            
        }
    }

}
