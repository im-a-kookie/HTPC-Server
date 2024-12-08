using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
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

                if (result != 0) return result;
            }

            return xChunks.Count.CompareTo(yChunks.Count);
        }

        private List<string> SplitIntoChunks(string input)
        {
            List<string> chunks = new List<string>();
            string currentChunk = string.Empty;

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    if (!string.IsNullOrEmpty(currentChunk) && !char.IsDigit(currentChunk[0]))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = string.Empty;
                    }
                    currentChunk += c;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentChunk) && char.IsDigit(currentChunk[0]))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = string.Empty;
                    }
                    currentChunk += c;
                }
            }

            if (!string.IsNullOrEmpty(currentChunk))
                chunks.Add(currentChunk);

            return chunks;
        }
    }
}
