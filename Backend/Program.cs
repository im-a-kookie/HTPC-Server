using Backend.API;
using Backend.Config;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.ContentLibrary;
using Cookie.Logging;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using Cookie.Serializing;
using System;
using System.Diagnostics;
using System.Net;

namespace Backend.ServerLibrary
{
    public class Program
    {
        public static JsonSerialization localSerializer = new();
        public static JsonSerialization remoteSerializer = new();

        



        public static void Main(string[] args)
        {
            Configurator config = new Configurator();


            var b = new Controller<Program>(new Program());

            // setup a provider
            Library library = new("S:/");
            LibraryProvider provider = new LibraryProvider(library);
            b.SetProvider(provider.Provider);
            var loginManager = new SimpleLoginManager<Program>(b);
            b.SetLoginManager(loginManager);

            // Set up the API
            b.Discover<Login>();
            b.Discover<Files>()!.Provider = provider;

            // And start the server
            b.ProvideStandardWebserver();
            b.StartHttp(config.Port);


            var cookie = new System.Net.Cookie("LoginToken", loginManager.NameUsers.First().Value.GenerateToken(TimeSpan.FromDays(1)));

            // Create a CookieContainer and add a cookie
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri($"http://127.0.0.1:{config.Port}/"), cookie);

            // Create HttpClientHandler with the container
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };
            using HttpClient http = new(handler);

            while (true)
            {
                var str = Console.ReadLine();

                if (str == "refresh")
                {
                    var task = Task.Run(() => http.GetAsync($"http://127.0.0.1:{config.Port}/content/refresh"));
                    task.Wait();
                    var response = task.Result;
                    Logger.Info($"Response: {response.StatusCode}");
                }
            }


        }






    }


}
