﻿using CookieCrumbs.Serializing;

namespace CookieCrumbs.ContentLibrary
{

    /// <summary>
    /// A file container object that describes various properties
    /// of a file, and allows it to be retrieved either locally,
    /// or from the backend
    /// </summary>
    public class MediaFile : ICanJson
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
        /// The Remote Path for this file, for communication over network.
        /// 
        /// <para>If <see cref="Path"/> is set and exists, then this is expected to be a backend
        /// value for providing files. Otherwise, this should be used to retrieve files from the backend</para>
        /// </summary>
        public string Remote { get; set; } = "";

        public string GetTargetIdentifier(SerializationEngine engine)
        {
            return "File";
        }
    }
}
