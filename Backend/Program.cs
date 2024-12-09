using Cookie.ContentLibrary;
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

            Dictionary<string, object> inner0 = new();
            inner0.Add("key0", "cake");
            inner0.Add("key1", "cookie");
            inner0.Add("key2", "pie");

            Dictionary<string, object> inner1 = new();
            inner1.Add("key0", "jelly");
            inner1.Add("key1", "custard");
            inner1.Add("key2", inner0);

            test.Add("deserts", inner1);
            test.Add("chocolate", new List<string>() { "milk", "cookies", "ice-cream" });


            var result = new NestedSerial().Condense(test, 0);

            new NestedSerial().Process($"{{{result}}}");



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
