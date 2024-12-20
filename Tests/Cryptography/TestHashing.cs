using Cookie.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Cryptography
{
    [TestClass]
    public class TestHashing
    {

        /// <summary>
        /// Tests correctness of SHA256 against known reference
        /// </summary>
        [TestMethod]
        public void TestSha256()
        {
            string input = "test";
            string hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
            string result = CryptoHelper.HashSha256(input);
            Assert.AreEqual(hash, result, "Incorrect hash!");
        }

        /// <summary>
        /// Tests correctness of SHA1 against known reference
        /// </summary>
        [TestMethod]
        public void TestSha1()
        {
            string input = "test";
            string hash = "a94a8fe5ccb19ba61c4c0873d391e987982fbbd3";

            string result = CryptoHelper.HashSha1(input);
            Assert.AreEqual(hash, result, "Incorrect hash!");
        }

        /// <summary>
        /// Tests that hasher algorithm provides variable length hashes
        /// </summary>
        [TestMethod]
        public void TestVariableHashLength()
        {
            string input = "test";

            // Test truncation
            string hash = "a94a8fe5ccb19ba61c4c0873d391e9";
            string result = CryptoHelper.HashSha1(input, hash.Length);
            Assert.AreEqual(hash, result, "Incorrect hash!");

            // and test extension
            hash = "a94a8fe5ccb19ba61c4c0873d391e987982fbbd3a94a8fe5ccb19ba61c4c0873d391e987";
            result = CryptoHelper.HashSha1(input, hash.Length);
            Assert.AreEqual(hash, result, "Incorrect hash!");
        }


    }
}
