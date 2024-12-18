namespace Cookie.Utils
{
    /// <summary>
    /// https://stackoverflow.com/a/19165108/
    /// </summary>
    public static class JaroWinklerDistance
    {
        /* The Winkler modification will not be applied unless the 
         * percent match was at or above the mWeightThreshold percent 
         * without the modification. 
         * Winkler's paper used a default value of 0.7
         */
        private static readonly double mWeightThreshold = 0.7;

        /* Size of the prefix to be concidered by the Winkler modification. 
         * Winkler's paper used a default value of 4
         */
        private static readonly int mNumChars = 4;


        /// <summary>
        /// Returns the Jaro-Winkler distance between the specified  
        /// strings. The distance is symmetric and will fall in the 
        /// range 0 (perfect match) to 1 (no match). 
        /// </summary>
        /// <param name="aString1">First String</param>
        /// <param name="aString2">Second String</param>
        /// <returns></returns>
        public static double Distance(string aString1, string aString2)
        {
            return 1.0 - Proximity(aString1, aString2);
        }


        /// <summary>
        /// Calculates the Jaro-Winkler distance between two strings. 
        /// The distance is symmetric and falls in the range [0, 1], 
        /// where 0 represents no match and 1 represents a perfect match.
        /// </summary>
        /// <param name="aString1">The first string to compare.</param>
        /// <param name="aString2">The second string to compare.</param>
        /// <returns>The Jaro-Winkler similarity score as a double.</returns>
        public static double Proximity(string aString1, string aString2)
        {
            // Lengths of the input strings
            int len1 = aString1.Length;
            int len2 = aString2.Length;

            // If both strings are empty, similarity is perfect
            if (len1 == 0)
                return len2 == 0 ? 1.0 : 0.0;

            // Search range is half the length of the longer string, minus 1
            int searchRange = Math.Max(0, Math.Max(len1, len2) / 2 - 1);

            // Boolean arrays to track matched characters
            bool[] matched1 = new bool[len1];
            bool[] matched2 = new bool[len2];

            // Count the number of matching characters
            int numCommon = 0;
            for (int i = 0; i < len1; ++i)
            {
                int start = Math.Max(0, i - searchRange);
                int end = Math.Min(i + searchRange + 1, len2);

                for (int j = start; j < end; ++j)
                {
                    if (matched2[j]) continue; // Already matched
                    if (aString1[i] != aString2[j]) continue; // Characters do not match

                    // Mark characters as matched
                    matched1[i] = true;
                    matched2[j] = true;
                    ++numCommon; // Increment the count of common characters
                    break;
                }
            }

            // If no common characters, similarity is zero
            if (numCommon == 0)
                return 0.0;

            // Count the number of transpositions (half transpositions are calculated first)
            int numHalfTransposed = 0;
            int k = 0; // Index for the second string
            for (int i = 0; i < len1; ++i)
            {
                if (!matched1[i]) continue; // Skip unmatched characters
                while (!matched2[k]) ++k; // Advance to the next matched character in the second string
                if (aString1[i] != aString2[k])
                    ++numHalfTransposed; // Count mismatched but matched positions
                ++k; // Move to the next character
            }
            int numTransposed = numHalfTransposed / 2;

            // Calculate the Jaro similarity weight
            double numCommonD = numCommon;
            double weight = (numCommonD / len1 + numCommonD / len2 + (numCommon - numTransposed) / numCommonD) / 3.0;

            // If the weight is below the threshold, return the calculated weight
            if (weight <= mWeightThreshold)
                return weight;

            // Calculate the Winkler adjustment
            int maxPrefix = Math.Min(mNumChars, Math.Min(aString1.Length, aString2.Length));
            int prefixLength = 0;
            while (prefixLength < maxPrefix && aString1[prefixLength] == aString2[prefixLength])
                ++prefixLength;

            // If there is no common prefix, return the weight
            if (prefixLength == 0)
                return weight;

            // Apply the Winkler adjustment: boost based on common prefix
            return weight + 0.1 * prefixLength * (1.0 - weight);
        }


    }
}