using Server.API;
using Server.Config;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.ContentLibrary;
using Cookie.Logging;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Server
{
    public class Program
    {

        public static Configurator config = new Configurator();

        public static Controller<Program> InitializeServer()
        {
            config.LoadingTask.Wait();

            var b = new Controller<Program>(new Program());
            // setup a provider
            Library library = new("S:/");

            Task.Run(async () =>
            {
                if (!File.Exists(library.GetLibraryCacheFile))
                {
                    Searcher s = new(library.RootPath);
                    await s.Enumerate(2, library);
                    library.StoreCache();
                }
            });

            LibraryProvider provider = new LibraryProvider(library);
            b.SetProvider(provider.Provider);
            var loginManager = new SimpleLoginManager<Program>(b);
            b.SetLoginManager(loginManager);

            // Set up the API
            b.Discover<Login>();
            var files = b.Discover<Files>();
            files!.Provider = provider;

            // And start the server
            b.ProvideStandardWebserver();
            return b.StartHttp(config.Port);

        }






    }


}
