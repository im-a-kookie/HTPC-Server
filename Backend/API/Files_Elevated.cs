using Backend.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.API
{
    /// <summary>
    /// Elevated file definitions
    /// </summary>
    public partial class Files
    {



        [Route("setpath", "Sets the path of the library in this instance to the given target, as an absolute drive/directory")]
        public Response SetPath(User user, string json)
        {
            // Requre user to be high level
            if (user == null || user.Permission.ReadLevel < Level.HIGH)
            {
                return new Response().NotAuthorized();
            }

            if (InFlightRefresh.WaitOne(50))
            {
                lock (this)
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (dict != null && dict.TryGetValue("path", out var path) && Directory.Exists(path))
                        {
                            Provider.ProvidedLibrary.RootPath = path;
                            Searcher searcher = new Searcher(path);
                            Provider.ProvidedLibrary = searcher.Enumerate();
                            return new Response()
                                .SetJson("{\"success\":true}")
                                .SetResult(HttpStatusCode.OK);
                        }
                    }
                    catch { }
                    finally
                    {
                        InFlightRefresh.Set();
                    }
                }
            }

            return new Response().BadRequest();
        }

    }
}
