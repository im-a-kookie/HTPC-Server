using Avalonia.Controls;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalPlayer.ViewModels
{
    public class PlayerViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        public string? UrlPathSegment => "player";

        public IScreen HostScreen { get; set; }


        private readonly LibVLC _libVlc = new LibVLC();

        public MediaPlayer MediaPlayer { get; }


        public PlayerViewModel(IScreen screen)
        {
            HostScreen = screen;
            MediaPlayer = new MediaPlayer(_libVlc);


        }

        public void Play()
        {
            if (Design.IsDesignMode)
            {
                return;
            }

            using var media = new Media(_libVlc, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            MediaPlayer.Play(media);
        }

        public void Stop()
        {
            MediaPlayer.Stop();
        }

        public void Dispose()
        {
            MediaPlayer?.Dispose();
            _libVlc?.Dispose();
        }

    }
}
