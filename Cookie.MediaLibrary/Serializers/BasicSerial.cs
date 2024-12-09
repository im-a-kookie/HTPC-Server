using System.Text;

namespace Cookie.Serializers
{
    public abstract class BasicSerial
    {

        public abstract Dictionary<string, string> Write();

        public abstract void Read(Dictionary<string, string> data);

        public override string ToString()
        {
            return Condense(Write());
        }

        /// <summary>
        /// Reads the given input as an object, that is either a dictionary of strings-strings, a list of strings,
        /// or a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static object? Read(string input)
        {
            if (input.StartsWith("{dict:"))
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                var str = input.Remove(input.Length - 1).Substring(6);
                foreach (var result in ReadList(str))
                {
                    var parts = result.Split('=');
                    if (parts.Length == 2)
                    {
                        dict.Add(parts[0], Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])));
                    }
                }
                return dict;
            }
            else if (input.StartsWith("{list:"))
            {
                var str = input.Remove(input.Length - 1).Substring(6);
                return ReadList(str);
            }
            else
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(input));
            }
        }

        /// <summary>
        /// Reads a list of strings from the given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static List<string> ReadList(string input)
        {
            List<string> result = new();
            var str = input.Remove(input.Length - 1).Substring(6);
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(str));
            var parts = data.Split(";");
            foreach (string val in parts)
            {
                if (string.IsNullOrWhiteSpace(val)) continue;
                result.Add(val);
            }
            return result;
        }


        /// <summary>
        /// Condense a dictionary of string-string into a string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Condense(Dictionary<string, string> data)
        {
            StringBuilder sb = new();
            foreach (var k in data)
            {
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(k.Value));
                sb.Append($"{k.Key}:{value};");
            }
            return "{dict:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())) + "}";
        }

        /// <summary>
        /// Condense a dictionary of string-object into a value string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Condense(Dictionary<string, object> data)
        {
            StringBuilder sb = new();
            foreach (var k in data)
            {
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(k.Value.ToString()!));
                sb.Append($"{k.Key}:{value};");
            }
            return "{dict:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())) + "}";
        }

        /// <summary>
        /// condense a list of strings into a value string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Condense(List<string> data)
        {
            StringBuilder sb = new();
            foreach (var k in data)
            {
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(k));
                sb.Append($"{value};");
            }
            return "{list:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())) + "}";
        }

        /// <summary>
        /// Condense a list of objects into a value string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Condense(List<object> data)
        {
            StringBuilder sb = new();
            foreach (var k in data)
            {
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(k.ToString()!));
                sb.Append($"{value};");
            }
            return "{list:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())) + "}";
        }

    }
}
