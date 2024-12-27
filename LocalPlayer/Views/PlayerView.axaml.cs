using Avalonia.ReactiveUI;
using LocalPlayer.ViewModels;
using LibVLCSharp.Avalonia;
using Avalonia.Controls;
using LibVLCSharp.Shared;
namespace LocalPlayer.Views
{
    public partial class PlayerView :  ReactiveUserControl<PlayerViewModel>
    {
        public PlayerView()
        {
            InitializeComponent();
        }
    }
}
