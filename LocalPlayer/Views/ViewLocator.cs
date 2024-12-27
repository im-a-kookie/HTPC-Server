using LocalPlayer.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalPlayer.Views
{
    public class ViewLocator : IViewLocator
    {
        public IViewFor ResolveView<T>(T? viewModel, string? contract = null) => viewModel switch
        {
            PlayerViewModel context => new PlayerView { DataContext = context },
            MainViewModel context => new MainView { DataContext = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }

}
