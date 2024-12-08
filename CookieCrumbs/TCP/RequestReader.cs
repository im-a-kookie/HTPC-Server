namespace CookieCrumbs.TCP
{
    /// <summary>
    /// Provides a summary of information parsed from an incoming network stream. Allows pairing with
    /// <see cref="ResponseSender"/> to simplify HTTP over a raw TCP connection.
    /// </summary>
    internal class RequestReader
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public string Target = "";

        public Dictionary<string, string> headers = [];

        /// <summary>
        /// The underlying network stream that provides requests and responses
        /// </summary>
        public Stream UnderlyingStream;

        /// <summary>
        /// Gets the integer bounds of the requested Content-Range or Range header
        /// </summary>
        /// <returns></returns>
        public (int start, int end)? GetRange()
        {
            string? value = null;
            if (headers.TryGetValue("Content-Range", out value) || headers.TryGetValue("Range", out value))
            {
                var parts = value.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0].StartsWith("byte")
                    && int.TryParse(parts[1], out int start)
                    && int.TryParse(parts[2].Split('/')[0], out int end))
                {
                    return (start, end);
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new request reader on the given stream
        /// </summary>
        /// <param name="inputStream"></param>
        public RequestReader(Stream stream)
        {
            this.UnderlyingStream = stream;
            using var inputStream = new StreamReader(stream, leaveOpen: true);
            Read(inputStream);
            // This leaves the stream at the end of the header body
            // So normal reading can resume post-haste
        }

        public async void Read(StreamReader inputStream)
        {
            var firstLine = await inputStream.ReadLineAsync();
            if (firstLine != null)
            {
                // Read the components
                int pos = firstLine.IndexOf(' ');
                var method = firstLine.Remove(pos);

                // Now read to the HTTP section
                int httpPos = firstLine.IndexOf("HTTP");
                Target = firstLine.Substring(pos, httpPos).Trim();

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
                if (read?.Trim().Length <= 0) break;
                int pos = read?.IndexOf(':') ?? -1;
                if (pos > 0)
                {
                    // and doink
                    string key = read!.Remove(pos);
                    string body = read.Substring(pos + 1);
                    headers.Add(key, body.Trim());
                }

            }
        }

    }


}
