using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
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

        public void NotifySeriesUpdate(List<Title> title)
        {
            OnSeriesUpdate?.Invoke(this, title);
        }


        /// <summary>
        ///  An event triggered whenever a series is updated, providing the affected library and a list of affected titles.
        /// </summary>
        public event SeriesUpdated? OnSeriesDeleted;
        public void NotifySeriesDeleted(List<Title> title)
        {
            OnSeriesDeleted?.Invoke(this, title);
        }

        /// <summary>
        /// The root path for this dictionary
        /// </summary>
        public string RootPath = "";

        /// <summary>
        /// An enumerable lookup of all series that have been found by this series library
        /// </summary>
        public ConcurrentDictionary<string, Title> FoundSeries = [];

        /// <summary>
        /// A secondary mapping of file names to series information
        /// </summary>
        public ConcurrentDictionary<string, Title> NameToSeries = [];

        /// <summary>
        /// An abbreviation lookup that maps long strings to much shorter strings for
        /// compression purposes
        /// </summary>
        public List<string> abbreviations = [];

        /// <summary>
        /// A mapping of file targets to their actual media files
        /// </summary>
        public Dictionary<int, MediaFile> targetToFileMap = [];

        /// <summary>
        /// Creates a new library in the given root path
        /// </summary>
        /// <param name="RootPath"></param>
        public Library(string RootPath)
        {
            this.RootPath = RootPath;

            //Directory.CreateDirectory(RootPath + "/__library_cache");
            //Directory.CreateDirectory(RootPath + "/__library_cache/__covers");

            // updates always trigger immediate writeback to the cache
            OnSeriesUpdate += (x, l) =>
            {
                Directory.CreateDirectory(RootPath + "/__library_cache");
                lock (this)
                {
                    foreach (var title in l)
                    {
                        WriteTitle(title);
                    }
                }
            };

            try
            {
                using var file = File.OpenRead(RootPath + "/__library_cache/__library.dat");
                var dict = Byter.FromBytes(file);
                if(dict != null)
                {
                    this.FromDictionary(dict!);
                }

                // now see if we can load the series
                foreach (var datfile in Directory.EnumerateFiles(RootPath + "/__library_cache", "*.dat", SearchOption.TopDirectoryOnly))
                {
                    var filename = Path.GetFileName(datfile);
                    if (filename.StartsWith("__")) continue;

                    try
                    {
                        using var data = File.OpenRead(datfile);
                        dict = Byter.FromBytes(data);
                        var title = new Title();
                        if (dict != null)
                        {
                            title.FromDictionary(dict!);
                            this.FoundSeries.TryAdd(title.ID, title);
                            this.NameToSeries.TryAdd(title.Name, title);
                            title.Owner = this;
                        }

                    }
                    catch { }

                }

                // now refresh the file mappings
                RefreshTargetFileMaps();

            }
            catch { }

        }

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
            NameToSeries.Clear();
            Random r = new();
            foreach (var title in FoundSeries.Values)
            {
                title.Owner = this;
                NameToSeries.TryAdd(title.Name, title);

                foreach (var season in title.Eps.Values)
                {
                    foreach (var ep in season.Eps)
                    {
                        ep.FileLookup = targetToFileMap.Count ^ 0x2EF7B;
                        targetToFileMap.TryAdd(ep.FileLookup, ep);
                    }
                }
            }
        }

        /// <summary>
        /// Stores this entire library to the library cache
        /// </summary>
        public void StoreCache()
        {
            Directory.CreateDirectory(RootPath + "/__library_cache");
            Directory.CreateDirectory(RootPath + "/__library_cache/__covers");

            lock (this)
            {
                foreach (var f in FoundSeries.Values)
                {
                    WriteTitle(f);
                }
            }            
        }

        /// <summary>
        /// Saves a title to the disk
        /// </summary>
        /// <param name="title"></param>
        public void WriteTitle(Title title)
        {
            try
            {
                lock (title)
                {
                    Directory.CreateDirectory(RootPath + "/__library_cache");
                    File.Delete($"{RootPath}/__library_cache/{title.ID}.dat");

                    using var file = File.OpenWrite($"{RootPath}/__library_cache/{title.ID}.dat");
                    Byter.ToBytes(file, ((IDictable)title).MakeDictionary());
                }
            }
            catch { }
        }

        /// <summary>
        /// Saves this dictionary to the disk
        /// </summary>
        public void Save()
        {
            lock (this)
            {
                try
                {
                    File.Delete(RootPath + "/__library_cache/__library.dat");
                    using var f = File.OpenWrite(RootPath + "/__library_cache/__library.dat");
                    Byter.ToBytes(f, ((IDictable)this).MakeDictionary());
                }
                catch { }
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
                abbreviations.Clear();
                // Now let's go through every individual title

                foreach (var x in FoundSeries.Values.Where(x => x.EpisodeList.Count > 1))
                    Condense(x, maps, minimumAbbreviationLength, backwardsSearchDepth);

            }
            var time = timer.Elapsed.TotalMilliseconds;
            Console.WriteLine("Done: " + time);
        }

        public void Condense(Title title, IDictionary<string, string> maps, int minimumAbbreviationLength = 8, int backwardsSearchDepth = 10)
        {
            // Let's go through each episode and do some quick searching for long substrings
            // The logic is that we can walk backwards and map every such string in the backwards walk
            // But this becomes very memory expensive very quickly

            List<string> strings = new();
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
                int prefixLength = dir0!.Length;
                int minLength = int.Min(file0.Path.Length, file1.Path.Length);
                // Now get the last matching character
                for (; prefixLength < minLength; ++prefixLength)
                {
                    if (file0.Path[prefixLength] != file1.Path[prefixLength])
                    {
                        break;
                    }
                }

                // Now calculate the suffix length also;
                int suffixLength = 1;
                int maxLength = int.Max(file0.Path.Length, file1.Path.Length) - prefixLength;
                for (; suffixLength < maxLength; ++suffixLength)
                {
                    if (file0.Path[^suffixLength] != file1.Path[^suffixLength])
                    {
                        --suffixLength;
                        break;
                    }
                }

                // pos now represents the longest matching substring from 0,
                // which we can cache and find in the existing path list
                // Due to typical list size, List.IndexOf is faster overall than dictionary hashing
                var prefix = file0.Path.Remove(prefixLength);
                var suffix = file1.Path.Substring(file1.Path.Length - suffixLength);

                //group the suffixes
                if (suffix.Length > minimumAbbreviationLength)
                {
                    int n = strings.IndexOf(suffix);
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
                        strings.Add(suffix);
                        groups.Add([file0, file1]);
                    }
                }

                // and now group the prefixes
                if (prefix.Length > minimumAbbreviationLength)
                {
                    int n = strings.IndexOf(prefix);
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
                        strings.Add(prefix);
                        groups.Add([file0, file1]);
                    }
                }
            }

            var sortedIndices = OptimizeStrings(ref strings, ref groups, backwardsSearchDepth, minimumAbbreviationLength);

            // While the above step does affect the actual saving calculation,
            // The pairwise addition of values to groups ensures that the savings are now optimal
            // So all we have to do is perform the replacements
            for (int index = 0; index < sortedIndices.Count; index++)
            {
                int i = sortedIndices[index];

                // For safety's sake, get the replacer here
                bool add = false;
                if (!maps.TryGetValue(strings[i], out var replacer))
                {
                    replacer = $"?{(char)(0x0020 + maps.Count)}";
                    add = true;
                }

                // now replace it in every path
                foreach (var file in groups[i])
                {
                    var prev = file.Path;
                    file.Path = prev.Replace(strings[i], replacer);
                    // map it if necessary
                    if (prev != file.Path)
                    {
                        if (add)
                        {
                            add = false;
                            maps.TryAdd(strings[i], replacer);
                            abbreviations.Add(strings[i]);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Condenses the string collection in such a way that the alike-pairs of strings are merged
        /// based on their longest similar substrings.
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="affectedFiles"></param>
        /// <param name="depth"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        private List<int> OptimizeStrings(ref List<string> strings, ref List<HashSet<MediaFile>> affectedFiles, int depth, int minLength)
        {

            // Let's do another natural sort
            var comparer = new NaturalStringComparer();
            var sorting = strings.Select((x, index) => index).ToList();

            var strs = strings; // capture ref into closure
            sorting.Sort((x, y) => comparer.Compare(strs[x], strs[y]));

            strings = strings
                .Select((x, index) => (x, index))
                .OrderBy(x => sorting[x.index])
                .Select(x => x.x)
                .ToList();

            affectedFiles = affectedFiles
                .Select((x, index) => (x, index))
                .OrderBy(x => sorting[x.index])
                .Select(x => x.x)
                .ToList();

            // Now let's count how many characters each option can save
            var saves = new List<int>();
            for (int i = 0; i < strings.Count; ++i)
            {
                saves.Add(strings[i].Length * affectedFiles[i].Count);
            }

            // we assume the strings are in a natural sort order
            // So we can just check each string and see if they can be merged with small changes
            // And check the new result of this before accepting/rejecting
            for (int i = 0; i < strings.Count - 1; i++)
            {
                var path0 = strings[i];
                var path1 = strings[i + 1];

                int len0 = path0.Length;
                int len1 = path1.Length;

                // Consider every pair of substrings
                int bestLength = 0;
                int best0 = -1;
                int best1 = -1;

                for (int pos0 = 0; pos0 < len0; pos0++)
                {
                    for (int pos1 = 0; pos1 < len1; pos1++)
                    {
                        int currentMatch = 0;
                        while ((pos0 + currentMatch) < len0 && (pos1 + currentMatch) < len1
                               && path0[pos0 + currentMatch] == path1[pos1 + currentMatch])
                        {
                            currentMatch++;
                        }
                        if (currentMatch > bestLength)
                        {
                            bestLength = currentMatch;
                            best0 = pos0;
                            best1 = pos0;
                        }
                        bestLength = int.Max(bestLength, currentMatch);
                    }
                }

                if (bestLength < minLength) continue;
                string newPath = path0.Substring(best0, bestLength);


                // now let's calculate the total saving
                var newGroup = affectedFiles[i]
                    .Union(affectedFiles[i + 1])
                    .ToHashSet();

                // calculate the new saving amount
                int newSave = newGroup.Count * newPath.Length;
                // If it's better, then merge the pairs
                if (newSave > saves[i] && newSave > saves[i + 1])
                {
                    // Put old data here
                    affectedFiles[i] = newGroup;
                    strings[i] = newPath;
                    saves[i] = newSave;

                    affectedFiles.RemoveAt(i + 1);
                    strings.RemoveAt(i + 1);
                    saves.RemoveAt(i + 1);

                    // Iterate from the beginning
                    i = -1;
                }
            }

            // Now we will sort this again

            // Now let's sort the collection according to the saving value
            // Can envisage as saves.Select((x, value) => (x, value)).OrderBy(...).Select(...)
            // But the Linq is slower
            var sortedIndices = new List<int>();
            for (int i = 0; i < strings.Count; i++) sortedIndices.Add(i);
            sortedIndices.Sort((i, j) => saves[j].CompareTo(saves[i]));

            return sortedIndices;

        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["P"] = RootPath;
            dict["D"] = abbreviations;
        }

        public Dictionary<string, object> MakeFullDictionary()
        {
            var dict = ((IDictable)this).MakeDictionary();
            dict["S"] = FoundSeries;
            return dict;
        }
        
        public void FromDictionary(IDictionary<string, object> dict)
        {
            RootPath = (string)dict["P"];
            abbreviations = (List<string>)dict["D"];
            if(dict.TryGetValue("S", out var series) && series is Dictionary<string, Title> result)
            {
                FoundSeries = new ConcurrentDictionary<string, Title>(result);
                RefreshTargetFileMaps();
            }
        }
    }
}
