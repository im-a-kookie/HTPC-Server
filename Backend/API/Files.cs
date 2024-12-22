using Backend.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.ContentLibrary;
using Cookie.Logging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Backend.API
{
    [Route("content", "Endpoint for accessing the HTPC content")]
    public partial class Files
    {
#pragma warning disable CS8618 // null
        public LibraryProvider Provider;
#pragma warning restore CS8618 




        public AutoResetEvent InFlightRefresh = new AutoResetEvent(true);

        [Route("refresh", "Refreshes the entire content library from the disk")]
        public Response Refresh(User? user)
        {
            Logger.Info("Refreshing");
            if(user == null) return new Response().NotAuthorized();
            if(InFlightRefresh.WaitOne(TimeSpan.FromSeconds(0.5)))
            {
                try
                {
                    var searcher = new Searcher(Provider.ProvidedLibrary.RootPath);
                    Provider.ProvidedLibrary = searcher.Enumerate();
                }
                catch { }
                finally
                {
                    InFlightRefresh.Set();
                }

            }
            return new Response()
                .SetJson("{\"success\":true}")
                .SetResult(System.Net.HttpStatusCode.OK);
        }


        [Route("cover", "Gets the cover for a file from the given query")]
        public Response GetCover(User? user, string query)
        {
            Logger.Info("Getting cover");

            if (user == null) return new Response().NotAuthorized();

            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            // see if we can get the cover
            foreach(var k in new string[] { ".jpg", ".png"})
            {
                var coverPath = Provider.ProvidedLibrary.RootPath + "/__library_cache/__covers/" + testKey + k;
                if (File.Exists(coverPath))
                    return new Response().SetFile(coverPath);
            }
            return new Response().NotFound();
        }


        [Route("details", "Gets the details of a title by the given identifier")]
        public Response GetDetails(User? user, string query)
        {
            Logger.Info("Getting details");

            if (user == null) return new Response().NotAuthorized();

            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            if(Provider.ProvidedLibrary.FoundSeries.TryGetValue(testKey, out var series))
            {
                return new Response()
                    .SetJson(new Title.Packer(series).Serialize());
            }

            return new Response().NotFound();
        }


        [Route("video", "The main endpoint for retrieving a video file")]
        public Response GetVideoFile(User? user, string query)
        {

            Logger.Info("Getting video file");

            if (user == null) return new Response().NotAuthorized();

            var queryParts = query.Split(';');
            foreach(var str in queryParts)
            {
                // see if we have file= or just v_...
                var bits = str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (bits.Count() == 1 && bits[0].StartsWith("v_"))
                {
                    // we have a prefixed path directly in the query
                    var url = Provider.GetPath(bits[0]);
                    if (url != null) return new Response().SetFile(url);
                }
                else if (bits.Count() >= 2)
                {
                    if (bits[0].ToLower().Trim() == "file")
                    {
                        // The path in the query requested file=path
                        if (File.Exists(bits[1])) return new Response().SetFile(bits[1]);
                        var url = Provider.GetPath(bits[1]);
                        if (url != null) return new Response().SetFile(url);
                    }
                }
            }

           
            // didn't find it
            return new Response().NotFound();

        }

    }
}
