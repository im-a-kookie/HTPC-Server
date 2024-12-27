using Server.API;
using Server.Config;
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
using System.Text.Json;

namespace Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var controller = Task.Run(() => Server.ServerLibrary.Program.InitializeServer());


            while (true)
            {
                if(Console.ReadLine() == "exit")
                {
                    break;
                }



            }


            controller.Wait();
            var c = controller.Result;
            var task = c.SignalCloseServer();
            try
            {
                if (!task.IsCompleted) task.Wait();
            }
            catch { }
        }
    }


}
