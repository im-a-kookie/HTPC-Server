using System.Collections;
using System.Reflection;
using System.Text;

using static Cookie.Serializers.SerializationConstants;

namespace Cookie.Serializers.Bytewise
{
    public class Byter
    {

        public class SerialContext
        {
            public BinaryWriter? writer;
            public BinaryReader? reader;
            public Dictionary<Type, int> TypeLookup = new();
            public Dictionary<int, Type> CodeLookup = new();
            public Dictionary<int, ConstructorInfo> ConstructorLookup = new();

            /// <summary>
            /// Gets a code for an input type
            /// </summary>
            /// <param name="t"></param>
            /// <returns></returns>
            public int GetCode(Type t)
            {
                if (TypeLookup.TryGetValue(t, out var code)) return code;
                else
                {
                    TypeLookup.Add(t, TypeLookup.Count + 1);
                    CodeLookup.Add(TypeLookup.Count, t);
                    return TypeLookup.Count;
                }
            }

            public Type? GetType(int i)
            {
                return CodeLookup[i] ?? null;
            }


            public IDictable? ReadEmptyInstance()
            {
                int n = reader!.ReadInt32();
                if (ConstructorLookup.TryGetValue(n, out var c))
                {
                    return (IDictable?)c.Invoke(null);
                }
                return null;
            }
        }


        public static short Version = 1;

        public delegate void Writer(SerialContext writer, object obj);
        public delegate object? Reader(SerialContext reader, char code);

        public static Dictionary<Type, char> TypeToHeader = new();
        public static Dictionary<Type, (char code, Writer writer)> Writers = new();
        public static Writer? GenericWriter = null;
        public static Writer? DictableWriter = null;

        public static Reader?[] Readers;
        public static Type?[] Types;

        static Byter()
        {
            // enumerate the serialization constants
            var constants = typeof(SerializationConstants)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly) // `IsLiteral` identifies constants
                .Where(f => f.FieldType == typeof(char));

            // Calculate the size of the lookup arrays that we're going to need
            int min = int.MaxValue;
            int max = int.MinValue;
            List<int> headerChars = [];

            foreach (var constant in constants)
            {
                var x = (int)((char)constant.GetValue(null)!);
                headerChars.Add(x);
                min = int.Min(min, x);
                max = int.Max(max, x);
            }
            if (max <= min)
                throw new ArgumentOutOfRangeException(
                    "Catastrophic WTF in serializer construction. Min/Max params broken.");

            Types = new Type?[max];
            Readers = new Reader?[max];

            DeclareTypes(max);
            DeclareReaders(max);




            // do the same for types
            DeclareWriters(max);


        }

        /// <summary>
        ///  Declares the actual types for the Byter to use for type conversions
        /// </summary>
        internal static void DeclareTypes(int len)
        {

            // Transpose into correct length array
            Types = new Type?[len];
            Types[Hint] = typeof(int);
            Types[Hlong] = typeof(long);
            Types[Hfloat] = typeof(float);
            Types[Hstring] = typeof(string);
            Types[Hbyte] = typeof(byte[]);
            Types[Hdict] = typeof(IDictionary);
            //Types[Hmap] = typeof(Dictionary<string, object>);
            Types[Hlist] = typeof(IList);
            Types[Hobj] = typeof(object);
            // and backwards, map types to characters
            for (int i = 0; i < Types.Length; ++i)
            {
                if (Types[i] != null)
                    TypeToHeader.TryAdd(Types[i]!, (char)i);
            }

            TypeToHeader.TryAdd(typeof(double), Hfloat);

        }

        public static void DeclareWriters(int len)
        {

            // string writer
            Writers[typeof(string)] = (Hstring, (context, data) =>
            {
                context.writer!.Write(Hstring);
                context.writer!.Write((string)data);
            }
            );

            // int writer
            Writers[typeof(int)] = (Hint, (context, data) =>
            {
                context.writer!.Write(Hint);
                context.writer!.Write((int)data);
            }
            );

            // int writer
            Writers[typeof(long)] = (Hlong, (context, data) =>
            {
                context.writer!.Write(Hlong);
                context.writer!.Write((long)data);
            }
            );

            // float writer
            Writers[typeof(float)] = (Hfloat, (context, data) =>
            {
                context.writer!.Write(Hfloat);
                context.writer!.Write((double)data);
            }
            );

            // double writer
            Writers[typeof(double)] = (Hfloat, (context, data) =>
            {
                context.writer!.Write(Hfloat);
                context.writer!.Write((double)data);
            }
            );

            // and byte arrays
            Writers[typeof(byte[])] = (Hbyte, (context, data) =>
            {
                context.writer!.Write(Hbyte);
                context.writer!.Write(((byte[])data).Length);
                context.writer!.Write((byte[])data);
            }
            );

            // generic dictable writer
            // (unlikely to land on initial lookup, but we can getter it later)
            Writers[typeof(IDictable)] = (Hcoded, DictableWriter = (context, data) =>
            {
                context.writer!.Write(Hcoded);
                context.writer!.Write(context.GetCode(data.GetType()));
                ToStream(context, ((IDictable)data).MakeDictionary());
                return;
            }
            );

            // Handle some of the more likely-to-be-used collections

            Writers[typeof(Dictionary<string, string>)] = (Hdict, (context, data) =>
            {
                ToStream(context, (Dictionary<string, string>)data);
            }
            );

            Writers[typeof(List<string>)] = (Hlist, (context, data) =>
            {
                ToStream(context, (List<string>)data);
            }
            );


            Writers[typeof(string[])] = (Hlist, (context, data) =>
            {
                ToStream(context, ((string[])data).ToList());
            }
            );


            // Also register a generic writer
            Writers[typeof(object)] = (Hobj, GenericWriter = TryToStreamGeneric);

        }


        /// <summary>
        /// Sets up the readers for reading back into objects
        /// </summary>
        public static void DeclareReaders(int max)
        {
            Readers = new Reader?[max];

            Readers[Hint] = (context, code) => context.reader!.ReadInt32();
            Readers[Hlong] = (context, code) => context.reader!.ReadInt64();

            Readers[Hstring] = (context, code) => context.reader!.ReadString();
            Readers[Hfloat] = (context, code) => context.reader!.ReadDouble();

            Readers[Hbyte] = (context, code) =>
            {
                int len = context.reader!.ReadInt32();
                return context.reader.ReadBytes(len);
            };

            // coded strings are a bit ugh
            Readers[Hcoded] = (context, code) =>
            {
                IDictable? target = context.ReadEmptyInstance();
                if (target != null)
                {
                    //now journey into the mapping
                    var c = context.reader!.ReadChar();
                    if (c == Hdict)
                    {
                        var keyType = context.reader!.ReadChar();
                        var valType = context.reader!.ReadChar();
                        int count = context.reader!.ReadInt32();
                        if (keyType == Hstring)
                        {
                            var dict = ReadDict<string, object>(context, count, keyType, valType);
                            if (dict != null)
                                target.FromDictionary(dict!);
                        }

                    }
                }
                return target;
            };

            MethodInfo? mList = typeof(Byter).GetMethod("ReadList", BindingFlags.Static | BindingFlags.NonPublic)!;
            Readers[Hlist] = (context, code) =>
            {
                // get the type of the data
                var valCode = context.reader!.ReadChar();
                Type? valType = Types[valCode];

                if (valCode == Hcoded)
                {
                    int type = context.reader!.ReadInt32();
                    valType = context.GetType(type);
                    valCode = Hobj;
                }

                var length = context.reader.ReadInt32();
                if (valType != null)
                {
                    var tm = mList.MakeGenericMethod(valType);
                    // TODO cache dynamic delegate from emitter to improve invocation performance
                    return tm.Invoke(null, [context, length, valCode]);
                }
                return null;
            };

            MethodInfo? mDict = typeof(Byter).GetMethod("ReadDict", BindingFlags.Static | BindingFlags.NonPublic)!;
            Readers[Hdict] = (context, code) =>
            {
                // get the type of the data
                var keyCode = context.reader!.ReadChar();
                var valCode = context.reader!.ReadChar();
                Type? valType = Types[valCode];
                if (valCode == Hcoded)
                {
                    int type = context.reader!.ReadInt32();
                    valType = context.GetType(type);
                    valCode = Hobj;
                }

                var length = context.reader.ReadInt32();
                Type? keyType = Types[keyCode];
                if (keyType != null && valType != null)
                {
                    var tm = mDict.MakeGenericMethod(keyType!, valType!);
                    var dict = tm.Invoke(null, [context, length, keyCode, valCode]);
                    return dict;
                }
                return null;
            };


            Readers[Hobj] = (context, code) =>
            {
                // prevent infinite loop
                if (code == Hobj) return null;
                // now get the actual reader for the type here
                var _innerReader = Readers[code];
                if (_innerReader == null) return null;
                return _innerReader(context, code);
            };

        }


        /// <summary>
        /// Generates a list of objects of the given type, given the cout and char identifier. Assumes
        /// writer is positioned at the first index of the list
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="context"></param>
        /// <param name="count"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        internal static IDictionary<K, V?>? ReadDict<K, V>(SerialContext context, int count, char key, char val) where K : notnull
        {
            Reader? keyReader = Readers[key];
            Reader? valReader = Readers[val];

            if (valReader == null) return null;
            Dictionary<K, V?>? results = new((count * 3) / 2);
            for (int i = 0; i < count; i++)
            {
                var k = context.reader!.ReadChar();
                var KeyData = keyReader(context, k);
                var v = context.reader!.ReadChar();
                var ValueData = valReader(context, v);
                results.TryAdd((K)KeyData!, (V?)ValueData);
            }
            return results;
        }

        /// <summary>
        /// Generates a list of objects of the given type, given the cout and char identifier. Assumes
        /// writer is positioned at the first index of the list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="count"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        internal static IList<T?>? ReadList<T>(SerialContext context, int count, char code)
        {
            Reader? valueReader = Readers[code];
            if (valueReader == null) return null;
            List<T?>? results = new(count);
            for (int i = 0; i < count; i++)
            {
                var valCode = context.reader!.ReadChar();
                var obj = valueReader(context, valCode)!;
                results.Add((T?)(obj ?? null));
            }
            return results;
        }

        public static IDictionary<string, object?>? FromBytes(Stream input)
        {
            SerialContext context = new SerialContext();
            using BinaryReader reader = new(input);
            context.reader = reader;
            // now read a thing
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int index = reader.ReadInt32();
                string t = reader.ReadString();

                var type = Type.GetType(t);
                if (type == null) throw new Exception("Cannot decode type header: " + t);
                if (!type.IsAssignableTo(typeof(IDictable))) throw new Exception("Cannot decode non-dictable type " + t);

                var c = type.GetConstructor([]);
                if (c == null) throw new Exception("Type must have empty constructor! " + t);

                // load it
                context.TypeLookup.Add(type, index);
                context.CodeLookup.Add(index, type);
                context.ConstructorLookup.Add(index, c);

            }

            // now we can just read I guesssss
            var header = reader.ReadChar();
            if (header == Hdict)
            {
                var key = reader.ReadChar();
                var value = reader.ReadChar();
                int keys = reader.ReadInt32();
                return ReadDict<string, object>(context, keys, Hstring, Hobj);
            }
            return null;
        }

        public static void ToBytes(Stream output, IDictionary<string, object> data)
        {
            using MemoryStream firstWrite = new();
            using BinaryWriter bw = new(firstWrite);
            SerialContext context = new();
            context.writer = bw;

            ToStream(context, data);

            //process the context for rebuilding
            using BinaryWriter contextWriter = new(output, Encoding.Default, leaveOpen: true);
            contextWriter.Write(context.TypeLookup.Count);
            foreach (var kv in context.CodeLookup)
            {
                contextWriter.Write(kv.Key);
                contextWriter.Write(kv.Value.AssemblyQualifiedName!);
            }

            firstWrite.Seek(0, SeekOrigin.Begin);
            firstWrite.CopyTo(output);
            firstWrite.Dispose();
            context.writer.Dispose();

        }

        public static void ToStream(SerialContext context, object data)
        {
            if (Writers.TryGetValue(data.GetType(), out var header))
            {
                header.writer(context, data);
            }
            else TryToStreamGeneric(context, data, false);
        }

        public static void ToStream(SerialContext context, IDictionary<string, string> data)
        {
            context.writer!.Write(Hdict);
            context.writer.Write(Hstring);
            context.writer.Write(Hstring);
            context.writer.Write(data.Count);
            foreach (var item in data)
            {
                context.writer.Write(Hstring);
                context.writer.Write(item.Key);
                context.writer.Write(Hstring);
                context.writer.Write(item.Value);
            }
        }

        public static void ToStream(SerialContext context, IList<string> data)
        {
            context.writer!.Write(Hlist);
            context.writer.Write(Hstring);
            context.writer.Write(data.Count);
            foreach (var s in data)
            {
                context.writer.Write(Hstring);
                context.writer.Write(s);
            }
        }


        public static void TryToStreamGeneric(SerialContext context, object data)
        {
            TryToStreamGeneric(context, data, true);
        }

        /// <summary>
        /// Tries to stream the given object into the provided context, where the streaming method
        /// selects the best approach for the object provided.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        public static void TryToStreamGeneric(SerialContext context, object data, bool check = true)
        {
            var t = data.GetType();

            if (check && Writers.TryGetValue(t, out var writer))
            {
                writer.writer(context, data);
                return;
            }

            if (t.IsAssignableTo(typeof(IDictable)))
            {
                context.writer!.Write(Hcoded);
                context.writer!.Write(context.GetCode(t));
                ToStream(context, ((IDictable)data).MakeDictionary());
                return;
            }

            if (!t.IsGenericType) return;
            var gt = t.GetGenericTypeDefinition();

            // get the generic dictionary enumerable
            if (gt.IsAssignableTo(typeof(IDictionary)))
            {
                if (__WriteDictionaryGeneric(context, t, data))
                    return;
            }

            if (data is IEnumerable)
            {
                if (_WriteEnumerableGeneric(context, t, data))
                    return;
            }

            throw new InvalidDataException($"Cannot serialize data of type {t}");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static bool _WriteEnumerableGeneric(SerialContext context, Type t, object data)
        {
            var t1 = t.GetGenericArguments()[0];
            if (Writers.TryGetValue(t1, out var ValWriter)
                || (t1.IsAssignableTo(typeof(IDictable))
                     && Writers.TryGetValue(typeof(IDictable), out ValWriter)))
            {
                try
                {
                    var list = Enumerable.Cast<object>((IEnumerable)data);
                    context.writer!.Write(Hlist);
                    context.writer!.Write(ValWriter.code);
                    if (ValWriter.code == Hcoded)
                    {
                        context.writer!.Write(context.GetCode(t1));
                    }
                    context.writer!.Write(list.Count());
                    foreach (var val in list)
                    {
                        ValWriter.writer(context, val);
                    }
                    return true;
                }
                catch (InvalidCastException)
                {
                    SerializationWarnings.BadType.Warn($"Enable to cast values from expected <{t}>");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Reads an IDictionary in data with the type given by T, and provides it directly into
        /// the given serialization context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t">The type of the data, which must be assignable to IDictionary.</param>
        /// <param name="data"></param>
        internal static bool __WriteDictionaryGeneric(SerialContext context, Type t, object data)
        {
            // Ensure the key and value are writable
            var t1 = t.GetGenericArguments()[0];
            var t2 = t.GetGenericArguments()[1];

            if (Writers.TryGetValue(t1, out var KeyWriter))
            {
                if (Writers.TryGetValue(t2, out var ValWriter)
                    || (t2.IsAssignableTo(typeof(IDictable))
                        && Writers.TryGetValue(typeof(IDictable), out ValWriter)))
                {
                    // Sadly, we need reflection
                    var _k = (t.GetProperty("Keys")!.GetValue(data) as IEnumerable);
                    var _v = (t.GetProperty("Values")!.GetValue(data) as IEnumerable);

                    if (_k != null && _v != null)
                    {
                        try
                        {
                            // Inner cast
                            var keys = _k.Cast<object>().ToArray();
                            var vals = _v.Cast<object>().ToArray();

                            // Now write it all
                            context.writer!.Write(Hdict);
                            context.writer!.Write(KeyWriter.code);
                            context.writer!.Write(ValWriter.code);
                            if (ValWriter.code == Hcoded)
                                context.writer!.Write(context.GetCode(t2));

                            context.writer!.Write(keys.Length);
                            for (int i = 0; i < keys.Length; i++)
                            {
                                KeyWriter.writer(context, keys[i]);
                                ValWriter.writer(context, vals[i]);
                            }
                            return true;
                        }
                        catch (InvalidCastException)
                        {
                            SerializationWarnings.BadType.Warn($"Enable to cast values from expected <{t}>");
                            return false;
                        }

                    }
                }
            }
            return false;
        }




    }

}
