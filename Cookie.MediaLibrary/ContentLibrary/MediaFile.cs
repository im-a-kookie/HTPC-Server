using Cookie.Serializers;

namespace Cookie.ContentLibrary
{

    /// <summary>
    /// A file container object that describes various properties
    /// of a file, and allows it to be retrieved either locally,
    /// or from the backend
    /// </summary>
    public class MediaFile : IDictable
    {
        /// <summary>
        /// The title that owns this file
        /// </summary>
        public Title? Owner = null;

        /// <summary>
        /// The resolution of this file, or -1 if unknown
        /// </summary>
        public int Res { get; set; } = -1;

        /// <summary>
        /// The codec of this file (e.g x264, HEVC, AV1, etc)
        /// </summary>
        public string Codec { get; set; } = "";

        /// <summary>
        /// The year associated with this file (e.g 2005)
        /// </summary>
        public int Year { get; set; } = -1;

        /// <summary>
        /// The season associated with this file
        /// </summary>
        public int SNo { get; set; } = -1;

        /// <summary>
        /// The episode associated with this file
        /// </summary>
        public int EpNo { get; set; } = -1;

        /// <summary>
        /// The path to this file, represented as a filepath. If this string is
        /// empty or null, then it is assumed that <see cref="Remote"/> is
        /// configured to the remote location.
        /// 
        /// <para>If this is set and the file exists, then it is assumed that
        /// <see cref="Remote"/>, if set, represents an API request key</para>
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Decompresses this path using the given library's decompression scheme
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public string DecompressPath(Library library)
        {
            string path = Path;
            int pos = path.IndexOf('<');
            while (pos >= 0)
            {
                int epos = path.IndexOf('>');
                if (epos > 0)
                {
                    var substring = path.Substring(pos, pos - epos + 1);
                    if (library.abbreviations.TryGetValue(substring, out var abbrev))
                    {
                        path = path.Replace(substring, abbrev);
                    }
                }
                pos = path.IndexOf("<");
            }
            return path;
        }


        /// <summary>
        /// The Remote Path for this file, for communication over network.
        /// 
        /// <para>If <see cref="Path"/> is set and exists, then this is expected to be a backend
        /// value for providing files. Otherwise, this should be used to retrieve files from the backend</para>
        /// </summary>
        public string Remote { get; set; } = "";

        public void FromDictionary(IDictionary<string, object> dict)
        {
            Res = (int)dict["R"];
            Codec = (string)dict["C"];
            Year = (int)dict["Y"];
            SNo = (int)dict["S"];
            EpNo = (int)dict["E"];
            Path = (string)dict["P"];

        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["R"] = Res;
            dict["C"] = Codec;
            dict["Y"] = Year;
            dict["S"] = SNo;
            dict["E"] = EpNo;
            dict["P"] = Path;
            dict["M"] = Remote;
        }
    }
}
