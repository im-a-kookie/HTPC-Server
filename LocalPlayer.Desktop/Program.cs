using System;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Cookie.Server;

namespace LocalPlayer.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var result = Task.Run(ServerHost.InitializeServer);
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

        if(result.IsCompleted)
        {
            var server = result.Result;
            server.SignalCloseServer().Wait();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
