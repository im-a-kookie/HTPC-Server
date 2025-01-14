﻿using Cookie.ContentLibrary;
using Cookie.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cookie.Server.ServerLibrary
{
    /// <summary>
    /// Creates a new searcher
    /// </summary>
    /// <param name="rootFolder"></param>
    public partial class Searcher(string rootFolder)
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
                int.TryParse(directory[6..].Trim(), out season);
                directory = Path.GetDirectoryName(Path.GetDirectoryName(file))!;
                directory = Path.GetFileName(directory)!;
                if (directory != null)
                {
                    directory = Library.CleanTitle(directory);
                }
            }


            string filename = Path.GetFileName(Path.GetFileNameWithoutExtension(file));
            // Skip $ prefixed files since they almost always seem to be recyclers
            if (filename.StartsWith('$')) return null;
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
                var se = GetSeasonEpisode(directory ?? "/");
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
        ///  Trie to read the episode number out of a given input, for files formatted with
        ///  <para><code>drive:/path/Filler Text - 01 [filler].mkv</code></para>
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

        /// <summary>
        /// Gests the Season and Episode number out of a given filepath, defaulting to season = -1, episode = -1
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determines if the suffix is titular, e.g Season 04 1080p 2005 etc, this method determines
        /// if the details after the season number are superfluous
        /// </summary>
        /// <param name="show"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsSeasonSuffixTitular(Title show, MediaFile file)
        {
            if (!show.ID.EndsWith(" season")) return false;

            string _path = file.Path;
            _path = Library.CleanTitle(_path);

            // now find the title/season body component of the show id in the path
            int n = _path.IndexOf($"{show.ID}");

            if (n > 0)
            {
                n += show.ID.Length + 1;
                if (n < _path.Length)
                {
                    if (_path[n - 1] == ' ')
                    {
                        //now read to the next word
                        int m = _path.IndexOf(' ', n);
                        if (m < 0) m = _path.Length - 1;
                        string part = _path[n..m];
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


        public string Root = rootFolder;


        /// <summary>
        /// Enumerates this searcher from the given parent directory
        /// </summary>
        /// <param name="parentDirectory"></param>
        public async Task<Library> Enumerate(int threads = 1, Library? input = null)
        {
            var s = Stopwatch.StartNew();

            // BFS allows us to break this up with threads effectively
            BlockingCollection<string> Directories = new();
            Directories.Add(Root);

            // Use a separate collection to absorb found series
            ConcurrentDictionary<string, Title> grabbed = new();

            // Generate a list of tasks on the threadpool
            List<Task> tasks = new();
            for (int i = 0; i < int.Clamp(threads, 1, 4); i++)
            {
                tasks.Add(Task.Run(() =>
                {
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
                            foreach (string file in videoFiles)
                                ProcessFile(file, grabbed);
                        }
                        catch { }
                    }
                }
            ));
            }
            // let all of the workers finish
            await Task.WhenAll(tasks);

            // Now we need to do another pass
            // to collect everything into a Season/Episode collection

            foreach (var kv in grabbed)
            {
                // Now the fucky annoying part
                List<MediaFile> filesOwned = [];
                filesOwned.AddRange(kv.Value.EpisodeList.Values);

                // now let's seasonally sort them
                ConcurrentDictionary<int, Season> seasonSorting = [];
                foreach (var f in filesOwned)
                {
                    Season list = seasonSorting.GetOrAdd(f.SNo, new Season());
                    list.Eps.Add(f);
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
                    foreach (var f in season.Value.Eps)
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
                    season.Value.Eps.Sort((x, y) => x.EpNo.CompareTo(y.EpNo));
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
                if (show.Value.ID.EndsWith(" season"))
                {
                    string prospective = show.Value.ID.Remove(show.Value.ID.Length - 7);
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

            // Now go through each value and simply write details about it for debugging
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
                        foreach (var e in season.Value.Eps)
                        {
                            Console.WriteLine($"\t  - S{e.SNo}E{e.EpNo}: {e.Path}");
                        }
                    }
                }
            }

            Console.WriteLine("Searched Directory. Time: " + s.ElapsedMilliseconds + "ms");

            // Now set up the library and bazoonga
            input ??= new(Root);

            // now consolidate everything
            input.FoundSeries = new();
            foreach(var series in grabbed)
            {
                input.FoundSeries.TryAdd(series.Value.ID, series.Value);
            }
            // now compress and return details
            input.CompressPaths();
            input.RefreshTargetFileMaps();
            return input;
        }

        /// <summary>
        /// Processes a file into a dictionary of grabbed series
        /// </summary>
        /// <param name="file"></param>
        /// <param name="grabbed"></param>
        public static void ProcessFile(string file, ConcurrentDictionary<string, Title> grabbed)
        {

            var details = ParseFileName(file);
            if (details != null)
            {
                var n = details.Value;
                //Console.WriteLine($"Found: {n.Title} - s{n.Season}e{n.Episode}   {file}");

                Title t = new(n.Title);
                t = grabbed.GetOrAdd(n.Title, t);
                MediaFile f = new()
                {
                    SNo = n.Season ?? -1,
                    EpNo = n.Episode ?? -1,
                };

                f.SetPath(t, file);

                lock (t)
                {
                    if (!t.EpisodeList.TryAdd($"{n.Season}x{n.Episode}", f))
                    {
                        t.EpisodeList.TryAdd($"_unknown_{t.EpisodeList.Count}", f);
                    }
                }
            }

        }

        [GeneratedRegex(
                    @"# Match optional bracketed sections (like [Year], [Genre], etc.)
(?:\[[^\]]*\]\s*)*

# Capture the title (starts with an uppercase letter, followed by optional lowercase letters and other title parts)
(?<Title>
    [A-Z]                           # Starts with an uppercase letter
    (?:[a-z]+|)                     # Followed by lowercase letters (optional)
    (?:[.\s&-][A-Z][a-z]*)*         # Additional parts, starting with ""."", space, ""&"", or ""-"", then uppercase followed by lowercase
)

# Ensure the string contains at least one of these patterns:
# - Season designations (e.g., S01, Season 1)
# - A year (either in parentheses or just 4 digits)
# - Resolution or codec type (e.g., 1080p, HEVC, x264)
(?=.*(?: 
    S\d{2}                           # Season designation (Sxx)
    |(?:\(\d{4}\)|\d{4})             # Year (yyyy or (yyyy))
    |Season\s?\d{1,2}                # Season followed by 1 or 2 digits (Season x)
    |1080p|720p|x265|HEVC|x264|AV1   # Video quality or encoding format (1080p, x265, etc.)
))", 
            
            RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex TitleRegexBuilder();


        [GeneratedRegex(
            @"# Match Season or Sxx pattern (e.g., ""S01"", ""Season 1"", ""Season 01"", ""S01 1080p"")
(?:\b(S(?:\d{1,2})?|Season(?:[\s.-]*\d{1,2})?)   # Match ""S"" followed by 1 or 2 digits or ""Season"" followed by optional space, period, or hyphen, then 1 or 2 digits
  (?:[\s.-]*[xX]?\d{1,2})?                         # Optionally match a resolution (e.g., "" 1080"", ""x1080"", "" 720p"")
\b)

# Match video resolution or codec in the form ""xxXxx"" (e.g., ""720x1080"", ""1080x720"")
|(?:\b\d{1,2}[xX]\d{1,2}\b)

# Match 1 or 2 digits (e.g., ""15"", ""9"")
|(?:\b\d{1,2}\b)",
            
            RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex SeasonEpisodeRegexBuilder();
    }
}
