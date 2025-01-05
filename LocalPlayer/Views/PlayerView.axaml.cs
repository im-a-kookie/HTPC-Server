using Avalonia.ReactiveUI;
using LocalPlayer.ViewModels;
using LibVLCSharp.Avalonia;
using Avalonia.Controls;
using LibVLCSharp.Shared;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using Cookie.Logging;
using System;
using Avalonia.Threading;
using System.Diagnostics.Contracts;
namespace LocalPlayer.Views
{
    public partial class PlayerView :  ReactiveUserControl<PlayerViewModel>
    {

        const double FadeInTime = 0.3;
        const double FadeOutTime = 1;
        const double FadeOutDelay = 3;
        const double IdleDelay = 6;
        const double TargetOpacity = 1;

        bool Captured = false;

        double TimerValue = 0;
        DockPanel? Popup = null;
        DispatcherTimer _timer;
        PointerPoint previous;

        ~PlayerView()
        {
            _timer.Stop();
        }
        
        public PlayerView()
        {
            InitializeComponent();
            _timer = new();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += (s, e) =>
            {
                if (Popup == null) return;

                if (TimerValue > 0)
                {
                    TimerValue -= _timer.Interval.TotalSeconds;
                    if(TimerValue < FadeOutTime)
                    {
                        Popup.Opacity = double.Clamp(TargetOpacity * TimerValue / FadeOutTime, 0, TargetOpacity);
                    }

                    if(TimerValue < 0)
                    {
                        Popup.Opacity = 0;
                        TimerValue = 0;
                    }
                }

                if(Captured && TimerValue > FadeOutTime)
                {
                    if (Popup.Opacity < TargetOpacity) Popup.Opacity = double.Clamp(Popup.Opacity + _timer.Interval.TotalSeconds / FadeInTime, 0, TargetOpacity);
                }

            };

            _timer.Start();
        }

        public void PointerExitEvent(object sender, PointerEventArgs e)
        {
            TimerValue = FadeOutDelay + FadeOutTime;
            Captured = false;
            Logger.Info("Pointer Left by " + sender);

        }

        public void PointerMoveEvent(object sender, PointerEventArgs e)
        {
            Popup ??= this.GetControl<DockPanel>("NavigationPopup");
            Captured = true;
            var point = e.GetCurrentPoint(this);
            if(point != previous)
            {
                previous = point;
                TimerValue = IdleDelay;
            }
        }

    }
}
