using Cookie.Logging;
using Cookie.Utils;
using System.IO;
using System.Net;
using System.Text;

namespace Cookie.Connections
{
    public class Response
    {
        static string STUB = "Cookie.Connections.Stubs.";

        /// <summary>
        /// A simple struct for the start/end of file content
        /// </summary>
        public struct FileRange
        {
            public long Start;
            public long End;
            public FileRange(long start, long end) { this.Start = start; this.End = end; }
            public FileRange()
            {
                this.Start = 0;
                this.End = -1;
            }
        }


        /// <summary>
        /// The headers in this response
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = [];

        /// <summary>
        /// The type of data in this response
        /// </summary>
        public string DataType { get; set; } = "text/html";

        /// <summary>
        /// The raw data in this response
        /// </summary>
        public byte[] ResponseData { get; set; } = [];

        /// <summary>
        /// The cookies to provide in this response
        /// </summary>
        private Dictionary<string, System.Net.Cookie> Cookies { get; set; } = new();

        /// <summary>
        /// The HTTP Status code result
        /// </summary>
        private HttpStatusCode Result { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// The path to the file, or null if no file is set
        /// </summary>
        public string? Filepath { get; set; } = null;

        public FileRange? RequestedRange { get; set; } = null;

        public Request? initialRequest = null;

        /// <summary>
        /// Creates a new response to the given request
        /// </summary>
        /// <param name="request"></param>
        public Response(Request? request = null)
        {
            if (request != null)
                RequestedRange = request.GetRange();

            initialRequest = request;
        }

        /// <summary>
        /// Sets the HTTP status code of this response
        /// </summary>
        /// <param name="httpStatusCode"></param>
        public Response SetResult(HttpStatusCode httpStatusCode)
        {
            Result = httpStatusCode;
            return this;

        }


        /// <summary>
        /// Configures this as a NotFound request
        /// </summary>
        /// <param name="api"></param>
        public Response NotFound(bool api = false)
        {
            Headers.Clear();
            AddUserAgent();

            SetResult(HttpStatusCode.NotFound);
            string target = "NotFound." + (api ? "json" : "html");
            DataType = MimeHelper.GetFromFile(target)!;
            ResponseData = Encoding.UTF8.GetBytes(ResourceTool.GetResource(STUB + target)!);
            return this;

        }

        /// <summary>
        /// Configures this as a BadRequest
        /// </summary>
        /// <param name="api"></param>
        public Response BadRequest(bool api = false)
        {
            Headers.Clear();
            AddUserAgent();
            //
            SetResult(HttpStatusCode.BadRequest);
            string target = "BadRequest." + (api ? "json" : "html");
            DataType = MimeHelper.GetFromFile(target)!;
            ResponseData = Encoding.UTF8.GetBytes(ResourceTool.GetResource(STUB + target)!);
            return this;

        }

        /// <summary>
        /// Configures this as an Unauthorized response
        /// </summary>
        /// <param name="api"></param>
        public Response NotAuthorized(bool api = true)
        {
            Headers.Clear();
            AddUserAgent();

            SetResult(HttpStatusCode.Unauthorized);
            string target = "NotAuthorized." + (api ? "json" : "html");
            DataType = MimeHelper.GetFromFile(target)!;
            ResponseData = Encoding.UTF8.GetBytes(ResourceTool.GetResource(STUB + target)!);
            return this;

        }

        /// <summary>
        /// Configures this as a redirect
        /// </summary>
        /// <param name="target"></param>
        public Response Redirect(string target)
        {
            Headers.Clear();
            AddUserAgent();
            SetResult(HttpStatusCode.Redirect);
            string resource = "Redirect.html";
            DataType = MimeHelper.GetFromFile(resource)!;
            Headers["Location"] = target;
            ResponseData = Encoding.UTF8.GetBytes(ResourceTool.GetResource(STUB + resource)!);
            return this;

        }


        /// <summary>
        /// Sets a string into the response
        /// </summary>
        /// <param name="data"></param>
        public Response SetString(string data)
        {
            DataType = MimeHelper.GetFromExtension(".txt")!;
            ResponseData = Encoding.UTF8.GetBytes(data);
            return this;

        }

        /// <summary>
        /// Sets the response with json content
        /// </summary>
        /// <param name="json"></param>
        public Response SetJson(string json)
        {
            DataType = MimeHelper.GetFromExtension(".json")!;
            ResponseData = Encoding.UTF8.GetBytes(json);
            return this;
        }

        /// <summary>
        /// Sets the response with json content
        /// </summary>
        /// <param name="json"></param>
        public Response SetSuccessJson()
        {
            Result = HttpStatusCode.OK;
            DataType = MimeHelper.GetFromExtension(".json")!;
            ResponseData = Encoding.UTF8.GetBytes("{\"success\":true}");
            return this;
        }

        /// <summary>
        /// Sets the response with json content
        /// </summary>
        /// <param name="json"></param>
        public Response SetFailJson()
        {
            Result = HttpStatusCode.BadRequest;
            DataType = MimeHelper.GetFromExtension(".json")!;
            ResponseData = Encoding.UTF8.GetBytes("{\"success\":false}");
            return this;

        }

        /// <summary>
        /// Sets the HTML content
        /// </summary>
        /// <param name="html"></param>
        public Response SetHtml(string html)
        {
            DataType = MimeHelper.GetFromExtension(".html")!;
            ResponseData = Encoding.UTF8.GetBytes(html);
            return this;

        }

        /// <summary>
        /// Sets this response from the given data byte array
        /// </summary>
        /// <param name="data"></param>
        public Response SetBytes(byte[] data)
        {
            DataType = MimeHelper.GetFromExtension(".bin")!;
            ResponseData = data;
            return this;

        }

        /// <summary>
        /// Sets this response from the given file
        /// </summary>
        /// <param name="path"></param>
        public Response SetFile(string path)
        {
            DataType = MimeHelper.GetFromFile(path) ?? "text/html";
            ResponseData = [];
            Filepath = path;
            return this;

        }


        /// <summary>
        /// Sets this response from the given file
        /// </summary>
        /// <param name="path"></param>
        public Response InjectFile(string path)
        {
            DataType = MimeHelper.GetFromFile(path) ?? "text/html";
            ResponseData = File.ReadAllBytes(path);
            return this;

        }

        /// <summary>
        /// Adds a generic useragent string
        /// </summary>
        public Response AddUserAgent()
        {
            Headers["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
            return this;

        }

        /// <summary>
        /// Provides a cookie into this response
        /// </summary>
        /// <param name="cookie"></param>
        public Response AddCookie(System.Net.Cookie cookie)
        {
            Cookies[cookie.Name] = cookie;
            return this;
        }

        /// <summary>
        /// Gets the header as a string
        /// </summary>
        /// <returns></returns>
        public string GetHeader()
        {
            Headers["Content-Type"] = DataType;
            Headers["Content-Length"] = ResponseData.Length.ToString();

            StringBuilder sb = new();
            sb.AppendLine($"HTTP/1.1 {(int)Result} {Result.ToString()}");

            foreach (var header in Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }

            foreach (var cookie in Cookies)
            {
                sb.AppendLine($"Set-Cookie: " + cookie.Value.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes the header to the stream
        /// </summary>
        /// <param name="stream"></param>
        private void WriteHeader(Stream stream)
        {
            // write the first part
            using var w = new StreamWriter(stream, leaveOpen: true);
            w.WriteLine($"HTTP/1.1 {(int)Result} {Result}");

            w.WriteLine($"Content-Type: {DataType}");

            // Write the headers we know
            foreach (var header in Headers)
            {
                w.WriteLine($"{header.Key}: {header.Value}");
            }
            // and the cookies
            foreach (var cookie in Cookies)
            {
                w.WriteLine($"Set-Cookie: " + cookie.Value.ToString());
            }
        }

        /// <summary>
        /// Writes the header to the stream
        /// </summary>
        /// <param name="stream"></param>
        private async Task WriteHeaderAsync(Stream stream, bool writeWhitespace = false)
        {
            // write the first part
            using var w = new StreamWriter(stream, leaveOpen: true);

            await w.WriteLineAsync($"HTTP/1.1 {(int)Result} {Result}");
            await w.WriteLineAsync($"Content-Type: {DataType}");

            List<Task> tasks = new(Headers.Count + Cookies.Count);

            // Write the headers we know
            foreach (var header in Headers)
            {
                tasks.Add(w.WriteLineAsync($"{header.Key}: {header.Value}"));
            }
            // and the cookies
            foreach (var cookie in Cookies)
            {
                
                

                var sb = new StringBuilder();
                sb.Append($"{cookie.Value.ToString()}; ");
                sb.Append($"Path={cookie.Value.Path}; ");
                sb.Append($"Domain={cookie.Value.Domain}; ");
                if(cookie.Value.Secure) sb.Append($"Secure; ");
                if (cookie.Value.HttpOnly) sb.Append($"HttpOnly; ");
                var str = sb.ToString();
                tasks.Add(w.WriteLineAsync($"Set-Cookie: " + str.Trim()));
            }

            //now wait
            await Task.WhenAll(tasks);

            if (writeWhitespace)
                await w.WriteLineAsync();

        }

        /// <summary>
        /// Writes the data in this response to the stream.
        /// </summary>
        /// <param name="stream"></param>
        public void WriteData(Stream stream)
        {
            if (Filepath != null)
            {
                StreamFile(stream, Filepath);
            }
            else
            {
                // add the space
                Headers["Content-Length"] = ResponseData.Length.ToString();
                WriteHeader(stream);
                stream.Write(Encoding.UTF8.GetBytes("\r\n"));
                stream.Write(ResponseData);
            }
        }

        /// <summary>
        /// Writes the data in this response asynchronously to the stream
        /// </summary>
        /// <param name="stream"></param>
        public async Task WriteDataAsync(Stream stream)
        {
            // write the header
            if (Filepath != null)
            {
                await StreamFileAsync(stream, Filepath);
            }
            else
            {
                // add the space
                Headers["Content-Length"] = ResponseData.Length.ToString();
                await WriteHeaderAsync(stream);
                await stream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"));
                await stream.WriteAsync(ResponseData);
                
            }
        }

        /// <summary>
        /// Streams a file from the given path
        /// </summary>
        /// <param name="path"></param>
        private void StreamFile(Stream networkStream, string path)
        {

            // First things first, let's open the file
            Stream? content;
            try
            {
                content = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                content = null;
            }

            // ensure the stream is good
            if (content == null)
            {
                NotFound();
                return;
            }

            // now we can stream it
            StreamFileAsync(networkStream, content, MimeHelper.GetFromFile(path)!).Wait();
        }


        /// <summary>
        /// Streams a file from the given path
        /// </summary>
        /// <param name="path"></param>
        private async Task StreamFileAsync(Stream networkStream, string path)
        {

            // First things first, let's open the file
            Stream? content;
            try
            {
                content = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                content = null;
            }

            // ensure the stream is good
            if (content == null)
            {
                NotFound();
                return;
            }

            // now we can stream it
            try
            {
                await StreamFileAsync(networkStream, content, MimeHelper.GetFromFile(path)!);

            }
            catch
            {

            }
            finally
            {
                if (content != null) content.Dispose();
            }
        }



        /// <summary>
        /// Streams a file via a provided stream
        /// </summary>
        /// <param name="networkStream"></param>
        private async Task<long> StreamFileAsync(Stream networkStream, Stream content, string mime)
        {
            //Get the range parameters, if requested

            var _range = GetFileRanges(content);

            if (_range == null)
            {
                BadRequest();
                return -1;
            }
            var range = _range.Value;

            await WriteHeaderAsync(networkStream, true);

            // Seek if possible. We have already validated the possibility of this,
            // But we don't want to try and seek a non-supporting stream
            if (content.CanSeek) content.Seek(range.Start, SeekOrigin.Begin);

            // Read into a buffer and write the buffer into the stream

            byte[] buffer = new byte[content.Length < 4096 ? 4096 : 1024 * 1024];
            long bytesToRead = range.End - range.Start;
            long bytesReadTotal = 0;

            // Now write this into the underlying stream
            try
            {
                while (bytesToRead > 0)
                {
                    // Read a bunch of bytes at a time
                    int bytesToReadNow = (int)Math.Min(buffer.Length, bytesToRead);
                    int bytesRead = await content.ReadAsync(buffer.AsMemory(0, bytesToReadNow));
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    //and write them
                    await networkStream!.WriteAsync(buffer.AsMemory(0, bytesRead));
                    bytesToRead -= bytesRead;
                    bytesReadTotal += bytesRead;
                }
            }
            catch (Exception)
            {
                return bytesReadTotal;
            }

            return bytesReadTotal;
        }

        /// <summary>
        /// Gets the requested file ranges and establishes headers accordingly
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public FileRange? GetFileRanges(Stream stream)
        {

            var range = RequestedRange ?? new(-1, -1);
            long fileLength = stream.Length;

            // Calculate the length, and parameters, properly
            long start = range.Start < 0 ? 0 : range.Start;
            long end = range.End < 0 ? fileLength : range.End;

            if (start < 0 || end > fileLength || start > end)
            {
                //bad request
                return null;
            }

            // Validate that we can position the stream to the requested start
            if (start != 0 && !stream.CanSeek && stream.Position != 0)
            {
                // bad request
                return null;
            }

            if (end - start > 1024 * 1024 * 32 && range.Start != -1)
            {
                var r = initialRequest;
                end = start + 1024 * 1024 * 32;
                end = long.Min(end, fileLength - 1);
            }
            // If the content is partial, then
            // We need to mark it as such
            if (start > 0 || end < fileLength)
            {
                Result = HttpStatusCode.PartialContent;
                Headers.Add("Content-Range", $"bytes {start}-{end - 1}/{fileLength}");
                Headers.Add("Content-Length", $"{end - start}");
                //write the file headers
                Headers.Add("Accept-Ranges", "bytes");
            }
            // Otherwise we can just return "OK"
            else
            {
                Headers.Add("Content-Length", $"{fileLength}");

                Result = HttpStatusCode.OK;
            }


            return new(start, end);
        }

    }
}
