using Cookie.Serializers;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

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
        /// An integer that maps some tags for this media file
        /// </summary>
        public long Data = 0;

        /// <summary>
        /// The resolution of this file, or -1 if unknown
        /// </summary>
        public int Res { 
            get
            {
                return (int)(Data & 0xF);
            }
            set
            {
                Data = (Data & 0x7FFFFFFFFFFFFFF0) | (long)(value & 0xF);
            }
        }

        /// <summary>
        /// The codec of this file, 0 if unknown
        /// </summary>
        public int Codec
        {
            get
            {
                return (int)(Data & 0xF0) >> 4;
            }
            set
            {
                Data = (Data & 0x7FFFFFFFFFFFFF0F) | ((long)((value & 0xF) << 4));
            }
        }

        /// <summary>
        /// The year associated with this file (e.g 2005)
        /// </summary>
        public int Year
        {
            get
            {
                return 1800 + (int)((Data & 0xFFF00) >> 8);
            }
            set
            {
                Data = ((Data & 0x7FFFFFFF000FFF) | (((long.Clamp(value, 1800, 2200) - 1800) << 8)));
            }
        }

        /// <summary>
        /// The season associated with this file
        /// </summary>

        /// <summary>
        /// The year associated with this file (e.g 2005)
        /// </summary>
        public int SNo
        {
            get
            {
                return (int)((Data & 0xFF00000) >> 20);
            }
            set
            {
                Data = ((Data & 0xFF00000) | ((long)(value & 0xFF) << 20));
            }
        }

        /// <summary>
        /// The episode associated with this file
        /// </summary>
        public int EpNo
        {
            get
            {
                return (int)((Data & 0xFFF0000000) >> 28);
            }
            set
            {
                Data = ((Data & 0xFFF0000000) | ((long)(value & 0xFFF) << 28));
            }
        }

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
            int pos = path.IndexOf('?');
            while (pos >= 0)
            {
                int epos = pos + 1;
                char next = path[epos];
                int val = (int)next - 0x0020;
                if(val < library.abbreviations.Count && val >= 0)
                {
                    path = path.Replace($"?{next}", library.abbreviations[val]);
                }
                pos = path.IndexOf('?');
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
            Data = (long)dict["D"];
            Path = (string)dict["P"];
        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["D"] = Data;
            dict["P"] = Path;
        }
    }
}
