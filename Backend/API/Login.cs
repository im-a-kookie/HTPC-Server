using Backend.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.Logging;
using Cookie.Utils;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using static Backend.API.Login;

namespace Backend.API
{
    [Route("oauth", "Login endpoints")]
    public class Login
    {
        public ILoginManager<Program> LoginManager;
        public Controller<Program> Controller;


        /// <summary>
        /// Create a new login endpoint with the given controller
        /// </summary>
        /// <param name="controller"></param>
        public Login(Controller<Program> controller, ILoginManager<Program> login)
        {
            // setup the logging in things
            this.Controller = controller;
            LoginManager = login;
            LoginManager.Load();
            if (LoginManager.GetUserCount() <= 0)
            {
                Logger.Debug("Performing first-run setup!");
                LoginData? data = null;

                string? details = null;
                try
                {
                    details = ResourceTool.GetResource("Backend.Config.AdminLogin.json");
                    data = JsonSerializer.Deserialize<LoginData>(details!);
                    if (data == null || data.Username == null || data.Password == null) throw new NullReferenceException("Error loading AdminLogin defaults");
                }
                catch
                {
                    // generate a random user detail thing
                    var a = Random.Shared.Next(1000, 10000);
                    var b = Random.Shared.Next(10000, 100000);
                    data = new();
                    data.Username = $"_AdminUser_{a}";
                    data.Password = $"default_password_#{b}";
                    Logger.Warn($"Error loading default admin login. Ensure config/AdminConfig.Json is embedded!");
                    Logger.Warn($"Using randomized default admin login;");
                    Logger.Warn($"Username: {data.Username}");
                    Logger.Warn($"Password: {data.Password}");
                }

                // create the default user
                LoginManager.CreateUser(data!.Username!, data!.Password!, new(Level.HIGH));
                LoginManager.Save();
                Logger.Debug("Created admin user!");

            }

            Logger.Debug("Setup login features!");
        }


        [Route("login", "Attempts to log in a user." +
            "\n\rExpects: json with username/password fields." +
            "\n\rReturns: Json success bool. HTTP OK on success, HTTP Unauthorized on fail.")]
        public Response LoginRequest(string json)
        {
            // bad login input
            if(json.Length <= 0)
            {
                return new Response()
                    .SetJson("{\"success\":false}")
                    .SetResult(HttpStatusCode.Unauthorized);
            }

            // the username and password are in the request json
            // Deserialize JSON string into a C# object
            LoginData? loginData = null;
            try
            {
                loginData = JsonSerializer.Deserialize<LoginData>(json);
            }
            catch { }
            if (loginData == null)
            {
                return new Response().NotAuthorized();
            }

            // now read it out of the login manager
            var user = LoginManager.GetUser(loginData.Username ?? "", loginData.Password ?? "");
            if (user != null)
            {
                var response = new Response()
                    .SetJson("{\"success\":true}")
                    .SetResult(HttpStatusCode.OK);

                Controller!.ApplyPersistentCookie(response, user);
                return response;

            }
            // bonk
            else return new Response()
                    .SetJson("{\"success\":false}")
                    .SetResult(HttpStatusCode.Unauthorized);

        }

        [Route("signup", "Creates a new user." +
            "\n\rExpects: json with username/password fields." +
            "\n\rReturns: json success bool. HTTP OK or HTTP BadRequest.")]
        public Response CreateUserRequest(string json)
        {

            // the username and password are in the request json
            // Deserialize JSON string into a C# object
            LoginData? loginData = null;
            try
            {
                loginData = JsonSerializer.Deserialize<LoginData>(json);
            }
            catch { }

            if(loginData == null || loginData.Username == null || loginData.Password == null)
            {
                return new Response()
                    .BadRequest();
            }

            // now create a user
            var user = LoginManager.CreateUser(loginData.Username, loginData.Password, new(Level.MED));

            if(user != null)
            {
                var response = new Response()
                    .SetJson("{\"success\":true}")
                    .SetResult(HttpStatusCode.OK);
                return response;
            }

            return new Response()
                    .BadRequest();

        }


    }
}
