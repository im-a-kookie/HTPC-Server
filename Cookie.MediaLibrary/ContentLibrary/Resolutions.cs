using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.ContentLibrary
{
    public class Resolution(string res, int dim)
    {
        public string res = res;
        public int dim = dim;
        public int index = -1;
    }

    public static class Resolutions
    {

        /// <summary>
        /// The internal list of resolution values.
        /// </summary>
        public static List<Resolution> Values = [
            new("<>", -1),
            new("720p", 720),
            new("1080p", 1080),
            new("2160p", 2160),
            new("4K", 2160),
            new("480p", 480),
            new("144p", 480),
            new("360p", 360),
            new("1440p", 1440)
        ];

        static Resolutions()
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
                if (file.Contains(res.res)) return res.index;
            }
            return 0;
        }

        /// <summary>
        /// Matches a resolution from a width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int Match(Size size)
        {
            return Match(size.Width, size.Height);
        }

        /// <summary>
        /// Matches a resolution from a width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int Match(int width, int height)
        {
            double bestDistance = double.MaxValue;
            int bestIndex = 0;
            foreach(var res in Values)
            {
                if (res.dim < 0) continue;
                if (res.dim > (1.15 * height)) continue;
                double dist = double.Abs(height - res.dim);
                if(dist < bestDistance)
                {
                    bestIndex = res.index;
                    bestDistance = dist;
                }
            }
            return bestIndex;
        }

        /// <summary>
        /// Gets the dimension value for the resolution at the given index
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static int GetSize(int resolution)
        {
            if (resolution > 0 && resolution < Values.Count)
            {
                return Values[resolution].dim;
            }
            else return -1;
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
                return Values[resolution].res;
            }
            else return "Unknown";
        }

    }


}


