using Cookie.Crumbs.Utils;
using Cookie.Utils;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using static Cookie.Utils.Error;

namespace Cookie.Addressing
{
    /// <summary>
    /// An address represented by the given struct <typeparamref name="T"/> where T dictates
    /// the binary backing data for the address.
    /// 
    /// <para>The address is encoded 1:1 into an ASCII readable mapping matching sizeof(T) using the specified encoder.</para> 
    /// <para>It is recommended not to use single-byte primitives in this instance.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Address<T> where T : struct
    {

        internal static class Errors<AddressType>
        {
            private static ExceptionBuilder AooRE = (m, e) => new ArgumentOutOfRangeException(m, e);

            internal static Error UnmatchedArrays = new(new("Invalid Data Length", $"Generated array does not match sizof({typeof(AddressType)})"), AooRE);

            internal static Error NoMoreAddresses = new(new("Out of Range Address", "The input value exceeds the allowed address length"), AooRE);

            internal static Error BitshiftTooLong = new(new("Bitshift Error", "The internal bitshift was too long."), AooRE);

        }

        /// <summary>
        /// The encoder used to provide this address
        /// </summary>
        private static IAddressEncoder Encoder = new HexEncoder<T>();

        /// <summary>
        /// Bits of information per character of the encoder
        /// </summary>
        public static readonly int BitsPerChar = Encoder.GetBitsPerChar();

        /// <summary>
        /// The bit density of the addressor with type <typeparamref name="T"/>
        /// </summary>
        public static readonly int BitDensity;

        /// <summary>
        /// The number of bytes here
        /// </summary>
        private static int _byteSize;

        /// <summary>
        /// The number of bytes allocated to type <typeparamref name="T"/>
        /// </summary>
        public static int ByteSize => _byteSize;

        /// <summary>
        /// An internal array denoting index reordering
        /// </summary>
        private static byte[] Shuffler;

        /// <summary>
        /// An internal array denoting xor scrambling
        /// </summary>
        private static byte[] Scrambler;

        /// <summary>
        /// An internal array denoting xor scrambling
        /// </summary>
        private static byte[] Bonkler;

        public static Address<T> Zero = new(default);

        /// <summary>
        /// Set up static information for address hashing etc later
        /// </summary>
        static Address()
        {
            // Set the byte size for the type T
            _byteSize = Marshal.SizeOf<T>();
            BitDensity = _byteSize * BitsPerChar;
            Random r = new Random(_byteSize * 5623);

            // Create a seed to shuffle the data bytes
            ulong seed = (ulong)r.NextInt64();
            int realLen = (BitDensity + 7) / 8;  // Calculate the real length (rounded up to byte size)

            // Initialize shuffler and scrambler arrays
            Shuffler = new byte[realLen];
            Scrambler = new byte[realLen];
            Bonkler = new byte[realLen];

            // Initialize Shuffler array with indices
            for (int i = 0; i < Shuffler.Length; i++)
            {
                Shuffler[i] = (byte)i;
                Bonkler[i] = (byte)i;
            }

            // Generate a mask and apply it to the Scrambler array
            var mask = GetMaskBits_Pooled();
            for (int i = 0; i < Scrambler.Length; i++)
            {
                // Update the seed and scramble the data
                seed = (ulong)r.NextInt64();
                Scrambler[i] = (byte)(seed >> i & mask[i]);
            }

            // Return the mask to the array pool
            ArrayPool<byte>.Shared.Return(mask);

            // Determine the upper shuffle bound (excluding partial byte if necessary)
            int upper = realLen;
            if (realLen * 8 != BitDensity)
            {
                upper -= 1;
            }

            if (upper > 1)
            {
                // Reorder the shuffler array, excluding the topmost partial byte
                for (int i = 0; i < upper; ++i)
                {
                    // Generate new seed and shuffle
                    int b = i;
                    while (Shuffler[b] == Shuffler[i])
                    {
                        b = (int)(seed % (ulong)upper);
                        seed = (ulong)r.NextInt64();
                    }
                    (Shuffler[i], Shuffler[b]) = (Shuffler[b], Shuffler[i]);
                }
            }
        }


        /// <summary>
        /// Gets the size in bytes of this address value
        /// </summary>
        public int Size => Marshal.SizeOf<T>();

        /// <summary>
        /// the internal value
        /// </summary>
        private T _value;

        /// <summary>
        /// Gets the internal value of this address, fully packed.
        /// </summary>
        public T Value => _value;

        /// <summary>
        /// Gets this address as a string value
        /// </summary>
        public string Text => Encoding.ASCII.GetString(ToByteArraySafe(_value));

        /// <summary>
        /// Gets this address as a byte array whose size will match <see cref="Size"/>
        /// </summary>
        public byte[] Data => ToByteArraySafe(_value);

        /// <summary>
        /// Creates a new address with the given value of <typeparamref name="T"/>. Unless <paramref name="value"/> is
        /// being retrieved from a cache, address should be constructed with <see cref="Address{T}.Get"/>
        /// </summary>
        /// <param name="value"></param>
        public Address(T value)
        {
            if (Size < 2) Logger.Warn(Messages.InsufficientAddressLength, $"{Size} bytes (recommend: {2}+)");
            _value = value;
        }

        /// <summary>
        /// Hashes a given long value to a set of input bits. 
        /// This method guarantees that all valuues from 0 to 2^bits-1 are represented by a unique value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bits"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value of T exceeds the maximum bit density</exception>
        public static T HashToBits(T value)
        {
            // Get the mask and data byte arrays
            var mask = GetMaskBits_Pooled();
            var data = ToByteArray_Pooled(value);

            try
            {

                if (mask.Length < _byteSize || data.Length < _byteSize)
                    throw Errors<T>.UnmatchedArrays.Get($"Data({data.Length}), Mask({mask.Length}), expects {_byteSize}");

                int accumulator = 0;
                // Apply the mask to the data
                for (int i = 0; i < _byteSize; ++i)
                {
                    byte d = data[i];
                    data[i] &= mask[i];  // Apply the mask bitwise
                    accumulator += data[i] * (i + 1);
                    // Check if data is within the valid range
                    if (d != data[i])
                    {
                        throw Errors<T>.NoMoreAddresses.Get($"Max argument is 2^bits-1 ({(1 << BitDensity) - 1})");
                    }
                }

                //cut the last index if the last byte is partial
                int modulus = Scrambler.Length;
                if (Scrambler.Length * 8 != _byteSize * Encoder.GetBitsPerChar())
                {
                    modulus -= 1;
                }

                // Jumble up the data using the Scrambler
                for (int i = 0; i < Scrambler.Length; ++i)
                {
                    int j = Shuffler[i];

                    int index = i;

                    if (Scrambler.Length * 8 == _byteSize * Encoder.GetBitsPerChar() || i < Scrambler.Length - 1)
                    {
                        mask[i] = (byte)(data[j] ^ Scrambler[(i + accumulator) % modulus]);
                    }
                    else
                    {
                        mask[i] = (byte)(data[j] ^ Scrambler[i]);
                    }
                }

                //doink
                return FromByteArray(Encoder.Compound(mask));

            }
            finally
            {
                // Return memory pools
                ArrayPool<byte>.Shared.Return(mask);
                ArrayPool<byte>.Shared.Return(data);
            }
        }

        /// <summary>
        /// Gets the mask bits for the given number of bits, as a byte array
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static byte[] GetMaskBits_Pooled()
        {
            int size = ByteSize;
            int bits = BitDensity;

            if (bits > Marshal.SizeOf<T>() * 8)
                throw Errors<T>.BitshiftTooLong.Get($"{bits} given, {Marshal.SizeOf<T>() * 8} max");

            byte[] result = ArrayPool<byte>.Shared.Rent(_byteSize);

            int realLen = 1 + bits / 8;
            for (int i = 0; i < realLen; ++i)
            {
                // Get the offset in the position
                int offset = 8 * i;
                int remain = bits - offset;
                if (remain >= 8)
                {
                    result[i] = byte.MaxValue;
                }
                else
                {
                    result[i] = (byte)((1 << remain) - 1);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts the given object to a byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToByteArray_Pooled(T value)
        {
            // Calculate and allocate the array
            int size = Marshal.SizeOf<T>();
            byte[] bytes = ArrayPool<byte>.Shared.Rent(_byteSize);
            // And write it directly via memory marashal
            MemoryMarshal.Write(bytes.AsSpan(), in value);
            return bytes;
        }

        /// <summary>
        /// Converts the given object to a byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToByteArraySafe(T value)
        {
            // Calculate and allocate the array
            int size = Marshal.SizeOf<T>();
            byte[] bytes = new byte[size];
            // And write it directly via memory marashal
            MemoryMarshal.Write(bytes.AsSpan(), in value);
            return bytes;
        }

        /// <summary>
        /// Reads a byte array into the given value type
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static T FromByteArray(byte[] data)
        {
            // Validate input
            if (data == null)
                throw GenericErrors.NullArgument.Get("Address.FromByteArray::" + nameof(data));
            //ensure length is appropriate
            if (data.Length < Marshal.SizeOf<T>())
                throw Errors<T>.UnmatchedArrays.Get($"Given: {data.Length} bytes, {typeof(T).Name}: {_byteSize} bytes");

            // MemoryMarshal allows reinterpretation of bytes as a struct.
            return MemoryMarshal.Read<T>(data.AsSpan().Slice(0, _byteSize));
        }

        public override string ToString()
        {
            return Text;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            return (obj is T t && t.Equals(_value));
        }

    }
}
