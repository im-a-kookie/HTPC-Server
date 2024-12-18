namespace Cookie.Utils
{
    public class NaturalStringComparer : IComparer<string>
    {
        /// <summary>
        /// Compare x to y using natural string comparison
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Split strings into chunks of digits and non-digits
            var xChunks = SplitIntoChunks(x);
            var yChunks = SplitIntoChunks(y);

            int maxLength = Math.Min(xChunks.Count, yChunks.Count);

            for (int i = 0; i < maxLength; i++)
            {
                int result;

                // Compare numeric chunks as numbers
                if (int.TryParse(xChunks[i], out int xNumber) && int.TryParse(yChunks[i], out int yNumber))
                {
                    result = xNumber.CompareTo(yNumber);
                }
                else
                {
                    // Compare non-numeric chunks as strings
                    result = string.Compare(xChunks[i], yChunks[i], StringComparison.OrdinalIgnoreCase);
                }

                // return now that we've found a point of imbalance
                if (result != 0) return result;
            }

            // otherwise, compare the entire strings
            return xChunks.Count.CompareTo(yChunks.Count);
        }

        /// <summary>
        /// Divides the given string into chunks that separate numeric and non-numeric values
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private List<string> SplitIntoChunks(string input)
        {
            List<string> chunks = new List<string>();
            string currentChunk = string.Empty;

            foreach (char c in input)
            {
                // Flip-flop when we find digits/non-digits
                // alternately built the chunk, or make a new chunk to build
                if (char.IsDigit(c))
                {
                    // flip flip the numeric chunk if the current chunk is non-numeric
                    if (!string.IsNullOrEmpty(currentChunk) && !char.IsDigit(currentChunk[0]))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = string.Empty;
                    }
                    currentChunk += c;
                }
                else
                {
                    // and flip-flop the string
                    if (!string.IsNullOrEmpty(currentChunk) && char.IsDigit(currentChunk[0]))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = string.Empty;
                    }
                    currentChunk += c;
                }
            }

            // Make sure to catch the last chunk
            if (!string.IsNullOrEmpty(currentChunk))
                chunks.Add(currentChunk);

            return chunks;
        }
    }
}
