using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CookieCrumbs.Serializing
{
    public class SerializationEngine
    {
        public const string ID = "_id_";


        /// <summary>
        /// A mode flag that can be used to inform a serialization child of the mode
        /// that this serializer is running in. In general, this is used to differentiate
        /// identifier tags in <see cref="ICanJson.GetTargetIdentifier(SerializationEngine)"/> and 
        /// </summary>
        public HostMode Mode { get; private set; }

        /// <summary>
        /// Initializes a serializer in the given mode
        /// </summary>
        /// <param name="mode"></param>
        public SerializationEngine(HostMode mode = HostMode.LOCAL)
        {
            this.Mode = mode;
        }


        private Dictionary<string, Func<ICanJson>> Rebuilders = new();

        /// <summary>
        /// Registers a builder for the given unique ID, which is provided by <see cref="ICanJson.GetTargetIdentifier(SerializationEngine)"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="builder"></param>
        public void RegisterBuilder(string ID, Func<ICanJson> builder)
        {
            Rebuilders.TryAdd(ID, builder);
        }

        /// <summary>
        /// Converts the given componded Dictionary into a json string.
        /// 
        /// <para>Warn: This method does not validate dictionary structure. Preference <see cref="CompoundToString(ICanJson)"/>
        /// unless dictionary structure is otherwise assured.</para>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string CompoundToString(Dictionary<string, object> data)
        {
            return JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Converts the given ICanJson object into a json string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public  string CompoundToString(ICanJson data)
        {
            return CompoundToString(GetCompounded(data));
        }
        
        


        /// <summary>
        ///  Rebuilds the given json dictionary object into an ICanJson instance.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object? Rebuild(Dictionary<string, object> data)
        {
            // let's seek depth first into the tree
            foreach(var key in data.Keys.ToArray())
            {
                var value = data[key];
                if(value is Dictionary<string, object> child)
                {
                    // Recursively reconstruct ICanJsons
                    var instance = Rebuild(child);
                    if(instance != null) data[key] = instance;
                }
                else if(value is IList<Dictionary<string, object>> list)
                {
                    List<ICanJson?> datas = new();
                    for(int i = 0; i < list.Count; ++i)
                    {
                        var input = Rebuild(list[i]);
                        if (input != null)
                        {
                            datas.Add((ICanJson)input);
                        }
                    }
                    data[key] = datas;
                }
            }

            // We have now reached the bottom of this given branch
            // So let's try to remake an object
            if(data.TryGetValue(ID, out var val))
            {
                if(Rebuilders.TryGetValue(val.ToString(), out var builder))
                {
                    // make a new one, and populate it
                    var newContainer = builder();
                    newContainer.FromCompound(this, data);
                    return newContainer;
                }
            }
            
            // We could not build this dictionary into an object
            // So return null
            return data;

        }

        static Dictionary<string, object?> JsonToDictionary(JsonNode? jsonNode)
    {
        var result = new Dictionary<string, object?>();

        if (jsonNode is JsonObject jsonObject)
        {
            foreach (var kvp in jsonObject)
            {
                result[kvp.Key] = kvp.Value switch
                {
                    JsonObject obj => JsonToDictionary(obj),
                    JsonArray arr => arr.Select(JsonToDictionary).ToList(),
                    JsonValue val => val.GetValue<object>(),
                    _ => null
                };
            }
        }

        return result;
    }


        /// <summary>
        ///  Rebuilds the given json text into an ICanJson instance.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public object? Rebuild(string json)
        {
            var deserializedDictionary = JsonToDictionary(JsonNode.Parse(json));
            if (deserializedDictionary == null) return null;
            return Rebuild(deserializedDictionary!);

        }


        /// <summary>
        /// Compounds the given Json interface into a json-ifiable dictionary
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetCompounded(ICanJson json)
        {
            var dict = json.ToCompoundableDictionary(this);
            dict.TryAdd(ID, json.GetTargetIdentifier(this));
            Compound(dict);
            //now we have to go through and compound any sub node that needs compounding
            return dict;
        }

        /// <summary>
        /// Compounds the given dictionary. Note: recursion into <see cref="GetCompounded(ICanJson)"/>
        /// </summary>
        /// <param name="dictionary"></param>
        internal void Compound(Dictionary<string, object> dictionary)
        {
            foreach(var k in dictionary.Keys.ToArray())
            {
                var value = dictionary[k];
                if(value is ICanJson subtype)
                {
                    var container = GetCompounded(subtype);
                    dictionary[k] = container;
                }
                else
                {
                    var t = value.GetType();
                    if(t.IsGenericType)
                    {
                        var _t = t.GetGenericTypeDefinition();
                        if (_t.IsAssignableTo(typeof(List<>)))
                        {
                            var elementType = t.GetGenericArguments()[0]; // Element type

                            if (elementType.IsAssignableTo(typeof(ICanJson)))
                            {
                                // Dynamically process the list
                                var items = (IEnumerable<object>)value;
                                var mapping = new List<Dictionary<string, object>>();

                                foreach (var item in items)
                                {
                                    mapping.Add(GetCompounded((ICanJson)item));
                                }
                                dictionary[k] = mapping;
                            }
                        }
                        if (_t.IsAssignableTo(typeof(Dictionary<,>)) || _t.IsAssignableTo(typeof(ConcurrentDictionary<,>)))
                        {
                            var genericArguments = t.GetGenericArguments();
                            var valueType = genericArguments[1]; // TValue type

                            if (valueType.IsAssignableTo(typeof(ICanJson)))
                            {
                                // Dynamically handle dictionary
                                var keys = (IEnumerable<object>)value.GetType().GetProperty("Keys")!.GetValue(value)!;
                                var values = (IEnumerable<object>)value.GetType().GetProperty("Values")!.GetValue(value)!;

                                var mapping = new Dictionary<object, Dictionary<string, object>>();

                                var enumeratorKeys = keys.GetEnumerator();
                                var enumeratorValues = values.GetEnumerator();

                                while (enumeratorKeys.MoveNext() && enumeratorValues.MoveNext())
                                {
                                    var subKey = enumeratorKeys.Current;
                                    var subValue = enumeratorValues.Current;

                                    if (subValue is ICanJson canJsonValue)
                                    {
                                        mapping[subKey] = GetCompounded(canJsonValue);
                                    }
                                }

                                dictionary[k] = mapping;
                            }
                        }
                    }
                }
            }
        }





    }
}
