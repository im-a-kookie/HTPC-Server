using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.ContentLibrary
{
    public class Codec
    {
        public string code = "";
        public int index = -1;
        public Codec(string code)
        {
            this.code = code;
        }
    }

    public static class Codecs
    {

        /// <summary>
        /// The internal list of resolution values.
        /// </summary>
        public static List<Codec> Values = [
            new("<>"),
            new("X264"),
            new("X265"),
            new("VP9"),
            new("AV1"),
        ];

        static Codecs()
        {
            for (int i = 0; i < Values.Count; i++) Values[i].index = i;
        }

        /// <summary>
        /// Matches the resolution from string to index
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static int Match(string file)
        {
            foreach(var res in Values)
            {
                if (file.Contains(res.code)) return res.index;
            }
            return 0;
        }


        /// <summary>
        /// Gets the string name for the resolution at the given index
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static string GetName(int resolution)
        {
            if (resolution > 0 && resolution < Values.Count)
            {
                return Values[resolution].code;
            }
            else return "Unknown";
        }

    }


}


