using Cookie.Server.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.ContentLibrary;
using Cookie.Logging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cookie.Server.API
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
            if(user == null) return new Response().NotAuthorized(api: true);
            if(InFlightRefresh.WaitOne(TimeSpan.FromSeconds(0.1)))
            {
                try
                {
                    var searcher = new Searcher(Provider.ProvidedLibrary.RootPath);
                    var t = searcher.Enumerate(2);
                    t.Wait();
                    Provider.ProvidedLibrary = t.Result; 
                }
                catch { }
                finally
                {
                    InFlightRefresh.Set();
                }

            }
            return new Response().SetSuccessJson();
        }


        [Route("cover", "Gets the cover for a file from the given query")]
        public Response GetCover(User? user, string query)
        {
            if (user == null) return new Response().NotAuthorized(api: true);

            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            // see if we can get the cover
            var coverPath = Provider.ProvidedLibrary.GetLibraryCoverDirectory + testKey;
            if (File.Exists(coverPath))
                return new Response().SetFile(coverPath);
            
            return new Response().SetFile("wwwroot/cover.jpg");
        }

        /// <summary>
        /// Gets a smaller thumbnail image for the given title ID
        /// </summary>
        /// <param name="user"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("thumb", "Gets the cover for a file from the given query")]
        public Response GetThumb(User? user, string query)
        {
            if (user == null) return new Response().NotAuthorized(api: true);

            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            // see if we can get the cover

            var coverPath = Provider.ProvidedLibrary.GetLibraryThumbnailDirectory + testKey;
            if (File.Exists(coverPath))
                return new Response().SetFile(coverPath);

            return new Response().SetFile("wwwroot/thumb.jpg");
        }

        /// <summary>
        /// Gets a value from a query string. e.g url?id=blah; will return "blah"
        /// </summary>
        /// <param name="query"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetKeyFromQuery(string query, string key)
        {
            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            // Find the position of the key= component of the query
            int pos = testKey.IndexOf(key + "=");
            if (pos >= 0) testKey = testKey[(pos + key.Length + 1)..];
            // and cut the section, if delimited
            pos = testKey.IndexOf(';');
            if (pos >= 0) testKey = testKey[..pos];
            
            return testKey;
        }

        [Route("details", "Gets the details of a title by the given identifier")]
        public Response GetDetails(User? user, string query)
        {
            if (user == null) return new Response().NotAuthorized(api: true);

            var testKey = GetKeyFromQuery(query, "id");
            if(Provider.ProvidedLibrary.FoundSeries.TryGetValue(testKey, out var series))
            {
                return new Response()
                    .SetJson(new Title.Packer(series).Serialize());
            }

            return new Response().NotFound();
        }

        /// <summary>
        /// Gets an html series page
        /// </summary>
        /// <param name="user"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("series", "Gets the series page for a given series")]
        public Response GetSeriesPage(User? user, string query)
        {
            if (user == null) return new Response().Redirect("/login");

            var testKey = GetKeyFromQuery(query, "id");

            if (Provider.ProvidedLibrary.FoundSeries.TryGetValue(testKey.Trim(), out var series))
            {
                try
                {
                    var file = File.ReadAllText("wwwroot/$series.html")
                        .Replace("const page_was_generated = false;", "const page_was_generated = true;")
                        .AsSpan();

                    List<string> parts = new();


                    StringBuilder builder = new();
                    var title = series.Name;
                    var pos = file.IndexOf("%TITLE%");
                    if (pos > 0)
                    {
                        builder.Append(file[..pos]);
                        builder.Append(title);
                        pos += "%TITLE%".Length;
                        file = file[pos..];
                    }

                    pos = file.IndexOf("%DESCRIPTION%");
                    if (pos > 0)
                    {
                        builder.Append(file[..pos]);
                        builder.Append(series.Description);
                        pos += "%DESCRIPTION%".Length;
                        file = file[pos..];
                    }

                    pos = file.IndexOf("%SEASONS%");
                    if (pos > 0)
                    {
                        builder.Append(file[..pos]);

                        int value = 0;

                        foreach(var season in series.Eps)
                        {
                            /*
                            <div class="season-list">
                                <!-- Season 1 -->
                                <div class="season-item">
                                    <h3>Season 1</h3>
                                    <ul>
                                        <li><a href="/episode1">Episode 1: The Beginning</a></li>
                                        <li><a href="/episode2">Episode 2: Into the Unknown</a></li>
                                    </ul>
                                </div> 
                            </div> 
                            */
                            if (series.PredictMovie)
                            {
                                builder.Append("\t\t<!-- Video Files -->");
                                builder.AppendLine($"\t\t<div class=\"season-item\" id=\'season-item-{value}\" onClick=\"clickSeasonTile({value})\">");
                                builder.AppendLine($"\t\t\t<h3>Files</h3>");
                                builder.AppendLine($"\t\t\t<div class=\"episode-list\" id=\"episode-list-{value}\">");
                                builder.AppendLine($"\t\t\t\t<ul>");
                                foreach (var episode in season.Value.Eps)
                                {
                                    if(episode.EpNo == 4095)
                                    {
                                        builder.AppendLine($"\t\t\t\t\t<li><a href=\"/player?id={episode.FileLookup}\">Click here to play.</a></li>");
                                    }
                                    else
                                    {
                                        builder.AppendLine($"\t\t\t\t\t<li><a href=\"/player?id={episode.FileLookup}\">Part {episode.EpNo}</a></li>");
                                    }
                                }
                                builder.AppendLine($"\t\t\t\t</ul>");
                                builder.AppendLine($"\t\t\t</div>");
                                builder.AppendLine($"\t\t</div>");

                            }
                            else
                            {
                                builder.AppendLine($"\t\t<!-- Season {season.Key} -->");
                                builder.AppendLine($"\t\t<div class=\"season-item\" id=\"season-item-{value}\" onClick=\"clickSeasonTile({value})\">");
                                builder.AppendLine($"\t\t\t<h3>Season {season.Key}</h3>");
                                builder.AppendLine($"\t\t\t<div class=\"episode-list\" id=\"episode-list-{value}\">");
                                builder.AppendLine($"\t\t\t\t<ul>");
                                foreach(var episode in season.Value.Eps)
                                {
                                    builder.AppendLine($"\t\t\t\t\t<li><a href=\"/player?id={episode.FileLookup}\">Episode {episode.EpNo}</a></li>");
                                }
                                builder.AppendLine($"\t\t\t\t</ul>");
                                builder.AppendLine($"\t\t\t</div>");
                                builder.AppendLine($"\t\t</div>");

                            }
                            ++value;
                        }
                        pos += "%SEASONS%".Length;
                        file = file[pos..];
                    }

                    builder.Append(file);

                    return new Response().SetHtml(builder.ToString());

                }
                catch { }
            }

            return new Response().Redirect("/library");
        }


        [Route("library", "Gets the full library as a simple condensed JSON structure")]
        public Response GetLibraryCompact(User? user, string query)
        {
            if (user == null) return new Response().NotAuthorized(api: true);
            // let's just return all of it
            StringBuilder sb = new();
            foreach(var t in Provider.ProvidedLibrary.FoundSeries)
            {
                Dictionary<string, string> data = new()
                {
                    { "id", t.Value.ID },
                    { "title", t.Value.Name },
                    { "description", (t.Value.PredictMovie ? "Movie: " : "TV: ") + t.Value.Description },
                };
                sb.Append(JsonSerializer.Serialize(data) + ",");
            }
            sb.Remove(sb.Length - 1, 1);
            return new Response().SetJson("[\n" + sb.ToString() + "\n]");
        }


        [Route("valid", "Checks if the given episode number is valid")]
        public Response CheckIDValid(User? user, string query)
        {

            if (user == null) return new Response().NotAuthorized(api: true);

            var queryParts = query.Split(';');
            foreach (var str in queryParts)
            {
                // see if we have file= or just v_...
                var bits = str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (bits.Length == 1 && int.TryParse(bits[0], out var index))
                {
                    // we have a prefixed path directly in the query
                    if (Provider.ProvidedLibrary.targetToFileMap.TryGetValue(index, out var file))
                    {
                        return new Response().SetResult(HttpStatusCode.OK).SetJson("{\"success\":true}");
                    }
                }
                else if (bits.Length >= 2)
                {
                    if (bits[0].ToLower().Trim() == "id" && int.TryParse(bits[1], out index))
                    {
                        // we have a prefixed path directly in the query
                        if (Provider.ProvidedLibrary.targetToFileMap.TryGetValue(index, out var file))
                        {
                            return new Response().SetResult(HttpStatusCode.OK).SetJson("{\"success\":true}");
                        }
                    }
                }
            }


            // didn't find it
            return new Response().NotFound();
        }



        /// <summary>
        /// Returns a response that provides a filestream to the video identified by the query.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="user"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("video", "The main endpoint for retrieving a video file")]
        public Response GetVideoFile(Request request, User? user, string query)
        {
            //if (user == null) return new Response().NotAuthorized(api: true);
            query = query.Replace(".mp4", "");
            var queryParts = query.Split(';');
            foreach(var str in queryParts)
            {
                // see if we have file= or just v_...
                var bits = str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (bits.Length == 1 && int.TryParse(bits[0], out var index))
                {
                    // we have a prefixed path directly in the query
                    if(Provider.ProvidedLibrary.targetToFileMap.TryGetValue(index, out var file))
                    {
                        return new Response(request).SetFile(file.DecompressPath(Provider.ProvidedLibrary));
                    }
                }
                else if (bits.Length >= 2)
                {
                    if (bits[0].ToLower().Trim() == "id" && int.TryParse(bits[1], out index))
                    {
                        // we have a prefixed path directly in the query
                        if (Provider.ProvidedLibrary.targetToFileMap.TryGetValue(index, out var file))
                        {
                            return new Response(request).SetFile(file.DecompressPath(Provider.ProvidedLibrary));
                        }
                    }
                }
            }
            // didn't find it
            return new Response().NotFound();

        }

    }
}
