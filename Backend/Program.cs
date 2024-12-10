using Cookie.ContentLibrary;
using Cookie.Serializers;
using Cookie.Serializers.Nested;
using Cookie.Serializing;
using System.Diagnostics;
using System.Text;

namespace Backend.ServerLibrary
{
    public class Program
    {
        public static SerializationEngine localSerializer = new();
        public static SerializationEngine remoteSerializer = new();

        public static void Main(string[] args)
        {


            Dictionary<string, object> test = new();

            test.Add("banana", new Dictionary<string, string>());
            ((Dictionary<string, string>)test["banana"]).Add("0", "apple");
            ((Dictionary<string, string>)test["banana"]).Add("1", "orange");
            ((Dictionary<string, string>)test["banana"]).Add("2", "pear");
            test.Add("peanut", "butter");

            Dictionary<string, object> inner0 = new()
            {
                { "key0", "cake" },
                { "key1", "cookie" },
                { "key2", "pie" }
            };

            Dictionary<string, object> inner1 = new()
            {
                { "key0", "jelly" },
                { "key1", "custard" },
                { "key2", inner0 }
            };

            string b128 = Encoding.UTF8.GetBytes("ahskjfhskjdss").ToBase128();
            Console.WriteLine(b128);
            Console.WriteLine(Encoding.UTF8.GetString(b128.ToBytesBase128()));


            test.Add("deserts", inner1);
            test.Add("chocolate", new List<string>() { "milk", "cookies", "ice-cream" });

            var result = NestedEncoder.Condense(test, 0);
            NestedDecoder.Process($"{{{result}}}");

            var n = new Title("banana");
            var sb = new StringBuilder();


            var searcher = new Searcher("S:/");
            var lib = searcher.Enumerate(4);

            Directory.CreateDirectory("Testing");
            Stopwatch s = Stopwatch.StartNew();
            foreach (var item in lib.FoundSeries)
            {
                var str = item.ToString();
                File.WriteAllText($"Testing/{item.Value.Name}.txt", str);
            }
            Console.WriteLine("Time: " + s.Elapsed.TotalMilliseconds);


            Console.WriteLine("Done!");
        }






    }


}
