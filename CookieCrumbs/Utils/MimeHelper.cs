namespace Cookie.Utils
{
    public class MimeHelper
    {
        private static Dictionary<string, string> _mimeTypes = [];

        /// <summary>
        /// Create the mime helper and insert the encrypted file forms
        /// </summary>
        static MimeHelper()
        {

            string resourceName = "CookieCrumbs.Utils.mimes.txt"; // Update this with the correct namespace and file name
            var content = ResourceTool.GetResource(resourceName);
            //make sure we actually found it
            if (content == null)
            {
                throw new FileNotFoundException("Internal MIME type data not found!");
            }
            foreach (string line in content.Split("\n"))
            {
                var parts = line.Split(",");
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    _mimeTypes.TryAdd(key, value);
                }
            }
        }

        /// <summary>
        /// Gets the MIME type from the given file extension
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="mime"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        public static bool GetFromExtension(string ext, out string? mime)
        {
            if (!ext.StartsWith(".")) ext = "." + ext;
            return _mimeTypes.TryGetValue(ext.ToLower(), out mime);
        }

        /// <summary>
        /// Gets the MIME type from the given file extension
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="mime"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        public static string? GetFromExtension(string ext)
        {
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (_mimeTypes.TryGetValue(ext.ToLower(), out var mime)) return mime;
            return null;
        }


        /// <summary>
        /// Gets the MIME type from the given path
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="mime"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        public static bool GetFromFile(string file, out string? mime)
        {
            string s = Path.GetExtension(file).ToLower();
            return _mimeTypes.TryGetValue(s, out mime);
        }

        /// <summary>
        /// Gets the MIME type from the given path
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="mime"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        public static string? GetFromFile(string file)
        {
            string s = Path.GetExtension(file).ToLower();
            if (_mimeTypes.TryGetValue(s, out var mime)) return mime;
            return null;
        }


    }
}
