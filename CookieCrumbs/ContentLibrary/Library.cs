using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CookieCrumbs.ContentLibrary
{
    public class Library
    {

        public delegate void SeriesUpdated(Library library, List<Title> affectedTitles);

        /// <summary>
        ///  An event triggered whenever a series is updated, providing the affected library and a list of affected titles.
        /// </summary>
        public event SeriesUpdated? OnSeriesUpdate;


        /// <summary>
        ///  An event triggered whenever a series is updated, providing the affected library and a list of affected titles.
        /// </summary>
        public event SeriesUpdated? OnSeriesDeleted;

        /// <summary>
        /// An enumerable lookup of all series that have been found by this series library
        /// </summary>
        public ConcurrentDictionary<string, Title> FoundSeries = [];


        /// <summary>
        /// Cleans the title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string CleanTitle(string title)
        {
            title = title.ToLower().Trim();
            title = title.Replace("&", " and ");
            title = title.Replace(".", " ").Replace("_", " ");
            while (title.Contains("  ")) title = title.Replace("  ", " ");
            return title;
        }



        

    }
}
