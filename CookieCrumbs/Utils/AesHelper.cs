using System.Security.Cryptography;
using System.Text;

namespace Cookie
{
    public class AesHelper
    {


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

        public static string Encrypt(string plainText)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText)).ToArray());
        }

        public static string Decrpyt(string plainText)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(plainText)));
        }

        public static byte[] Encrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                (aes.Key, aes.Key) = GenerateKeyAndIV("43o87yreiuytw346vrte");

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                (aes.Key, aes.Key) = GenerateKeyAndIV("43o87yreiuytw346vrte");

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }




    }
}
