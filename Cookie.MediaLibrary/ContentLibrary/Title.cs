﻿using Cookie.Serializers;

namespace Cookie.ContentLibrary
{
    public class Title : IDictable
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


        public Title()
        {
            id = Path.GetRandomFileName();
            Name = "Default";
        }

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


        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["N"] = Name;
            dict["I"] = id;
            dict["C"] = Code;

            List<MediaFile> episodes = EpisodeList.Values.ToList();
            dict["E"] = episodes;
        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            Name = (string)dict["N"];
            id = (string)dict["I"];
            Code = (int)dict["C"];

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
            return (obj is Title t && t.Name == this.Name && t.id == this.id);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.id.GetHashCode();
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
