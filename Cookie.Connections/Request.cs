using Cookie.Logging;
using Cookie.Utils;
using System.Text;
using System.Text.Json.Nodes;
using static Cookie.Connections.Response;

namespace Cookie.Connections
{
    public class Request
    {

        /// <summary>
        /// The headers in this response
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// The target of this request
        /// </summary>
        public string Target = "/";

        /// <summary>
        /// The parameter data that was contained in the URL target
        /// </summary>
        public string Parameters = "";

        /// <summary>
        /// The raw data in this response
        /// </summary>
        public byte[] RequestData { get; set; } = [];

        /// <summary>
        /// The cookies to provide in this response
        /// </summary>
        private Dictionary<string, System.Net.Cookie> Cookies { get; set; } = new();

        /// <summary>
        /// The HTTP Status code result
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>
        /// The type of data in this response
        /// </summary>
        public string DataType { get; set; } = "text/html";

        /// <summary>
        /// Builds a new HTTP Request to the given target
        /// </summary>
        /// <param name="target"></param>
        public Request(string target = "/")
        {
            this.Method = HttpMethod.Get;
            this.Target = target;
        }

        /// <summary>
        /// Builds a new HTTP request with the given REST method and target path
        /// </summary>
        /// <param name="method"></param>
        /// <param name="target"></param>
        public Request(HttpMethod method, string target = "/")
        {
            this.Method = method;
            this.Target = target;
        }

        /// <summary>
        /// Generates a request out of a stream
        /// </summary>
        /// <param name="stream"></param>
        public Request(Stream stream)
        {
            Read(stream);
        }

        /// <summary>
        /// Generates an empty/default request
        /// </summary>
        public Request()
        {
            // meh
        }

        /// <summary>
        /// Sets the HTTP status code of this response
        /// </summary>
        /// <param name="httpStatusCode"></param>
        public void SetMethod(HttpMethod httpMethod)
        {
            Method = httpMethod;
        }

        /// <summary>
        /// Asks for Json content
        /// </summary>
        public void AcceptJson()
        {
            Headers["Accept"] = MimeHelper.GetFromExtension(".json")!;
        }

        /// <summary>
        /// Asks for HTML content
        /// </summary>
        public void AcceptHTML()
        {
            Headers["Accept"] = MimeHelper.GetFromExtension(".html")!;
        }

        /// <summary>
        /// Accepts any content type (default)
        /// </summary>
        public void AcceptAny()
        {
            Headers["Accept"] = "*/*"!;
        }

        /// <summary>
        /// Sets a string into the response
        /// </summary>
        /// <param name="data"></param>
        public void SetString(string data)
        {
            DataType = MimeHelper.GetFromExtension(".txt")!;
            RequestData = Encoding.UTF8.GetBytes(data);
        }

        /// <summary>
        /// Sets the response with json content
        /// </summary>
        /// <param name="json"></param>
        public void SetJson(string json)
        {
            DataType = MimeHelper.GetFromExtension(".json")!;
            RequestData = Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Sets the response with json content
        /// </summary>
        /// <param name="json"></param>
        public void SetJson(JsonObject json)
        {
            DataType = MimeHelper.GetFromExtension(".json")!;
            RequestData = Encoding.UTF8.GetBytes(json.ToString());
        }

        /// <summary>
        /// Sets the HTML content
        /// </summary>
        /// <param name="html"></param>
        public void SetHtml(string html)
        {
            DataType = MimeHelper.GetFromExtension(".html")!;
            RequestData = Encoding.UTF8.GetBytes(html);
        }

        /// <summary>
        /// Sets this response from the given data byte array
        /// </summary>
        /// <param name="data"></param>
        public void SetBytes(byte[] data)
        {
            DataType = MimeHelper.GetFromExtension(".bin")!;
            RequestData = data;
        }

        /// <summary>
        /// Adds a generic useragent string
        /// </summary>
        public void AddUserAgent()
        {
            Headers["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
        }

        /// <summary>
        /// Provides a cookie into this response
        /// </summary>
        /// <param name="cookie"></param>
        public void AddCookie(System.Net.Cookie cookie)
        {
            Cookies[cookie.Name] = cookie;
        }

        /// <summary>
        /// Gets the integer bounds of the requested Content-Range or Range header
        /// </summary>
        /// <returns></returns>
        public FileRange? GetRange()
        {
            string? value = null;
            if (Headers.TryGetValue("Range", out value))
            {
                // Get the things after the "=" since it can exist (e.g bytes=)
                int a = value.IndexOf('=');
                var s = value.AsSpan();
                if (a > 0) s = s.Slice(a + 1);
                int b = s.IndexOf('-');

                long l0 = 0;
                // -1 can be caught to default to length
                if (b < 0) return new(0L, -1L);

                if (!long.TryParse(s.Slice(0, b), out l0))
                    l0 = 0; // start at 0

                long l1 = -1;
                if (b < s.Length - 1)
                    long.TryParse(s.Slice(b + 1), out l1);

                return new(l0, l1);

            }
            // bonk
            return null;
        }

        /// <summary>
        /// Reads this request out of a stream
        /// </summary>
        /// <param name="input"></param>
        public void Read(Stream input)
        {
            ReadAsync(input).Wait();
        }

        /// <summary>
        /// Reads this request out of a stream
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task ReadAsync(Stream input)
        {
            using var inputStream = new StreamReader(input, leaveOpen: true);

            var firstLine = await inputStream.ReadLineAsync();
            if (firstLine != null)
            {
                // Read the components
                int pos = firstLine.IndexOf(' ');
                var method = firstLine.Remove(pos);
                // Now read to the HTTP section
                int httpPos = firstLine.IndexOf("HTTP");
                Target = firstLine.Substring(pos, httpPos - pos).Trim();
                // clean it
                ReadParametersFromTarget();

                // Select the HTTP method
                if (pos > 0)
                {
                    var start = firstLine.Remove(pos);
                    switch (start.ToLower())
                    {
                        case "get":
                            Method = HttpMethod.Get;
                            break;
                        case "post":
                            Method = HttpMethod.Post;
                            break;
                        case "put ":
                            Method = HttpMethod.Put;
                            break;
                        case "delete":
                            Method = HttpMethod.Delete;
                            break;
                    }
                }
            }
            // Now read the request lines until we get to the end of the header
            while (true)
            {
                var read = await inputStream.ReadLineAsync();
                Logger.Debug(read);
                if (read?.Trim().Length <= 0)
                    break;
                int pos = read?.IndexOf(':') ?? -1;
                if (pos > 0)
                {
                    // and doink
                    string key = read!.Remove(pos);
                    string body = read.Substring(pos + 1);

                    // read the cookies
                    if (key == "Cookie" || key == "Set-Cookie")
                    {
                        var c = ReadCookie(body.Trim());
                        if (c != null)
                            Cookies[c.Name] = c;
                    }
                    else
                    {
                        Headers[key] = body.Trim();
                    }
                    //Console.WriteLine(key + ": " + body);
                }

            }

            if (Method != HttpMethod.Get)
            {
                var bodyData = inputStream.ReadToEnd().Trim();
                RequestData = Encoding.UTF8.GetBytes(bodyData);
            }
            else RequestData = [];

        }

        /// <summary>
        /// Reads API/URL target and parameters correctly
        /// </summary>
        internal void ReadParametersFromTarget()
        {
            char[] delimiters = { '=', '?', '#', ';' }; // Possible delimiters
            int[] pos = new int[delimiters.Length];

            //now find the soonest delimiter
            int lastSlashPos = Target.LastIndexOf("/");
            if (lastSlashPos < 0) lastSlashPos = 0;
            for (int i = 0; i < delimiters.Length; i++)
            {
                pos[i] = Target.IndexOf(delimiters[i], lastSlashPos);
                if (pos[i] < 0) pos[i] = int.MaxValue;
            }

            // Now cut it
            var min = pos.Min();
            if (min < int.MaxValue && min > 0)
            {
                Parameters = Target.Substring(min + 1);
                Target = Target.Remove(min);
            }
        }

        /// <summary>
        /// Internal method to process cookie out of a given header (specifically, after the Cookie: or Set-Cookie:)
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        internal System.Net.Cookie? ReadCookie(string header)
        {
            // Extract the cookie part of the Set-Cookie header (ignoring attributes like Path, Secure, etc.)
            var parts = header.Split(';');
            if (parts.Length == 1) return null;
            string namePart = parts[0].Trim();

            // Split into name and value
            var cookieParts = namePart.Split('=');
            if (cookieParts.Length != 2) return null;

            //now we can get the cookie parts
            string name = cookieParts[0].Trim();
            string value = cookieParts[1].Trim();

            // Create the Cookie object
            System.Net.Cookie cookie = new System.Net.Cookie(name, value);

            // Parse additional cookie attributes (if any)
            string[] attributes = header.Split(';');
            foreach (var attribute in attributes)
            {
                string attrib = attribute.Trim().ToLower();
                if (attrib.StartsWith("expires="))
                {
                    cookie.Expires = DateTime.Parse(attrib.Substring(8));
                }
                else if (attrib == "secure")
                {
                    cookie.Secure = true;
                }
                else if (attrib.StartsWith("path="))
                {
                    cookie.Path = attrib.Substring(5);
                }
            }

            return cookie;
        }






    }
}
