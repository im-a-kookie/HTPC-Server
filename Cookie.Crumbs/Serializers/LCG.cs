namespace Cookie.Serializers
{
    internal class LCG
    {
        private const ulong m = 4294967296; // aka 2^32
        private const ulong a = 1664525;
        private const ulong c = 1013904223;
        private ulong _last;

        public LCG()
        {
            _last = (ulong)DateTime.UtcNow.Ticks % m;
        }

        public LCG(ulong seed)
        {
            _last = seed;
        }

        /// <summary>
        /// Returns the next random double value from this LCG
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            return Next() / (double)m;
        }

        /// <summary>
        /// Returns the next Int32 from this LCG
        /// </summary>
        /// <returns></returns>
        public int Next()
        {
            _last = ((a * _last) + c) % m;
            return (int)(_last & int.MaxValue);
        }

        /// <summary>
        /// Returns the next Int32 from this LCG with the given int constraint
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public int Next(int maxValue)
        {
            return Next() % maxValue;
        }

        public void Shuffle<T>(IList<T> input)
        {
            for (int i = input.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                // Swap arr[i] and arr[j]
                (input[i], input[j]) = (input[j], input[i]);
            }
        }

        public void Shuffle<T>(T[] input)
        {
            for (int i = input.Length - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                // Swap arr[i] and arr[j]
                (input[i], input[j]) = (input[j], input[i]);
            }
        }

    }
}
