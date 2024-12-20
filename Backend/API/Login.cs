using Cookie.Connections;
using Cookie.Connections.API;
using System.Net;
using System.Text.Json;

namespace Backend.API
{
    [Route("oauth", "Login endpoints")]
    public class Login
    {

        public class LoginData
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }


        



        [Route("login", "Main login endpoint")]
        public Response LoginRequest(Request request)
        {
            // the username and password are in the request json
            // Deserialize JSON string into a C# object
            var loginData = JsonSerializer.Deserialize<LoginData>(request.RequestData);
            if (loginData == null)
            {
                return new Response().NotAuthorized();
            }

            return new Response().SetResult(HttpStatusCode.OK);
        }



        [Route("create", "Main account creation endpoint")]
        public void CreateUserRequest(Request request, string json, string arguments)
        {

            Console.WriteLine($"json: {json}, arguments: {arguments}");


        }


    }
}
