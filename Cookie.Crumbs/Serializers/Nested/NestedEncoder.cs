using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Text;
using static Cookie.Serializers.SerializationConstants;

namespace Cookie.Serializers.Nested
{
    /// <summary>
    /// Establishes some utility methods for <see cref="NestedSerial"/>
    /// </summary>
    public class NestedEncoder
    {

       
        public static bool Indent = true;

        /// <summary>
        /// Serializes an object into a custom condensed format based on its type.
        /// </summary>
        /// <param name="data">The data to serialize.</param>
        /// <param name="depth">The current depth of serialization, used for indentation.</param>
        /// <returns>A condensed string representation of the object.</returns>
        /// <exception cref="SerializationException">Thrown if the object's type cannot be serialized.</exception>
        internal static string condense(object data, int depth)
        {
            // StringBuilder to construct the serialized output.
            StringBuilder sb = new();
            string indent = "".PadRight(depth, '\t'); // Generate indentation based on depth.

            switch (data)
            {
                case int i:
                    // Serialize integers with 'i' prefix.
                    sb.Append($"{Hint}{i};");
                    break;
                case float f:
                    // Serialize floats with 'f' prefix.
                    sb.Append($"{Hfloat}{f};");
                    break;
                case double d:
                    // Serialize doubles with 'f' prefix (similar to floats).
                    sb.Append($"{Hdouble}{d};");
                    break;
                case string s:
                    // Serialize strings with 's' prefix.
                    sb.Append($"{Hstring}{s.Replace($"{Terminator}", $"&#{(int)Terminator}{AltTerminator}")}{Terminator}");
                    break;
                case byte[] b:
                    // Serialize strings with 's' prefix.
                    sb.Append($"{Hbyte}{b.ToBase128()};");
                    break;
                case List<string> l:
                    // Serialize a list of strings.
                    sb.Append($"{Condense(l, depth + 1)};");
                    break;
                case List<object> l:
                    // Serialize a list of objects.
                    sb.Append($"{Condense(l, depth + 1)};");
                    break;
                case Dictionary<string, string> d:
                    // Serialize a dictionary with string keys and values.
                    sb.Append($"{Condense(d, depth + 1)};");
                    break;
                case Dictionary<string, object> d:
                    // Serialize a dictionary with string keys and object values.
                    sb.Append($"{Condense(d, depth + 1)}");
                    break;
                default:
                    // If the data type is not supported, throw an exception.
                    throw new SerializationException($"Could not serialize {data.GetType()}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Serializes a dictionary with string keys and object values into a condensed format.
        /// </summary>
        /// <param name="data">The dictionary to serialize.</param>
        /// <param name="depth">The current depth of serialization, used for indentation.</param>
        /// <returns>A condensed string representation of the dictionary.</returns>
        public static string Condense(Dictionary<string, object> data, int depth)
        {
            StringBuilder sb = new();
            string indent = "".PadRight(depth, Tab); // Generate indentation for current depth.

            // Serialize each key-value pair in the dictionary.
            var results = data.Select(x =>
            {
                var val = condense(x.Value, depth);

                // If the serialized value is a collection or complex structure, add indentation and braces.
                if (val[0] == Hlist || val[0] == Hdict || val[0] == Hmap)
                {
                    return $"{x.Key}{PropDelim}\n{indent}{OpenGroup}{val[0]}\n{indent}{Tab}{val.Substring(1)}\n{indent}{CloseGroup}{Terminator}";
                }

                // For simple values, serialize directly with a key.
                return $"{x.Key}{PropDelim}{val}";
            });

            // Append all serialized results to the StringBuilder.
            foreach (var s in results)
            {
                sb.Append($"{indent}{s}\n");
            }

            return $"{Hmap}{sb.ToString().Trim()}"; // Prefix with 'm' to indicate a dictionary.
        }

        /// <summary>
        /// Serializes a dictionary with string keys and values into a condensed format.
        /// </summary>
        /// <param name="data">The dictionary to serialize.</param>
        /// <param name="depth">The current depth of serialization, used for indentation.</param>
        /// <returns>A condensed string representation of the dictionary.</returns>
        internal static string Condense(Dictionary<string, string> data, int depth)
        {
            // Serialize by converting the dictionary to a list of key-value pairs joined by '~'.
            string result = Condense(data.Select(x => $"{x.Key}{KeyValDelim}{x.Value}").ToList(), depth);

            // Prefix with 'd' to indicate a dictionary.
            return Hdict + result.Substring(1);
        }

        /// <summary>
        /// Serializes a list of strings into a condensed format.
        /// </summary>
        /// <param name="data">The list of strings to serialize.</param>
        /// <param name="depth">The current depth of serialization, used for indentation.</param>
        /// <returns>A condensed string representation of the list.</returns>
        internal static string Condense(List<string> data, int depth)
        {
            string indent = "".PadRight(depth, Tab); // Generate indentation for current depth.
            StringBuilder sb = new();

            // Join list elements with ';' and separate by indentation.
            sb.Append($"{Hlist}{string.Join($"{Terminator}\n{indent}", data)}");

            return sb.ToString();
        }

        /// <summary>
        /// Serializes a list of objects into a condensed format.
        /// </summary>
        /// <param name="data">The list of objects to serialize.</param>
        /// <param name="depth">The current depth of serialization, used for indentation.</param>
        /// <returns>A condensed string representation of the list.</returns>
        internal static string Condense(List<object> data, int depth)
        {
            string indent = "".PadRight(depth, Tab); // Generate indentation for current depth.
            StringBuilder sb = new();

            // Serialize each object in the list.
            var results = data.Select(x => condense(x, depth + 1));

            // Join serialized elements with ';' and separate by indentation.
            sb.Append($"{Hlist}{string.Join($"{Terminator}\n{indent}", results)}");

            return sb.ToString();
        }



    }

}
