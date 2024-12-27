using Cookie.Cryptography;
using Cookie.Serializers;
using System.Text.Json;

namespace Cookie.ContentLibrary
{
    public class Title : IDictable
    {
        public Library? Owner { get; set; }

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


        private string _id;
        /// <summary>
        /// Gets or sets the id for this title. This should be unique.
        /// </summary>
        public string ID
        {
            get { return _id; }
            set
            {
                if (Owner != null)
                {
                    lock (Owner)
                    {
                        Owner.FoundSeries.TryRemove(_id, out _);
                        _id = value;
                        Owner.FoundSeries.TryAdd(_id, this);
                        Owner.NotifySeriesUpdate([this]);
                    }
                }
                else _id = value;
            }
        }

        private string _name;
        /// <summary>
        /// Gets or sets a readable title for this series. This does not
        /// have to be unique.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (Owner != null)
                {
                    lock (Owner)
                    {
                        Owner.NameToSeries.TryRemove(_id, out _);
                        _name = value;
                        Owner.NameToSeries.TryAdd(_id, this);
                        Owner.NotifySeriesUpdate([this]);
                    }
                }
                else _name = value;
            }
        }

        public string Link { get; set; } = "";

        public string Description { get; set; } = "";

        public Title()
        {
            _name = "Default";
            _id = CryptoHelper.HashSha1(Path.GetRandomFileName(), 10);
        }

        /// <summary>
        /// Instantiate a new title with the given id value
        /// </summary>
        /// <param name="id"></param>
        public Title(string id)
        {
            this._name = CapitalizeTitle(id);
            this._id = CryptoHelper.HashSha1(Name, 10);
        }

        /// <summary>
        /// A simple prediction of whether this Title is a movie or a series, based on the
        /// number of files that it contains
        /// </summary>
        public bool PredictMovie => EpisodeList.Count == 1;

        private static readonly char[] separator = new[] { ' ', '-', ':' };

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["N"] = Name;
            dict["I"] = ID;
            dict["C"] = Code;
            List<MediaFile> episodes = EpisodeList.Values.ToList();
            dict["E"] = episodes;

            dict["D"] = Description;
            dict["L"] = Link;

        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            Name = (string)dict["N"];
            ID = (string)dict["I"];
            Code = (int)dict["C"];

            Description = (string)dict["D"];
            Link = (string)dict["L"];

            var files = (List<MediaFile>)dict["E"];
            EpisodeList.Clear();
            int lastSeason = -1;
            Season? season = null;
            foreach (var file in files)
            {
                if (lastSeason != file.SNo || season == null)
                {
                    if (!Eps.TryGetValue(file.SNo, out season))
                    {
                        Eps.TryAdd(file.SNo, season = new());
                    }
                }
                season.Eps.Add(file);
                EpisodeList.Add($"{file.SNo}x{file.EpNo}", file);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            return (obj is Title t && t.Name == this.Name && t.ID == this.ID);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.ID.GetHashCode();
        }


        /*
         * 
         * Helper methods
         *
         */

        /// <summary>
        /// Capitalizes the words in a title with exclusions for words that are traditionally not capitalized.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string CapitalizeTitle(string title)
        {
            // List of small words that should not be capitalized unless they're the first or last word
            HashSet<string> smallWords = new(StringComparer.OrdinalIgnoreCase)
            {
                "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "nor", "of", "on", "or", "so", "the", "to", "with"
            };

            // Split the title into words
            string[] words = title.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> formattedWords = [];

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
            return char.ToUpper(word[0]) + word[1..].ToLower();
        }

        public class Packer
        {
            /// <summary>
            /// The name of this series
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The description of this series
            /// </summary>
            public string Desc { get; set; }
            /// <summary>
            /// The link to this series
            /// </summary>
            public string Link { get; set; }
            /// <summary>
            /// The seasons in this series
            /// </summary>
            public int Seasons { get; set; }
            /// <summary>
            /// The total number of episodes in this series
            /// </summary>
            public int Eps { get; set; }
            /// <summary>
            /// A list of the episodes in this series by their string lookup key thingy
            /// </summary>
            public Dictionary<string, int> Episodes { get; set; }

            public Packer(Title t)
            {
                Name = t.Name;
                Desc = t.Description;
                Link = t.Link;
                Seasons = t.Eps.Count;
                Eps = t.EpisodeList.Count;
                Episodes = new Dictionary<string, int>();
                foreach (var ep in t.EpisodeList)
                {
                    Episodes.Add(ep.Key, ep.Value.FileLookup);
                }
            }

            // Serialize to JSON
            public string Serialize()
            {
                return JsonSerializer.Serialize(this);
            }

            // Deserialize from JSON
            public static Packer? Deserialize(string json)
            {
                return JsonSerializer.Deserialize<Packer>(json);
            }
        }



    }
}
