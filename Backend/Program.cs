using Backend.API;
using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using Cookie.Serializing;
using Cookie.TCP;
using System.Diagnostics;

namespace Backend.ServerLibrary
{
    public class Program
    {
        public static JsonSerialization localSerializer = new();
        public static JsonSerialization remoteSerializer = new();

        public static void Main(string[] args)
        {

            var b = new Controller<Program>(new Program());


            var l = new Login();

            b.Discover<Login>(l);

            var searcher = new Searcher("E:/");
            var lib = searcher.Enumerate(4);
            lib.CompressPaths();
            Directory.CreateDirectory("Testing");
            Stopwatch s = Stopwatch.StartNew();

            using var ms = new MemoryStream();
            Byter.ToBytes(ms, ((IDictable)lib).MakeDictionary());
            File.WriteAllBytes("Total.txt", ms.ToArray());


            foreach (var item in lib.FoundSeries)
            {
                try
                {
                    File.Delete($"Testing/{item.Value.Name}.txt");
                    using var file = File.OpenWrite($"Testing/{item.Value.Name}.txt");
                    Byter.ToBytes(file, ((IDictable)item.Value).MakeDictionary());
                }
                catch { }

            }
            Console.WriteLine("Time: " + s.Elapsed.TotalMilliseconds);


            Console.WriteLine("Done!");





        }






    }


}
