using System.Text;

namespace Cookie.Serializers
{
    public static class Base128
    {
        /// <summary>
        /// A collection of potentially valid characters. Some of them aren't 0-255 range in ASCII
        /// but removing them is annoying and there are enough that it works just fine
        /// </summary>
        public static readonly string ValidCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef" +
            "ghijklmnopqrstuvwxyz€ƒ†‡‰ŠŒŽ•šœž" +
            "Ÿ¢£¤¥§©µ¶¼½¾¿®°ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐ" +
            "ÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïð" +
            "ñòóôõö÷øùúûüýþÿ";

        /// <summary>
        ///  Internal character array for converting a 7 bit value (0-127) into a readable(?) character
        /// </summary>
        private static readonly char[] ValueToChar;

        private static readonly bool[] ValidChar;

        /// <summary>
        /// Internal array mapping the valid base128 characters to 7 bit (0-128) binary values
        /// </summary>
        private static byte[] CharToValue = new byte[256];

        /// <summary>
        ///  Generate the 1:1 mapping
        /// </summary>
        static Base128()
        {
            ValueToChar = new char[128];
            ValidChar = new bool[256];
            var valid = ValidCharacters.ToCharArray().Where(x => x <= 255).ToHashSet().ToArray();
            // Shuffle it for funs
            new LCG(238525).Shuffle(valid);

            if (valid.Length < 128) throw new Exception("BONK!");
            // and populate using the first 128 characters
            for (int i = 0; i < 128; i++)
            {
                ValueToChar[i] = valid[i];
                ValidChar[valid[i]] = true;
                CharToValue[(byte)ValueToChar[i]] = (byte)i;
            }
        }

        public static byte[] FromBase128(string text)
        {
            if (text == null || text.Length <= 0)
                return [];

            int bitBuffer = 0; // Buffer to store bits
            int bitCount = 0; // Count of bits in the buffer

            int bitsRead = 0;
            int RealLen = (text.Length * 7);

            // Calculate the overshot/padding
            int overshot = 0;
            if (text[^1] >= '1' && text[^1] <= '7')
            {
                overshot = text[^1] - '0';
                RealLen -= overshot;
                RealLen -= 7;
            }
            RealLen /= 8;
            MemoryStream ms = new(RealLen);

            foreach (char c in text)
            {
                if (c >= '1' && c <= '7') break;

                int value = (int)c; // Get the 7-bit value from the character
                value = CharToValue[value & 0xFF];
                if (!ValidChar[c])
                    throw new FormatException($"Invalid Base128 Character {c}");


                string sc = value.ToString("B8");

                // This represents writing 7 bits
                // So read let's take the value and offset it by how many bits are left
                bitBuffer |= value << bitCount;
                bitCount += 7;
                bitsRead += 7;

                string s = bitBuffer.ToString("B16");
                int bb = bitBuffer & 0xFF;

                // Process complete 8-bit bytes
                while (bitCount >= 8 && RealLen > 0)
                {
                    --RealLen;
                    ms.WriteByte((byte)(bitBuffer & 0xFF));
                    bitBuffer >>= 8; // Shift buffer to remove the bits we've used
                    bitCount -= 8; // Adjust bit count
                    s = bitBuffer.ToString("B16");
                }
            }

            return ms.ToArray();
        }

        public static string ToBase128(ReadOnlySpan<byte> data)
        {
            StringBuilder sb = new StringBuilder((data.Length * 8) / 7 + 3);

            int bitBuffer = 0; // Buffer to store bits
            int bitCount = 0; // Count of bits in the buffer

            for (int i = 0; i < data.Length; i++)
            {
                byte currentByte = data[i];

                // Add bits to the buffer until we have at least 7 bits
                bitBuffer |= currentByte << bitCount;
                bitCount += 8;

                // Process full 7-bit chunks
                while (bitCount >= 7)
                {
                    // mask out the lower 7 bits and append
                    int value = (bitBuffer & 0x7F);
                    sb.Append(ValueToChar[value]);
                    bitBuffer >>= 7;
                    bitCount -= 7;
                }

            }

            // Handle any remaining bits (less than 7 bits) at the end
            if (bitCount > 0)
            {
                int rem = 7 - bitCount;
                int value = (bitBuffer & 0x7F); // Mask to get the remaining bits
                sb.Append(ValueToChar[value]); // Convert to character and append
                // indicate how many bits were appended
                sb.Append((char)('0' + rem));
            }

            return sb.ToString();

        }


        /// <summary>
        ///  Easy extension method
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToBase128(this byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));

            return ToBase128(byteArray.AsSpan());
        }

        public static byte[] ToBytesBase128(this string text)
        {
            return FromBase128(text);
        }


    }
}
