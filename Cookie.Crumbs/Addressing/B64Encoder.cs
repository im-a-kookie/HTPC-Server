using System.Runtime.InteropServices;
using System.Text;

namespace Cookie.Addressing
{
    internal class B64Encoder<T> : IAddressEncoder
    {
        private static int Bits = 6;
        // Base64 character set
        public static readonly char[] Characters = new char[]
        {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
        'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
        'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
        'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
        };


        public byte[] Compound(byte[] data)
        {
            int pos = 0;
            StringBuilder sb = new StringBuilder(Marshal.SizeOf<T>());
            while (pos < GetTotalBitDensity())
            {
                int a = pos / 8;
                int b = (pos + Bits) / 8;
                if (a == b)
                {
                    int n = pos - a * 8;
                    int chunk = data[a] >> n & 0x3F;
                    sb.Append(Characters[chunk]);
                }
                else
                {
                    //get the two parts
                    int n = pos - a * 8;
                    int r = 8 - n;
                    int chunk = data[a] >> n & (1 << r) - 1;


                    //now get the N bits from b
                    r = 6 - r;
                    chunk |= (data[b] & (1 << r) - 1) << 6 - r;
                    sb.Append(Characters[chunk]);
                }

                pos += Bits;
            }

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public int GetBitsPerChar() => Bits;

        public int GetTotalBitDensity() => Bits * Marshal.SizeOf<T>();
    }
}
