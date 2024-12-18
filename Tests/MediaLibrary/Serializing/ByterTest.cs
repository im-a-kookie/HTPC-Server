using Cookie.Serializers.Bytewise;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Text;

namespace Tests.MediaLibrary.Serializing
{

    [TestClass]
    public class ByterTest
    {

        [TestMethod]
        public void Test_RoundTrip()
        {
            var dict = TestDictionary.CreateSampleDictionary();
            using MemoryStream ms = new MemoryStream();
            Byter.ToBytes(ms, dict);

            var str = Encoding.UTF8.GetString(ms.ToArray());
            ConsoleOutput.Instance.WriteLine(str, OutputLevel.Error);

            ms.Seek(0, SeekOrigin.Begin);
            var result = Byter.FromBytes(ms);

            TestDictionary.ValidateDictionary(dict);

        }


    }
}
