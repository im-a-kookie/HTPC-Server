using Cookie.Utils;
using System.Net;
using System.Text;

namespace Cookie.TCP
{
    /// <summary>
    /// Response Sender provides core methods for sending HTTP responses over an underlying stream.
    /// 
    /// This class provides most key methods for delivering text and binary/data content.
    /// </summary>
    public partial class ResponseSender
    {
        /// <summary>
        /// Reference string for the HTML/JSON stubs for basic HTTP API response stuff
        /// </summary>
        public static string StubRoot = "CookieCrumbs.TCP.Stubs.";


        /// <summary>
        /// Internal flag indicating whether the header has been written
        /// </summary>
        private bool _writtenHeader = false;

        /// <summary>
        /// Gettable flag indicating whether the action on this response has been resolved.
        /// If this is true, then it is generally safe to close the response.
        /// </summary>
        public bool ActionCompleted { get; private set; } = false;

        /// <summary>
        /// The requested range of data, in bytes. Null if not present.
        /// </summary>
        public (int start, int end)? RequestedRange = null;

        /// <summary>
        /// HTTP status code for this response
        /// </summary>
        public HttpStatusCode Result = HttpStatusCode.OK;

        /// <summary>
        /// A collection of headers to be written into this HTTP response
        /// </summary>
        public Dictionary<string, string> Headers = new();

        /// <summary>
        /// The network stream underlying this response
        /// </summary>
        public Stream UnderlyingStream { get; private set; }

        /// <summary>
        /// Creates a new response to the given request
        /// </summary>
        /// <param name="request"></param>
        public ResponseSender(RequestReader request)
        {
            this.UnderlyingStream = request.UnderlyingStream;
            RequestedRange = request.GetRange();
        }

        /// <summary>
        /// Adds a header to this response
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddHeader(string name, string value)
        {
            if (!Headers.TryAdd(name, value))
            {
                Headers[name] = value;
            }
        }

        /// <summary>
        /// Submits the given data, appending content-length and finalizing a request.
        /// </summary>
        /// <param name="data"></param>
        internal void SubmitResource(string path)
        {
            string? content = ResourceTool.GetResource($"{StubRoot}{path}");
            if (content == null) throw new Exception($"Cannot submit: {path}. Resource not found!");
            Submit(content);
        }


        /// <summary>
        /// Submits the given data, appending content-length and finalizing a request.
        /// </summary>
        /// <param name="data"></param>
        internal void Submit(string text)
        {
            Submit(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Submits the given data, appending content-length and finalizing a request.
        /// </summary>
        /// <param name="data"></param>
        internal void Submit(byte[] data)
        {
            Write($"Content-Length: {data.Length}\r\n");
            Write("\r\n");
            Write(data);
            ActionCompleted = true;

        }

        /// <summary>
        /// Writes the given data to the stream. Internal use.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="data"></param>
        internal void Write(byte[] data)
        {
            UnderlyingStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Writes the given data to the stream. Internal use.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="data"></param>
        internal void Write(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes the headers in this HTTP response to the given stream
        /// </summary>
        /// <param name="s"></param>
        public void WriteHeaders()
        {
            if (_writtenHeader) return;
            _writtenHeader = true;

            //Let's write the results
            string HTTP = @$"HTTP/1.1 {(int)Result} {Result.ToString()}\r\n";
            Write(HTTP);
            foreach (var kv in Headers)
            {
                Write($"{kv.Key}: {kv.Value}\r\n");
            }
        }

        /// <summary>
        ///  Writes data content to the given stream
        /// </summary>
        /// <param name="s"></param>
        /// <param name="data"></param>
        public void WriteContent(byte[] data)
        {
            if (ActionCompleted) return;
            Result = HttpStatusCode.OK;
            AddHeader("Content-Type", $"{MimeHelper.GetFromExtension(".bin")}");
            Submit(data);
            ActionCompleted = true;
        }

        /// <summary>
        /// Writes HTML data to the given stream
        /// </summary>
        /// <param name="s"></param>
        /// <param name="text"></param>
        public void WriteHtml(string text)
        {
            if (ActionCompleted) return;
            Result = HttpStatusCode.OK;
            AddHeader("Content-Type", $"{MimeHelper.GetFromExtension("html")}");
            WriteHeaders();
            byte[] data = Encoding.UTF8.GetBytes(text);
            Submit(data);
            ActionCompleted = true;

        }

        /// <summary>
        /// Delivers the given file, from its local filepath
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<long> Deliver(string file)
        {
            string mime = MimeHelper.GetFromFile(file)!;
            return await Deliver(File.Open(file, FileMode.Open), MimeHelper.GetFromFile(file)!);
        }

        /// <summary>
        /// Delivers the given content stream into the underlying stream of this response
        /// </summary>
        /// <param name="content"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public async Task<long> Deliver(Stream? content, string mime)
        {
            if (_writtenHeader) return -1;
            _writtenHeader = true;

            if (content == null)
            {
                NotFound();
                return -1;
            }

            //Get the range parameters, if requested
            (int start, int end) range = RequestedRange.HasValue ? RequestedRange.Value : (-1, -1);

            // Calculate the length, and parameters, properly
            long fileLength = content.Length;
            long start = range.start < 0 ? 0 : range.start;
            long end = range.end < 0 ? fileLength - 1 : range.end;

            if (start < 0 || end >= fileLength || start > end)
            {
                //bad request
                BadRequest();
                return -1;
            }

            if (start != 0 && !content.CanSeek && content.Position != 0)
            {
                // bad request
                BadRequest();
                return -1;
            }

            // If the content is partial, then
            // We need to mark it as such
            if (start > 0 || end < fileLength - 1)
            {
                Result = HttpStatusCode.PartialContent;
                Headers.Add("Content-Range", $"bytes {start}-{end}/{fileLength}");
            }
            // Otherwise we can just return "OK"
            else Result = HttpStatusCode.OK;

            // Seek if possible. We have already validated the possibility of this,
            // But we don't want to try and seek a non-supporting stream
            if (content.CanSeek) content.Seek(start, SeekOrigin.Begin);

            // Read into a buffer and write the buffer into the stream
            byte[] buffer = new byte[8192];
            long bytesToRead = end - start + 1;
            long bytesReadTotal = 0;

            // Now write this into the underlying stream
            while (bytesToRead > 0)
            {
                // Read a bunch of bytes at a time
                int bytesToReadNow = (int)Math.Min(buffer.Length, bytesToRead);
                int bytesRead = await content.ReadAsync(buffer, 0, bytesToReadNow);
                if (bytesRead == 0)
                {
                    break;
                }
                //and write them
                UnderlyingStream?.WriteAsync(buffer, 0, bytesRead);
                bytesToRead -= bytesRead;
                bytesReadTotal += bytesRead;
            }

            return bytesReadTotal;

        }

    }
}
