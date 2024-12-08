using Cookie.Serializing;

namespace Cookie.ContentLibrary
{
    public class Title : ICanJson
    {


        /// <summary>
        /// A mapping of episodes in this title. It is expected that the server will provide
        /// unique remote access keys to fill this collection.
        /// </summary>
        public Dictionary<string, MediaFile> EpisodeList = new();

        /// <summary>
        /// A collection of all files in this title, organized in a Season-Episode-File structure.
        /// </summary>
        public Dictionary<string, Season> Eps { get; set; } = [];

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

        /// <summary>
        /// Converts this series object into a json that describes
        /// the entire inner layout.
        /// 
        /// <para>It is expected that <see cref="FromJson(string)"/> can reconstruct
        /// this instance from the return of this method.</para>
        /// </summary>
        /// <returns></returns>


        public string GetTargetIdentifier(SerializationEngine engine)
        {
            return "Title";
        }

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
