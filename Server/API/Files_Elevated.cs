using Cookie.Server.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using Cookie.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cookie.Server.API
{
    /// <summary>
    /// Elevated file definitions
    /// </summary>
    public partial class Files
    {

        public class Packer
        {
            public string? ID { get; set; }
            public string? Name { get; set; }
            public string ?Description { get; set; }
            public string? CoverUrl { get; set; }
            public string? ThumbUrl { get; set; }
            public string? Year { get; set; }
            public string[]? Genres { get; set; }
        }


        [Route("set_details", "Sets details for the given series")]
        public Response SetDetails(User? user, string query, string json)
        {
            // Requre user to be high level
            if (user == null || user.Permission.ReadLevel < Level.HIGH)
            {
                //return new Response().NotAuthorized();
            }

            // see if we can get it yessssss
            var testKey = query.Trim().Replace(" ", "_").ToLowerInvariant();

            int pos = testKey.IndexOf("id=");
            if (pos >= 0)
            {
                testKey = testKey[(pos + 3)..];
            }

            pos = testKey.IndexOf(';');
            if (pos >= 0)
            {
                testKey = testKey[..pos];
            }

            Packer? data = JsonSerializer.Deserialize<Packer>(json);

            if (testKey.Length < 8)
            {
                testKey = data?.ID ?? null;
            }

            if(testKey != null && data != null)
            {
                if (Provider.ProvidedLibrary.FoundSeries.TryGetValue(testKey, out var series))
                {
                    if (data.Name != null) series.Name = data.Name;
                    if (data.Description != null) series.Description = data.Description;
                    //...
                    if(data.CoverUrl != null)
                    {
                        //calculate the correct cover location
                        string path = Provider.ProvidedLibrary.GetLibraryCoverDirectory;
                        if(data.CoverUrl.EndsWith(".jpg"))
                        {
                            Directory.CreateDirectory(path);
                            var t = LoadCoverFromUrl(data.CoverUrl, path + testKey + ".jpg");
                            t.Wait();
                        }
                        else if (data.CoverUrl.EndsWith(".png"))
                        {
                            Directory.CreateDirectory(path);
                            var t = LoadCoverFromUrl(data.CoverUrl, path + testKey + ".png");
                            t.Wait();
                        }
                    }

                    if (data.ThumbUrl != null)
                    {
                        //calculate the correct cover location
                        string path = Provider.ProvidedLibrary.GetLibraryThumbnailDirectory;
                        if (data.ThumbUrl.EndsWith(".jpg"))
                        {
                            Directory.CreateDirectory(path);
                            var t = LoadCoverFromUrl(data.ThumbUrl, path + testKey + ".jpg");
                            t.Wait();
                        }
                        else if (data.ThumbUrl.EndsWith(".png"))
                        {
                            Directory.CreateDirectory(path);
                            var t = LoadCoverFromUrl(data.ThumbUrl, path + testKey + ".png");
                            t.Wait();
                        }
                    }

                }
            }


            return new Response().BadRequest();
        }


        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Loads a cover image from a URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static async Task LoadCoverFromUrl(string url, string targetPath)
        {
            // Check if the URL is a local file (starts with "file://")
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = url.Substring(7); // Remove "file://" prefix
                await LoadLocalFile(filePath, targetPath);
            }
            // Otherwise, treat it as a remote file (HTTP/HTTPS)
            else
            {
                await LoadRemoteFile(url, targetPath);
            }
        }

        /// <summary>
        /// Loads a local file from a URL/path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        private static async Task LoadLocalFile(string filePath, string targetPath)
        {
            try
            {
                await Task.Run(() => File.Copy(filePath, targetPath));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading local file: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a file from a remote URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        private static async Task LoadRemoteFile(string url, string targetPath)
        {
            try
            {
                // Send HTTP request to get the file content
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Will throw if not successful

                File.Delete(targetPath);
                File.Create(targetPath);
                using var f = File.OpenWrite(targetPath);
                await response.Content.ReadAsStream().CopyToAsync(f);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading remote file: {ex.Message}");
            }
        }


        [Route("setpath", "Sets the path of the library in this instance to the given target, as an absolute drive/directory")]
        public Response SetPath(User? user, string json)
        {
            // Requre user to be high level
            if (user == null || user.Permission.ReadLevel < Level.HIGH)
            {
                return new Response().NotAuthorized(api: true);
            }

            // now attempt to handle the 
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
                            var t = searcher.Enumerate();
                            t.Wait();
                            Provider.ProvidedLibrary = t.Result;
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
            else
                return new Response()
                    .BadRequest()
                    .SetResult(HttpStatusCode.TooManyRequests);

            return new Response()
                .BadRequest();
        }

    }
}
