using CookieCrumbs.ContentLibrary;

namespace Backend.ServerLibrary
{
    internal class Cache
    {
        public string Root;

        public Cache(string root)
        {
            this.Root = root;
        }

        // Cache hierarchy
        // Root
        // cache: Root / _media_library_cache
        //      cache/[title]
        //          - description.text          <- de/serialize Title body
        //          - cover.[jpg,png,...]
        //          [files]
        //              [Season Number]
        //                  episode_number.text <- de/serialize File body
        // Text format: json
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>

        public Library? FromCache()
        {


            return null;

        }

        public void ToCache(Library library)
        {

        }


        //public void BuildCache()
        //{
        //    string Path = $"{library.Root}/_media_library_cache";
        //    Directory.CreateDirectory(Path);

        //    // Every single title is given its own folder
        //    foreach (var kv in library.FoundSeries)
        //    {
        //        Directory.CreateDirectory($"{Path}/{kv.Key}");
        //        // Write the defails of this series
        //        // Every file is just a link
        //        Directory.CreateDirectory($"{Path}/{kv.Key}/files");

        //        ConcurrentDictionary<int, List<File>> SeasonCapture = new();

        //        foreach (var ekv in kv.Value.EpisodeList)
        //        {
        //            int season = ekv.Value.Season;
        //            List<File> l = SeasonCapture.GetOrAdd(season, []);
        //            l.Add(ekv.Value);
        //        }

        //        foreach (var kl in SeasonCapture)
        //        {
        //            Directory.CreateDirectory($"{Path}/{kv.Key}/files/{kl.Key}");

        //            // now we will sort them by their actual path
        //            // and then fill using either;
        //            // (a) the extracted episode
        //            // (b) the index
        //            for (int i = 0; i < kl.Value.Count; ++i)
        //            {




        //            }
        //        }



        //    }



        //}


    }
}
