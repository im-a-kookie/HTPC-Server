using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cookie.ContentLibrary
{
    public class Library : IDictable
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
        /// An abbreviation lookup that maps long strings to much shorter strings for
        /// compression purposes
        /// </summary>

        public Dictionary<string, string> abbreviations = [];

        /// <summary>
        /// A mapping of file targets to their actual media files
        /// </summary>
        public Dictionary<string, MediaFile> targetToFileMap = [];


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

        /// <summary>
        /// Flushes the dictionary of file mappings
        /// so that we can look them up quickly from just the shorter string
        /// </summary>
        public void RefreshTargetFileMaps()
        {
            targetToFileMap.Clear();
            foreach (var title in FoundSeries.Values)
            {
                foreach (var season in title.Eps.Values)
                {
                    foreach (var ep in season.Eps)
                    {
                        targetToFileMap.TryAdd("v_" + CryptoHelper.HashString(ep.Path).Remove(16), ep);
                    }
                }
            }


        }



        private class CompressionBundle
        {
            public int saving = 0;
            public List<MediaFile> files = new();
        }

        /// <summary>
        /// Compresses the paths of the media files stored by this library container, using a relatively simple
        /// string matching system.
        /// 
        /// <para>This algorithm is optimized for conventional TV/etc file structures, and assumes that most files
        /// will group into subdirectories with between 8 and 24 files.</para>
        /// </summary>
        /// <param name="minimumAbbreviationLength">The minimum length of permitted abbreviated substrings within a path</param>
        /// <param name="backwardsSearchDepth">The backwards depth that can be searched within matching paths for better unified matches</param>
        public void CompressPaths(int minimumAbbreviationLength = 10, int backwardsSearchDepth = 10)
        {
            var timer = Stopwatch.StartNew();

            lock (this)
            {
                ConcurrentDictionary<string, string> maps = new();
                // Now let's go through every individual title

                foreach (var x in FoundSeries.Values.Where(x => x.EpisodeList.Count > 1))
                    Condense(x, maps, minimumAbbreviationLength, backwardsSearchDepth);

                abbreviations.Clear();
                foreach (var m in maps)
                    abbreviations.TryAdd(m.Value, m.Key);

            }
            var time = timer.Elapsed.TotalMilliseconds;
            Console.WriteLine("Done: " + time);
        }

        public void Condense(Title title, IDictionary<string, string> maps, int minimumAbbreviationLength = 10, int backwardsSearchDepth = 10)
        {
            // Let's go through each episode and do some quick searching for long substrings
            // The logic is that we can walk backwards and map every such string in the backwards walk
            // But this becomes very memory expensive very quickly

            List<string> paths = new();
            List<HashSet<MediaFile>> groups = new();

            List<MediaFile> sortedFiles = new List<MediaFile>();
            foreach (var season in title.Eps)
            {
                sortedFiles.AddRange(season.Value.Eps);
            }

            // Sort them by the paths
            var comparer = new NaturalStringComparer();
            sortedFiles.Sort((i, j) => comparer.Compare(i.Path, j.Path));

            // Now go through every file
            for (int i = 0; i < sortedFiles.Count - 1; ++i)
            {
                // assuming natural sorting,
                // we can simply look at the next file in the ordering
                var file0 = sortedFiles[i];
                var file1 = sortedFiles[i + 1];

                if (file0.Path.Contains("Terminal"))
                {
                    Console.WriteLine("bananas!");
                }

                var dir0 = Path.GetDirectoryName(file0.Path);
                var dir1 = Path.GetDirectoryName(file1.Path);

                if (dir0 == null) continue;
                if (dir1 == null) continue;

                // Move back through the path directory naming
                while (dir0 != dir1
                    && dir0!.Length > minimumAbbreviationLength
                    && dir1!.Length > minimumAbbreviationLength)
                {
                    dir0 = Path.GetDirectoryName(dir0);
                    dir1 = Path.GetDirectoryName(dir1);
                }

                // Ensure that they are valid strings
                if (dir0 != dir1
                    || dir0!.Length < minimumAbbreviationLength
                    || dir1!.Length < minimumAbbreviationLength)
                    continue;

                // We have now located similar-ish directories, so we want the longest match within them
                var slice0 = file0.Path.AsSpan();
                var slice1 = file1.Path.AsSpan();
                int pos = dir0!.Length;

                // Now get the last matching character
                for (; pos < int.Min(slice0.Length, slice1.Length); ++pos)
                {
                    if (slice0[pos] != slice1[pos])
                    {
                        break;
                    }
                }

                // pos now represents the longest matching substring from 0,
                // which we can cache and find in the existing path list
                // Due to typical list size, List.IndexOf is faster overall than dictionary hashing
                var prefix = file0.Path.Remove(pos);
                int n = paths.IndexOf(prefix);
                if (n >= 0)
                {
                    // we have it already, so map us into this
                    var l = groups[n];
                    l.Add(file0);
                    l.Add(file1);
                }
                else
                {
                    // otherwise just chonk it onto the end
                    paths.Add(prefix);
                    groups.Add([file0, file1]);

                }
            }

            // Now let's count how many characters each option can save
            var saves = new List<int>();
            for (int i = 0; i < paths.Count; ++i)
            {
                saves.Add(paths[i].Length * groups[i].Count);
            }


            // The path collection is guaranteed to be organized such that comparison
            // pairs will be in natural sorting, so we don't need to sort again
            // So, we can now go through in a pair-wise fashion again
            for (int i = 0; i < paths.Count - 1; i++)
            {
                var path0 = paths[i];
                var path1 = paths[i + 1];

                // Now find the matching point, if it exists
                int j = int.Min(path0.Length, path1.Length) - 1;
                for (; j >= backwardsSearchDepth; j--)
                {
                    if (path0[j] == path1[j]) break;
                }

                // Ensure matching path
                if (path0.Remove(j) != path1.Remove(j)) continue;

                // Now let's collect the groups to calculate the new condensing value
                HashSet<MediaFile> newGroup = new(groups[i]);
                foreach (var file in groups[i + 1]) newGroup.Add(file);

                // calculate the new saving amount
                int newSave = newGroup.Count * j;
                // If it's better, then merge the pairs
                if (newSave > saves[i] && newSave > saves[i + 1])
                {
                    // Put old data here
                    groups[i] = newGroup;
                    paths[i] = path0.Remove(int.Min(path0.Length, j + 1));
                    saves[i] = newSave;
                    // Iterate from the beginning
                    i = -1;
                }
            }

            // Now let's sort the collection according to the saving value
            // Can envisage as saves.Select((x, value) => (x, value)).OrderBy(...).Select(...)
            // But the Linq is slower
            var sortedIndices = new List<int>();
            for (int i = 0; i < paths.Count; i++) sortedIndices.Add(i);
            sortedIndices.Sort((i, j) => saves[j].CompareTo(saves[i]));

            // While the above step does affect the actual saving calculation,
            // The pairwise addition of values to groups ensures that the savings are now optimal
            // So all we have to do is perform the replacements
            for (int index = 0; index < sortedIndices.Count; index++)
            {
                int i = sortedIndices[index];

                // For safety's sake, get the replacer here
                bool add = false;
                if (!maps.TryGetValue(paths[i], out var replacer))
                {
                    replacer = $"<{maps.Count}>";
                    add = true;
                }

                // now replace it in every path
                foreach (var file in groups[i])
                {
                    var prev = file.Path;
                    file.Path = prev.Replace(paths[i], replacer);
                    // map it if necessary
                    if (add && prev != file.Path)
                    {
                        add = false;
                        maps.TryAdd(paths[i], replacer);
                    }
                }
            }
        }




        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["D"] = abbreviations;
            dict["S"] = FoundSeries.ToDictionary<string, Title>();

        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            abbreviations = (Dictionary<string, string>)dict["D"];
            FoundSeries = new ConcurrentDictionary<string, Title>((Dictionary<string, Title>)dict["S"]);
        }
    }
}
