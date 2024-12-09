namespace Cookie.Addressing
{
    public interface IAddressEncoder
    {
        /// <summary>
        /// Gets number of bits per character (or the actual bits-per-byte) encoded by this format.
        /// </summary>
        /// <returns>The number of bits represented by each encoded byte/character</returns>
        /// <remarks>
        /// The return of <see cref="Compound(byte[])"/> is expected to be representable 1:1 as an ASCII string,
        /// therefore this represents the width of the ASCII selection (e.g Hex -> 4 bits).
        /// </remarks>
        public int GetBitsPerChar();

        /// <summary>
        /// The total bit density of this encoder. For example, if associated with an expected encoding of
        /// B bytes, then the density is given by;
        /// <code>Density = B * GetBitsPerChar()</code>
        /// </summary>
        /// <returns>The total number of bits of information to be expected by <see cref="Compound(byte[])"/></returns>
        public int GetTotalBitDensity();

        /// <summary>
        /// Compounds the given input data into a new byte array.
        /// </summary>
        /// <param name="data">The input data</param>
        /// <returns>An array of ASCII codes representing the first N bits, where N matches <see cref="GetTotalBitDensity"/>
        /// </returns>
        public byte[] Compound(byte[] data);

    }
}
