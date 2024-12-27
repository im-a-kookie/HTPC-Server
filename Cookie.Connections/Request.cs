using Cookie.Logging;
using Cookie.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using static Cookie.Connections.Response;

namespace Cookie.Connections
{
    public class Request
    {

        /// <summary>
        /// The headers in this request
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = [];

        /// <summary>
        /// The cookies contained in this request
        /// </summary>
        public Dictionary<string, System.Net.Cookie> Cookies { get; set; } = [];

        /// <summary>
        /// The target of this request
        /// </summary>
        public string Target = "/";

        /// <summary>
        /// The parameter data that was contained in the URL target
        /// </summary>
        public string Parameters = "";

        /// <summary>
        /// The raw data in this request
        /// </summary>
        public byte[] RequestData { get; set; } = [];


        /// <summary>
        /// The HTTP Status code result
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>
        /// The type of data in this request
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

#if !BROWSER
        /// <summary>
        /// Generates a request out of a stream
        /// </summary>
        /// <param name="stream"></param>
        public Request(TcpClient client, Stream stream)
        {
            Read(client, stream);
        }
#endif
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

                // -1 can be caught to default to length
                if (b < 0) return new(0L, -1L);

                if (!long.TryParse(s.Slice(0, b), out long l0))
                    l0 = 0; // start at 0

                long l1 = -1;
                if (b < s.Length - 1)
                    long.TryParse(s.Slice(b + 1), out l1);

                return new(l0, l1);

            }
            // bonk
            return null;
        }

#if !BROWSER
        /// <summary>
        /// Reads this request out of a stream
        /// </summary>
        /// <param name="input"></param>
        public void Read(TcpClient client, Stream input)
        {
            ReadAsync(client, input).Wait();
        }

        /// <summary>
        /// Reads this request out of a stream
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task ReadAsync(TcpClient client, Stream input)
        {
            int remain = client.Available;

            while(remain <= 0)
            {
                await Task.Delay(5);
                remain = client.Available;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(16384);
            using var ms = new MemoryStream();

            // Read the entire packet into the stream
            int totalRead = 0;
            while (remain > 0)
            {
                int len = int.Min(remain, buffer.Length);
                int amt = input.Read(buffer, 0, len);

                if (amt == 0)
                {
                    // If ReadAsync returns 0, there may be no data yet; wait briefly and check again
                    await Task.Delay(5); // Small delay to allow for data flush
                    remain = client.Available; // Re-check available data after the delay
                    continue; // Retry reading
                }

                remain -= amt;
                totalRead += amt;
                ms.Write(buffer, 0, amt);

                ms.Flush();
            }
            ArrayPool<byte>.Shared.Return(buffer);
            //now move back to the start and look for the newline break
            ms.Position = 0;
            int newliners = 0;
            for(int i = 0; i <= ms.Length; i++)
            {
                var c = ms.ReadByte();
                if (c < 0) break;
                if (c == '\n' || c == '\r')
                {
                    newliners += 1;
                    if (newliners == 4) break;
                }
                else newliners = 0;
            }

            // now we have the header, let's try and read the content
            int headerLength = (int)ms.Position;
            ms.Position = 0;

            var text = new byte[headerLength];
            await ms.ReadAsync(text, 0, text.Length);

            var headerString = Encoding.UTF8.GetString(text).Trim();
            var parts = headerString.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // The first line has the HTTP information
            if(parts.Length >= 1)
            {
                var firstLine = parts[0].Trim();
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
            for (int i = 1; i < parts.Length; i++)
            {

                var read = parts[i].Trim();
                if (read.Length <= 0) continue;
                int pos = read?.IndexOf(':') ?? -1;
                if (pos > 0)
                {
                    // and doink
                    string key = read!.Remove(pos).Trim();
                    string body = read[(pos + 1)..];

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

            // Now see if we have to read any more request content
            if (ms.Length - ms.Position > 0)
            {
                RequestData = new byte[ms.Length - ms.Position];
                await ms.ReadAsync(RequestData, 0, RequestData.Length);
            }
            else RequestData = [];

        }

        /// <summary>
        /// Reads API/URL target and parameters correctly
        /// </summary>
        internal void ReadParametersFromTarget()
        {
            char[] delimiters = ['=', '?', '#', ';']; // Possible delimiters
            int[] pos = new int[delimiters.Length];

            //now find the soonest delimiter
            int lastSlashPos = Target.LastIndexOf('/');
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
        internal static System.Net.Cookie? ReadCookie(string header)
        {
            // Extract the cookie part of the Set-Cookie header (ignoring attributes like Path, Secure, etc.)
            var parts = header.Split(';');
            if (parts.Length < 1) return null;
            string namePart = parts[0].Trim();

            // Split into name and value
            int epos = namePart.IndexOf('=');
            if(epos < 0) return null;

            //now we can get the cookie parts
            string name = namePart.Remove(epos).Trim();
            string value = namePart[(epos + 1)..].Trim();

            // Create the Cookie object
            System.Net.Cookie cookie = new(name, value);

            // Parse additional cookie attributes (if any)
            string[] attributes = header.Split(';');
            foreach (var attribute in attributes)
            {
                string attrib = attribute.Trim().ToLower();
                if (attrib.StartsWith("expires="))
                {
                    cookie.Expires = DateTime.Parse(attrib[8..]);
                }
                else if (attrib == "secure")
                {
                    cookie.Secure = true;
                }
                else if (attrib.StartsWith("path="))
                {
                    cookie.Path = attrib[5..];
                }
            }

            return cookie;
        }

#endif



    }
}
