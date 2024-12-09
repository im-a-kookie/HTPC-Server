using Cookie.Serializers;

namespace Cookie.ContentLibrary
{
    public class Title : BasicSerial
    {

        /// <summary>
        /// A mapping of episodes in this title. It is expected that the server will provide
        /// unique remote access keys to fill this collection.
        /// </summary>
        public Dictionary<string, MediaFile> EpisodeList = new();

        /// <summary>
        /// A collection of all files in this title, organized in a Season-Episode-File structure.
        /// </summary>
        public Dictionary<int, Season> Eps { get; set; } = [];

        /// <summary>
        /// A unique code that identifies this series
        /// </summary>
        public int Code = 0;

        /// <summary>
        /// Gets or sets the id for this title. This should be unique.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Gets or sets a readable title for this series. This does not
        /// have to be unique.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Instantiate a new title with the given id value
        /// </summary>
        /// <param name="id"></param>
        public Title(string id)
        {
            this.id = id;
            this.Name = CapitalizeTitle(id);
        }

        /// <summary>
        /// A simple prediction of whether this Title is a movie or a series, based on the
        /// number of files that it contains
        /// </summary>
        public bool PredictMovie => EpisodeList.Count == 1;


        public override Dictionary<string, string> Write()
        {
            Dictionary<string, string> result = new();
            result.Add("Code", Code.ToString());
            result.Add("id", id);
            result.Add("Name", Name.ToString());

            Dictionary<string, (int count, int index)> counter = new();
            Dictionary<MediaFile, string> condenser = new();

            int saved = 0;

            // Let's see if we can condense it
            foreach (var kv in Eps)
            {
                foreach (var ep in kv.Value.Eps)
                {
                    string p = Path.GetDirectoryName(ep.Path)!;
                    if (!counter.TryAdd(p, (1, counter.Count)))
                    {
                        var val = counter[p];
                        counter[p] = (val.count + 1, val.index);
                    }
                    condenser.Add(ep, p);
                }
            }

            foreach (var k in counter)
            {
                result.Add($"_{k.Value.index}", k.Key);
                saved += k.Key.Length * (k.Value.count - 1);
            }

            List<string> results = new List<string>();
            foreach (var kv in Eps)
            {
                // Let's just build a list of lists
                // And realign the series collection later
                foreach (var ep in kv.Value.Eps)
                {
                    Dictionary<string, string> data = new();
                    data["SNo"] = ep.SNo.ToString();
                    data["EpNo"] = ep.EpNo.ToString();
                    data["Codec"] = ep.Codec;
                    string pp = condenser[ep];
                    data["Path"] = $"<{counter[pp].index}>{ep.Path.Substring(pp.Length)}";
                    data["Year"] = ep.Year.ToString();
                    data["Res"] = ep.Res.ToString();
                    results.Add(Condense(data));
                }
            }

            result.Add("Eps", Condense(results));
            return result;
        }

        public override void Read(Dictionary<string, string> data)
        {
            if (data.TryGetValue("Code", out var s) && int.TryParse(s, out int i)) Code = i;
            if (data.TryGetValue("id", out s)) id = s;
            if (data.TryGetValue("Name", out s)) Name = s;

            if (data.TryGetValue("Eps", out s) && Read(s) is List<string> list)
            {
                foreach (var entry in list)
                {
                    if (Read(entry) is Dictionary<string, string> dict)
                    {
                        if (!dict.TryGetValue("SNo", out s) || !int.TryParse(s, out i))
                            i = -1;

                        if (!Eps.TryGetValue(i, out var season))
                            season = new Season();
                        Eps.TryAdd(i, season);

                        if (dict.TryGetValue("Path", out s))
                        {
                            i = s.IndexOf("<");
                            int j = s.IndexOf(">");
                            if (i >= 0 && j > i)
                            {
                                var k = s.Substring(i + 1, j - i - 1);
                                if (dict.TryGetValue(k, out var mask))
                                {
                                    dict["Path"] = mask + s.Substring(j + 1);
                                }
                            }
                        }

                        // now do more thing
                        MediaFile file = new();
                        file.Read(dict);
                        season.Eps.Add(file);
                    }
                }
            }
        }



        /// <summary>
        /// Converts this series object into a json that describes
        /// the entire inner layout.
        /// 
        /// <para>It is expected that <see cref="FromJson(string)"/> can reconstruct
        /// this instance from the return of this method.</para>
        /// </summary>
        /// <returns></returns>

        /*
         * 
         * Helper methods
         *
         */

        private static string CapitalizeTitle(string title)
        {
            // List of small words that should not be capitalized unless they're the first or last word
            HashSet<string> smallWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "nor", "of", "on", "or", "so", "the", "to", "with"
        };

            // Split the title into words
            string[] words = title.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> formattedWords = new List<string>();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                bool isFirstOrLast = (i == 0 || i == words.Length - 1);

                // Capitalize the word if it's not a small word or it's the first or last word
                if (isFirstOrLast || !smallWords.Contains(word.ToLower()))
                {
                    formattedWords.Add(CapitalizeWord(word));
                }
                else
                {
                    formattedWords.Add(word.ToLower());
                }
            }

            // Join the formatted words back into a single string
            return string.Join(" ", formattedWords);
        }

        private static string CapitalizeWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            // Capitalize the first letter and make the rest lowercase
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }


    }
}
