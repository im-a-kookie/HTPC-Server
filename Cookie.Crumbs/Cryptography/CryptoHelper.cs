using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cookie.Cryptography
{
    public class CryptoHelper
    {

        public static Func<string> DefaultKey = () => "43o87yreiuytw346vrte";

        /// <summary>
        /// Hashes a string with the given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashString(string input, int maxLength = -1)
        {
            return HashSha1(input);
        }

        /// <summary>
        /// Provides a consistent SHA256 hash of the provided input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashSha256(string input, int maxLength = -1)
        {
            using var sha = SHA256.Create();
            return HashAsHex(sha, input, maxLength);
        }

        /// <summary>
        /// Provides a consistent SHA256 hash of the provided input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashSha1(string input, int maxLength = -1)
        {
            using var sha = SHA1.Create();
            return HashAsHex(sha, input, maxLength);
        }

        /// <summary>
        /// Hashes the given value to a hexdecimal string of the given length
        /// </summary>
        /// <param name="hasher"></param>
        /// <param name="data"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string HashAsHex(HashAlgorithm hasher, string data, int maxLength = -1)
        {
            if (maxLength < 0)
            {
                return BitConverter.ToString(HashData(hasher, data)).Replace("-", "").ToLowerInvariant();
            }
            else
            {
                // just loop the hash into the result and then trim it
                StringBuilder result = new(maxLength);
                string hash = HashAsHex(hasher, data, -1);
                while (result.Length < maxLength)
                {
                    result.Append(hash);
                }
                string value = result.ToString();
                if (value.Length > maxLength) value = value.Remove(maxLength);
                return value.ToString();
            }
        }


        /// <summary>
        /// Hashes the given value to a hexdecimal string of the given length
        /// </summary>
        /// <param name="hasher"></param>
        /// <param name="data"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string HashAsHex(HashAlgorithm hasher, Stream data, int maxLength = -1)
        {
            if(maxLength < 0)
            {
                return BitConverter.ToString(HashData(hasher, data)).Replace("-", "").ToLowerInvariant();
            }
            else
            {
                // just loop the hash into the result and then trim it
                StringBuilder result = new(maxLength);
                string hash = HashAsHex(hasher, data, -1);
                while(result.Length < maxLength)
                {
                    result.Append(hash);
                }
                string value = result.ToString();
                if (value.Length > maxLength) value = value.Remove(maxLength);
                return value.ToString();
            }
        }


        /// <summary>
        /// Funnel for hashing with given algorithm/stream
        /// </summary>
        /// <param name="hasher"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] HashData(HashAlgorithm hasher, Stream data)
        {
            return hasher.ComputeHash(data);
        }

        /// <summary>
        /// Funnel for hashing with given algorithm/stream
        /// </summary>
        /// <param name="hasher"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] HashData(HashAlgorithm hasher, string data)
        {
            return hasher.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Provides a numeric representation of the SHA hash of the given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int GetStringHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            int n = 0;
            for (int i = 0; i < hash.Length; i++)
            {
                n ^= hash[i] << (i & 0x3);
            }
            return n;
        }

        /// <summary>
        /// Generates a Key and IV for AES unique to this platform
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static (byte[] key, byte[] iv) GenerateKeyAndIV(string input)
        {
            // Use SHA256 to hash the input string
            using var sha256 = SHA256.Create();
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
        public static string Decrypt(string plainText)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(plainText)));
        }

        /// <summary>
        /// Encrypts the given data with a key specific to this application
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] data)
        {
#if !BROWSER
            using var aes = Aes.Create();
            (aes.Key, aes.IV) = GenerateKeyAndIV(DefaultKey());

            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return memoryStream.ToArray();
#else
            return data;
#endif
        }

        /// <summary>
        /// Decrypts the given data as provided by <see cref="Encrypt(byte[])"/>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] data)
        {
#if !BROWSER
            using (Aes aes = Aes.Create())
            {
                (aes.Key, aes.IV) = GenerateKeyAndIV(DefaultKey());

                using MemoryStream memoryStream = new();
                using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
#else
            return data;
#endif
        }




    }
}
