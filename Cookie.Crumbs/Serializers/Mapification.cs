using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;

namespace Cookie.Serializers
{
    internal class Mapification
    {



        private class MapPair
        {
            public Map mapper;
            public Unmap unmapper;
            public MapPair(Map mapper, Unmap unmapper)
            {
                this.mapper = mapper;
                this.unmapper = unmapper;
            }

            public object Get(IDictionary<string, object> input)
            {
                return unmapper(input);
            }

            public IDictionary<string, object> Get(object? input)
            {
                return mapper(input);
            }

        }

        public delegate IDictionary<string, object> Map(object? o);

        public delegate object? Unmap(IDictionary<string, object> data);

        private static IDictionary<Type, MapPair> registry = new ConcurrentDictionary<Type, MapPair>();

        private static ConcurrentDictionary<Type, ConstructorInfo> builders = new();

        /// <summary>
        /// Freezes this instance into a faster readonly dictionary
        /// </summary>
        public static void Freeze()
        {
            if (registry.IsReadOnly) return;
            registry = registry.ToFrozenDictionary();
        }


        public static void RegisterMapping(Type t, Map mapper, Unmap unmapper)
        {
            if (registry.IsReadOnly) return;

            registry.TryAdd(t, new(mapper, unmapper));
        }

        /// <summary>
        /// Attempts to create an object by the given type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static object? Construct(string typeName)
        {
            var t = Type.GetType(typeName);
            if (t != null) return Construct(t);
            return null;
        }

        /// <summary>
        /// Attempts to create an object by the given type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static object? Construct(Type t)
        {
            if (builders.TryGetValue(t, out var builder))
            {
                return builder.Invoke([]);
            }
            else if ((builder = t.GetConstructor([])) != null)
            {
                builders.TryAdd(t, builder);
                return builder.Invoke([]);
            }
            return null;
        }

        private static IDictable? TryFillGeneric(IDictionary<string, object> data)
        {
            if (data.TryGetValue("__type__", out var typeName))
            {
                object? result = null;
                if (typeName is string s)
                {
                    result = Construct(s);
                    if (result is IDictable id)
                    {
                        id.FromDictionary(data);
                        return id;
                    }
                }
            }
            return null;
        }

        static Mapification()
        {
            RegisterMapping(typeof(IDictable), (x) => ((IDictable)x!).MakeDictionary(), (x) => TryFillGeneric(x));
        }


        public static bool GetDefaultConstructor(Type t)
        {
            var constructor = t.GetConstructor([]);
            if (constructor == null) return false;
            builders.TryAdd(t, constructor);
            return true;
        }


        public static T? RebuildObject<T>(Dictionary<string, object> data)
        {
            return default(T);
        }


        public static Dictionary<string, object> MapObject(IDictable o, bool aggressive = false)
        {
            return new();


        }

        public static Dictionary<string, object> MapObject(object o, bool aggressive = false)
        {
            return new();




        }







    }


}
