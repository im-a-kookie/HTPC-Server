using Cookie.Connections.API.Logins;
using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Config
{
    public class Configurator
    {
        public Task LoadingTask;

        public int Port { get; private set; } = 12345;

        public LoginData DefaultUser = new();

        /// <summary>
        /// Creates and loads configuration settings for the application
        /// </summary>
        public Configurator()
        {
            LoadingTask = Task.Run(async () =>
            {
                await RunConfigurations();
            });
        }

        /// <summary>
        /// Runs all application configurations
        /// </summary>
        private Task RunConfigurations()
        {
            return Task.WhenAll(
                LoadServerSettings(),
                RunCryptoConfiguration(),
                LoadDefaultUserSettings()
            );
        }

        /// <summary>
        /// Loads the settings for the server
        /// </summary>
        private async Task LoadServerSettings()
        {
            string? data = null;
            try
            {
                data = await File.ReadAllTextAsync("/Config/server.json");
            }
            catch
            { }
            if (data == null) return;
            
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(data!);
            if (dict == null) return;

            if (dict["Port"] is int i)
            {
                Port = i;
            }
        }

        /// <summary>
        /// Loads the settings for the server
        /// </summary>
        private async Task LoadDefaultUserSettings()
        {
            string? data = null;
            try
            {
                data = await File.ReadAllTextAsync("/Config/login.json");
            }
            catch
            { }
            if (data == null) return;

            var DefaultUser = JsonSerializer.Deserialize<LoginData>(data!);
         
        }


        /// <summary>
        /// Setup default application cryptography key stuff
        /// </summary>
        private static async Task RunCryptoConfiguration()
        {
            try
            {

                string? data = null;
                try
                {
                    data = await File.ReadAllTextAsync("/Config/crypto.json");
                }catch
                { }

                // load the key out of the encryption configuration json
                data ??= "{\"application_aes_key\":\"Default Aes Application Key!\"}";
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(data!);
                string? key = "";
                if(dict == null || !dict.TryGetValue("application_aes_key", out key))
                {
                    key = "Default AES Application Key!";
                }
                CryptoHelper.DefaultKey = () => key;
            }
            catch
            {
                var key = "Default AES Application Key!";
                var b = Encoding.UTF8.GetBytes(key).ToBase128();
                CryptoHelper.DefaultKey = () => Encoding.UTF8.GetString(b.ToBytesBase128());
            }
        }






    }
}
