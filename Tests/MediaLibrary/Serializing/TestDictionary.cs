using Cookie.Serializers;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Collections;

namespace Tests.MediaLibrary.Serializing
{
    [TestClass]
    public class TestDictionary
    {


        [TestMethod]
        public void SamplePassesTests()
        {
            ValidateDictionary(CreateSampleDictionary());
        }

        private class SampleClass : IDictable
        {
            public string value = "sample_value";

            public void FromDictionary(IDictionary<string, object> dict)
            {
                value = (string)dict["value"];
            }

            public void ToDictionary(IDictionary<string, object> dict)
            {
                dict["value"] = value;
            }
        }

        public static List<string> CreateStringList()
        {
            return ["value0", "value1", "value2"];
        }

        public static List<int> CreateIntList()
        {
            return [0, 1, 2];
        }

        public static Dictionary<string, string> CreateStringStringDict()
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < 3; i++)
                dict.Add($"key{i}", $"value{i}");
            return dict;
        }

        public static Dictionary<int, int> CreateIntIntDict()
        {
            var dict = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
                dict.Add(i, i);
            return dict;
        }

        /// <summary>
        /// Creates a sample dictionary for the purposes of serializing. This dictionary is
        /// necessarily validatable via <see cref="ValidateDictionary(Dictionary{string, object})"/>
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> CreateSampleDictionary()
        {
            Dictionary<string, object> test = new()
            {
                { "string", "value" },
                { "int", 1 },
                { "float", 1.23 },
                { "byte", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 } },
                { "string_list", CreateStringList() },
                { "int_list", CreateIntList() },
                { "string_dict", CreateStringStringDict() },
                { "int_dict", CreateIntIntDict() }
            };

            // now let's generate nested lists

            List<List<string>> nested_lists = new();
            for (int i = 0; i < 3; i++) nested_lists.Add(CreateStringList());
            test.Add("nested_lists", nested_lists);

            Dictionary<string, object> nested_dict = new();
            nested_dict.Add("nested_string_list", CreateStringList());
            nested_dict.Add("nested_int_list", CreateIntList());
            nested_dict.Add("nested_string_dict", CreateStringStringDict());

            test.Add("nested_dict", nested_dict);

            test.Add("sample_class", new SampleClass());

            return test;
        }

        /// <summary>
        /// Validates a given dictionary for structure against that which is expected in this class
        /// </summary>
        /// <param name="input"></param>
        public static void ValidateDictionary(Dictionary<string, object> input)
        {
            Dictionary<string, (bool passed, string? message)> results = [];
            // string
            Check(results, "String", input["string"], "value");
            Check(results, "Integer", input["int"], 1);
            Check(results, "Float", input["float"], 1.23);
            Check<byte>(results, "Byte[]", input["byte"], [0, 1, 2, 3, 4, 5, 6, 7]);

            // check [value0...2] in stringlist
            // just use the underlying filler thing
            List<string> string_list = (List<string>)input["string_list"];
            Check(results, "String List", CreateStringList(), string_list);
            // same for int
            List<int> int_list = (List<int>)input["int_list"];
            Check(results, "Int List", CreateIntList(), int_list);

            // dictionary
            Dictionary<string, string> string_dict = (Dictionary<string, string>)input["string_dict"];
            Check(results, "String Dict", CreateStringStringDict(), string_dict);

            // non-string dictionary
            Dictionary<int, int> int_dict = (Dictionary<int, int>)input["int_dict"];
            Check(results, "Int Dict", CreateIntIntDict(), int_dict);

            SampleClass expect = new SampleClass();
            SampleClass got = (SampleClass)input["sample_class"];
            Check(results, "IDictable", expect.value, got.value);

            // Now check that we passed everything
            var failed = results.Where(x => x.Value.passed == false);
            foreach (var kv in failed)
            {
                ConsoleOutput.Instance.WriteLine($"Failed {kv.Key}: {kv.Value.message ?? ""}", OutputLevel.Error);
            }

            Assert.AreEqual(0, failed.Count(), "Failed Validation!");

        }

        /// <summary>
        /// Checks a collection into the result map
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        private static void Check<T>(Dictionary<string, (bool, string?)> result, string key, object input, IEnumerable<T> output)
        {
            try
            {
                var list = Enumerable.Cast<object>((IEnumerable)input);
                // check lengths
                if (list == null || list.Count() != output.Count())
                {
                    result[key] = (false, "Collection size non-matching!");
                    return;
                }
                // check each element
                for (int i = 0; i < list.Count(); i++)
                {
                    if (!list.ElementAt(i).Equals(output.ElementAt(i)))
                    {
                        result[key] = (false, $"Elements not equal at {i}");
                        return;
                    }
                }
                result[key] = (true, null);
            }
            catch (Exception e)
            {
                result[key] = (true, e.GetType().Name);
            }
        }

        /// <summary>
        /// Checks equality into the result map
        /// </summary>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        private static void Check(Dictionary<string, (bool, string?)> result, string key, object input, object output)
        {
            try
            {
                var correct = input.Equals(output);
                if (correct) result[key] = (true, null);
                else result[key] = (false, $"{input} != {output ?? "null"}");
            }
            catch (Exception e)
            {
                result[key] = (true, e.GetType().Name);
            }
        }

    }
}
