using CookieCrumbs.ContentLibrary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace Backend.ServerLibrary
{
    public class Searcher
    {

        public static List<string> VideoExtensions = [".mp4", ".mkv", ".m4v", ".avi", ".wmv"];


        public static Regex TitleGet = new Regex(
            //@"(?:\[[^\]]*\]\s*)*(?<Title>[A-Z](?:[a-z]*|\d+)(?:[.\s&-][A-Z](?:[a-z]*|\d+))*)(?=.*(?:S\d{2}|(?:\(\d{4}\)|\d{4})|Season\s?\d{1,2}|1080p|720p|x265|HEVC|x264|AV1))",
            @"(?:\[[^\]]*\]\s*)*(?<Title>[A-Z](?:[a-z]+|)(?:[.\s&-][A-Z][a-z]*)*)(?=.*(?:S\d{2}|(?:\(\d{4}\)|\d{4})|Season\s?\d{1,2}|1080p|720p|x265|HEVC|x264|AV1))",
        //@"(?:\[[^\]]*\]\s*)*(?<Title>[A-Z][a-z]+(?:[.\s&][A-Z][a-z]+)*)(?=.*(?:S\d{2}|(?:\(\d{4}\)|\d{4})|Season\s?\d{1,2}|1080p|720p|x265|HEVC|x264|AV1))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Regex SeasonEpisode = new Regex(
            @"(?:\b(S(?:\d{1,2})?|Season(?:[\s.-]*\d{1,2})?)(?:[\s.-]*[xX]?\d{1,2})?\b)|(?:\b\d{1,2}[xX]\d{1,2}\b)|(?:\b\d{1,2}\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses a file name into a title, season, and episode tuple. Returns null if not valid.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static (string Title, int? Season, int? Episode)? ParseFileName(string file)
        {
            //1. See if we can load details from the parent directory
            string directory = Path.GetFileName(Path.GetDirectoryName(file))!;

            // Skip the recycler directory
            if (file.Contains("$RECYCLE")) return null;
            int season = -1;

            // Now inspect the directory
            // As a first step, some directories may be organized
            // Title / Season X / [files]
            // In this case we want the Title, Season X, and [files]
            directory = Library.CleanTitle(directory);
            if (directory.StartsWith("season"))
            {
                //strip "season" and see if we can read a season
                int.TryParse(directory.Substring(6).Trim(), out season);
                directory = Path.GetDirectoryName(Path.GetDirectoryName(file))!;
                directory = Path.GetFileName(directory)!;
                if (directory != null)
                {
                    directory = Library.CleanTitle(directory);
                }
            }


            string filename = Path.GetFileName(Path.GetFileNameWithoutExtension(file));
            // Skip $ prefixed files since they almost always seem to be recyclers
            if (filename.StartsWith("$")) return null;
            filename = Library.CleanTitle(filename);

            // Now let's try to match the title first, from the directory
            // Generally, this only works if (A) it contains season/episode information
            // Or B it contains a Season folder
            string? title = null;

            var titleMatch = TitleGet.Match(directory ?? "");

            if (directory != null && titleMatch.Success)
            {
                title = titleMatch.Groups["Title"].Value;

                // Check forwards
                int pos = directory.IndexOf(title) + title.Length;

                if (pos < directory.Length + 5 && (directory[pos] == '-' || directory[pos + 1] == '-'))
                {
                    // see if we should grab ahead
                }


            }
            // It wasn't a valid title (???) but
            // It was in a Title/Season/File format
            // So we can just use the folder name
            else if (directory != null && season > 1)
            {
                //Strip out anything in square brackets
                string cleaned = Regex.Replace(directory, @"\[[^\]]*\]", "");
                title = cleaned;
            }
            else
            {
                // Otherwise match the filename
                titleMatch = TitleGet.Match(filename);
                if (titleMatch.Success)
                {
                    title = titleMatch.Groups["Title"].Value;
                }
            }

            // If we found a title, then we should try to read an episode thingy
            if (title != null)
            {
                var se = GetSeasonEpisode(directory);
                if (season < 0)
                {
                    season = se.season;
                }
                var episode = se.episode;

                se = GetSeasonEpisode(filename);

                //now do our best to get the things
                if (se.season > -1) season = se.season;
                if (se.episode > -1) episode = se.episode;

                // How to determine if we are a movie vs an episode
                if (episode < 0)
                {
                    episode = TryReadEpisode(filename);
                }


                if (title.EndsWith(" s"))
                {
                    title = title.Remove(title.Length - 2);
                    if (title.Length <= 0) return null;
                }

                return (title, season, episode);

            }

            return null;
        }



        /// <summary>
        ///  Trie to read the episode number out of a given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int TryReadEpisode(string input)
        {
            input = input.Replace("-", " ");

            string cleaned = Regex.Replace(input, @"\[[^\]]*\]", "");
            // List of strings to remove
            string[] stringsToRemove = { "10bit", "10 bit", "ac3", "6ch", "5 1", "x265", "x264", "1080p", "720p" };
            // Create regex pattern to match the strings
            string pattern = $"\\b({string.Join("|", stringsToRemove)})\\b";

            cleaned = Regex.Replace(cleaned, pattern, "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"[^0-9]", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"\b\d{3,}\b", "");
            var match = Regex.Match(cleaned, @"^\d{1,2}|\d{1,2}$");

            if (match.Success)
            {
                int.TryParse(match.Value, out var ep);
                return ep;
            }
            return -1;


        }

        public static (int season, int episode) GetSeasonEpisode(string text)
        {
            //@"(?:S|s)(\d{2})(?:\s*E|\s*e)?(\d{2})"
            //@"(?:s|season\s)(\d{1,2})(?:e(\d{1,2})|(\d{1,2}))?

            var match = Regex.Match(text, @"(?:s|season\s)(\d{1,2})(?:\s*e(\d{1,2})|(\d{1,2}))?", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                int season = int.Parse(match.Groups[1].Value);
                int episode = -1; // Default value for missing episode

                if (match.Groups[2].Success) // Checks if episode is present
                {
                    episode = int.Parse(match.Groups[2].Value);
                }
                else if (match.Groups[3].Success) // Checks for alternate episode match (e.g., "S01E01")
                {
                    episode = int.Parse(match.Groups[3].Value);
                }
                return (season, episode);
            }
            return (-1, -1);
        }

        public static bool IsSeasonSuffixTitular(Title show, MediaFile file)
        {
            if (!show.id.EndsWith(" season")) return false;

            string _path = file.Path;
            _path = Library.CleanTitle(_path);

            // now find the title/season body component of the show id in the path
            int n = _path.IndexOf($"{show.id}");

            if (n > 0)
            {
                n += show.id.Length + 1;
                if (n < _path.Length)
                {
                    if (_path[n - 1] == ' ')
                    {
                        //now read to the next word
                        int m = _path.IndexOf(' ', n);
                        if (m < 0) m = _path.Length - 1;
                        string part = _path.Substring(n, m - n);
                        part = part.Trim();
                        string pattern = @"^\d{1,2}$|^(complete|720p|1080p)$";
                        if (Regex.IsMatch(part, pattern))
                        {
                            return false;
                        }

                        pattern = @"^\(\d{4}\)$";
                        if (Regex.IsMatch(part, pattern))
                        {
                            return true;
                        }

                        pattern = @"^\d{4}$";
                        if (Regex.IsMatch(part, pattern))
                        {
                            return true;
                        }
                    }
                }
            }
            return true;
        }



        /// <summary>
        /// Enumeration of series that have been discovered by the Searcher
        /// </summary>
        private Library library = new();

        public string Root = "";

        /// <summary>
        /// Creates a new searcher
        /// </summary>
        /// <param name="rootFolder"></param>
        public Searcher(string rootFolder)
        {
            Root = rootFolder;
        }


        /// <summary>
        /// Enumerates this searcher from the given parent directory
        /// </summary>
        /// <param name="parentDirectory"></param>
        public Library Enumerate(int threads = 1)
        {
            var s = Stopwatch.StartNew();

            // BFS allows us to break this up with threads effectively
            BlockingCollection<string> Directories = new();
            Directories.Add(Root);

            // Use a separate collection to absorb found series
            ConcurrentDictionary<string, Title> grabbed = new();

            // Generate a list of tasks on the threadpool
            //List < Task > tasks = new();
            //for (int i = 0; i < int.Clamp(threads, 1, 16); i++)
            //{
            //    tasks.Add(Task.Run(() =>
            //    {
            // Essentially, let's just enumerate the hell out of everything
            // We should delay a little bit, just to make sure
            // that we don't run out of directories
            while (Directories.TryTake(out var d, 100))
            {
                try
                {
                    // Let's get every directory in this directory
                    // So that the other threads can work
                    foreach (var dd in Directory.EnumerateDirectories(d)) Directories.Add(dd);
                }
                catch { }

                // See if this folder contains videos
                try
                {
                    // Now process all of the files in this directory, non-recursively
                    var videoFiles = Directory.EnumerateFiles(d).Where(x => VideoExtensions.Contains(Path.GetExtension(x).ToLower()));
                    foreach (string file in videoFiles) ProcessFile(file, grabbed);
                }
                catch { }
            }
            //    }
            //    ));
            //}
            //// let all of the workers finish
            //Task.WaitAll(tasks);

            // Now we need to do another pass
            // to collect everything into a Season/Episode collection

            foreach (var kv in grabbed)
            {
                // Now the fucky annoying part
                List<MediaFile> filesOwned = [];
                filesOwned.AddRange(kv.Value.EpisodeList.Values);

                // now let's seasonally sort them
                ConcurrentDictionary<string, Season> seasonSorting = [];
                foreach (var f in filesOwned)
                {
                    Season list = seasonSorting.GetOrAdd(f.SNo.ToString(), new Season());
                    list.Episodes.Add(f);
                }

                // Now, we need to sort this list based on Episode - Filename
                foreach (var season in seasonSorting)
                {
                    // In some cases, the episode numberings may be lost.
                    // This is very difficult to solve, due to the wide variety of formats.
                    // However, we can assume that natural ordering will be valid,
                    // Aka the default ordering that they would have in a file window

                    // First let's map the sorting keys to the files
                    Dictionary<string, MediaFile> remap = [];
                    foreach (var f in season.Value.Episodes)
                    {
                        // Sort it in episodic order, then path order
                        int n = f.EpNo < 0 ? 999 : f.EpNo;
                        remap.Add($"{n} - {f.Path}", f);
                    }

                    // Now perform the natural string sorting
                    List<string> sorted = remap.Keys.ToList();
                    sorted.Sort(new NaturalStringComparer());

                    // Lastly, remap the sorting keys back to get the episode index
                    for (int i = 0; i < sorted.Count; ++i)
                    {
                        var e = remap[sorted[i]];
                        if (e.EpNo == -1)
                            e.EpNo = i + 1;
                    }

                    // Lastly, sort the actual episode list by episode order
                    season.Value.Episodes.Sort((x, y) => x.EpNo.CompareTo(y.EpNo));
                }

                //Now push the sorted season/episode list into the SeasonEpisode format
                kv.Value.Eps = seasonSorting.ToDictionary();
            }


            // Now we need to process "Season" artifacts
            // But it's reasonable to think that e.g Hallmark Christmas movies
            // May contain "Season" in the title, so a simple "Season" or "Seasons" filter
            // could produce problems.

            // This ambiguity only seems to exist in uncleaned directory/file names, where
            // e.g Title.With.Season.In.It
            // May yield;
            // e.g Title.With.Season.In.It.Season.1
            // e.g Title.With.Season.In.It.S01
            // e.g Title.With.Season.In.It.2024.Season.1
            // e.g Title With Season In It (2024)
            //etc

            // In this case, we

            foreach (var show in grabbed)
            {
                if (show.Value.id.EndsWith(" season"))
                {
                    string prospective = show.Value.id.Remove(show.Value.id.Length - 7);
                    List<string> removed = [];

                    foreach (var f in show.Value.EpisodeList)
                    {
                        if (!IsSeasonSuffixTitular(show.Value, f.Value))
                        {
                            Title t = new(prospective);
                            t = grabbed.GetOrAdd(prospective, t);
                            removed.Add(f.Key);

                            // Add it if we can
                            if (!t.EpisodeList.ContainsKey(f.Key))
                            {
                                f.Value.Owner = t;
                                t.EpisodeList.Add(f.Key, f.Value);
                            }
                        }
                    }

                    // It already existed
                    foreach (var r in removed)
                    {
                        show.Value.EpisodeList.Remove(r);
                    }
                }
            }


            // Clean empty entries
            foreach (var k in grabbed.Keys.ToArray())
            {
                if (grabbed[k].EpisodeList.Count <= 0) grabbed.TryRemove(k, out _);
            }


            foreach (var show in grabbed)
            {

                if (show.Value.PredictMovie)
                {
                    Console.WriteLine($"Movie: {show.Value.Name}\n{show.Value.EpisodeList.First().Value.Path}");
                }
                else
                {
                    Console.WriteLine($"Show: {show.Value.Name}. Seasons: {show.Value.Eps.Count}");
                    foreach (var season in show.Value.Eps)
                    {
                        Console.WriteLine($"\tSeason {season.Key}");
                        foreach (var e in season.Value.Episodes)
                        {
                            Console.WriteLine($"\t  - S{e.SNo}E{e.EpNo}: {e.Path}");
                        }
                    }
                }
            }

            Console.WriteLine("Searched Directory. Time: " + s.ElapsedMilliseconds + "ms");

            // Now set up the library and bazoonga
            Library library = new();
            library.FoundSeries = grabbed;
            return library;
        }


        public void ProcessFile(string file, ConcurrentDictionary<string, Title> grabbed)
        {


            var details = ParseFileName(file);
            if (details != null)
            {
                var n = details.Value;
                //Console.WriteLine($"Found: {n.Title} - s{n.Season}e{n.Episode}   {file}");

                Title t = new(n.Title);
                t = grabbed.GetOrAdd(n.Title, t);
                MediaFile f = new MediaFile()
                {
                    SNo = n.Season ?? -1,
                    EpNo = n.Episode ?? -1,
                    Path = file
                };

                lock (t)
                {
                    if (!t.EpisodeList.TryAdd($"{n.Season}x{n.Episode}", f))
                    {
                        t.EpisodeList.TryAdd($"_unknown_{t.EpisodeList.Count}", f);
                    }
                }
            }

        }



    }
}
