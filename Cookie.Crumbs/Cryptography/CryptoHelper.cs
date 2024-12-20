using System.Security.Cryptography;
using System.Text;

namespace Cookie.Cryptography
{
    public class CryptoHelper
    {
        private const string defaultKey = "43o87yreiuytw346vrte";

        /// <summary>
        /// Provides a consistent SHA256 hash of the provided input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Provides a numeric representation of the SHA hash of the given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int GetStringHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                int n = 0;
                for (int i = 0; i < hash.Length; i++)
                {
                    n ^= hash[i] << (i & 0x3);
                }
                return n;
            }
        }

        /// <summary>
        /// Generates a Key and IV for AES unique to this platform
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static (byte[] key, byte[] iv) GenerateKeyAndIV(string input)
        {
            // Use SHA256 to hash the input string
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashA = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                byte[] hashB = sha256.ComputeHash(Encoding.UTF8.GetBytes("splarg" + input));

                // Use the first 16 bytes for the AES key (for AES-128)
                var key = new byte[32];
                Array.Copy(hashA, 0, key, 0, 16);
                // Use the next 16 bytes for the IV
                var iv = new byte[16];
                Array.Copy(hashB, 16, iv, 0, 16);
                return (key, iv);
            }
        }

        /// <summary>
        /// Encrypts the given plaintext. Note: Currently returns same data on Browser.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Encrypt(string plainText)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText)).ToArray());
        }

        /// <summary>
        /// Decrypts the given plaintext. Matches input of <see cref="Encrypt(byte[])"/>
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Decrpyt(string plainText)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(plainText)));
        }

        public static byte[] Encrypt(byte[] data)
        {
#if !BROWSER
            using (Aes aes = Aes.Create())
            {
                (aes.Key, aes.IV) = GenerateKeyAndIV("43o87yreiuytw346vrte");

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
#else
            return data;
#endif
        }

        public static byte[] Decrypt(byte[] data)
        {
#if !BROWSER
            using (Aes aes = Aes.Create())
            {
                (aes.Key, aes.IV) = GenerateKeyAndIV("43o87yreiuytw346vrte");

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
#else
            return data;
#endif
        }




    }
}
