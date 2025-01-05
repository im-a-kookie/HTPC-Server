using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cookie.ContentLibrary;
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


        public Library? CurrentLibrary;

        public List<MediaFile> Playlist = [];

        public MediaFile? CurrentFile = null;

        private static string DefaultPath = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

        DispatcherTimer _timer;

        public PlayerViewModel(IScreen screen)
        {
            HostScreen = screen;
            MediaPlayer = new MediaPlayer(_libVlc);
            _timer = new();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (s, e) =>
            {
                if (Popup == null) return;
                if (TimerValue > 0)
                {
                    Popup.Opacity = 0.6 * double.Min(1, TimerValue);
                    TimerValue -= _timer.Interval.TotalSeconds;
                    if (TimerValue < 0)
                    {
                        if (Popup != null) Popup.IsVisible = false;
                    }
                }
            };

        }

        public void Start()
        {
            if (Design.IsDesignMode) return;
            

        }

        public void Stop()
        {
            if (Design.IsDesignMode) return;
            
            MediaPlayer.Stop();
        }

        public void Next() { }

        public void Back() { }

        public void PlayPause()
        {
            if (Design.IsDesignMode) return;

            try
            {
                if(MediaPlayer.Media == null)
                {
                    string url = DefaultPath;

                    // play from the local file
                    if(CurrentFile != null && CurrentLibrary != null)
                    {
                        url = CurrentFile.DecompressPath(CurrentLibrary);
                    }
                    using var media = new Media(_libVlc, new Uri(url));

                    MediaPlayer.Play(media);
                }
                // otherwise, play/pause
                else if (MediaPlayer.IsPlaying) MediaPlayer.Pause();
                else MediaPlayer.Play();
            }
            catch { }
        }

        public void SkipForwards()
        {

        }

        public void SkipBackwards()
        {

        }

        
        double TimerValue = 0;
        DockPanel? Popup = null;
        public void PointerMove(object sender, PointerEventArgs e)
        {
            Popup ??= ((Control)sender).GetControl<DockPanel>("NavigationPopup");

            var point = e.GetPosition(sender as Control);
            if(point.Y > (((Control)sender).Height * 2) / 3)
            {
                if (Popup != null)
                {
                    Popup.IsVisible = true;
                    Popup.Opacity = 0.6;
                    TimerValue = -1;
                }
            }
            else
            {
                if (Popup != null 
                    && Popup.IsVisible 
                    && TimerValue < 0) 
                    TimerValue = 3;
            }
        }

        
        public void PointerExit(object sender, PointerCaptureLostEventArgs e)
        {

        }

        public void Dispose()
        {
            MediaPlayer?.Dispose();
            _libVlc?.Dispose();
        }

    }
}
