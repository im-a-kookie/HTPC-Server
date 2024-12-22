using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Config
{
    internal class Configurator
    {

        public int Port { get; private set; } = 12345;

        /// <summary>
        /// Creates and loads configuration settings for the application
        /// </summary>
        public Configurator()
        {
            RunConfigurations();
        }

        /// <summary>
        /// Runs all application configurations
        /// </summary>
        private void RunConfigurations()
        {
            LoadServerSettings();
            RunCryptoConfiguration();
        }

        /// <summary>
        /// Loads the settings for the server
        /// </summary>
        private void LoadServerSettings()
        {
            var data = ResourceTool.GetResource("Backend.Config.ServerSettings.json");
            if(data == null) return;
            
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(data!);
            if (dict == null) return;

            if (dict["Port"] is int i)
            {
                Port = i;
            }

        }


        /// <summary>
        /// Setup default application cryptography key stuff
        /// </summary>
        private void RunCryptoConfiguration()
        {
            try
            {
                // load the key out of the encryption configuration json
                var data = ResourceTool.GetResource("Backend.Config.Encryption.json") ?? "{\"application_aes_key\":\"Default Aes Application Key!\"}";
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
