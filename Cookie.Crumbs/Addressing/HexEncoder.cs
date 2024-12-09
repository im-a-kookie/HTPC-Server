using System.Runtime.InteropServices;
using System.Text;

namespace Cookie.Addressing
{
    internal class HexEncoder<T> : IAddressEncoder
    {
        private static int Bits = 4;
        private static string[] Lookup;
        private static int Size;
        private static int TotalBits;
        private static int UsableSize;

        static HexEncoder()
        {
            Size = Marshal.SizeOf<T>();
            TotalBits = Size * Bits;
            UsableSize = (TotalBits + 7) / 8;

            //the hex chars are from 0 to 255 so
            Lookup = new string[256];
            for (int i = 0; i < 256; i++)
            {
                //single char hex
                Lookup[i] = ((byte)i).ToString("X2");
            }
        }

        /// <summary>
        /// Compounds the given bytes into a hex string. The length of the bytes will match
        /// the byte length of 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Compound(byte[] data)
        {

            StringBuilder sb = new StringBuilder(data.Length * 2);
            for (int i = 0; i < UsableSize; i++)
            {
                sb.Append(Lookup[data[i]]);
            }
            return Encoding.ASCII.GetBytes(sb.ToString().Remove(Size));
        }

        public int GetBitsPerChar() => Bits;

        public int GetTotalBitDensity() => TotalBits;

    }
}
