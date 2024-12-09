using System.Buffers;
using System.Runtime.InteropServices;

namespace Cookie.Addressing
{

    public class AddressProvider<T> : IAddressProvider<T> where T : struct
    {
        /// <summary>
        /// Internal counter representing the total number of instances allocated in this lifetime
        /// </summary>
        private int _lifetimeAllocation = 0;

        public bool Randomized { get; private set; } = false;

        public byte[] XorMask;

        public AddressProvider(bool randomized = true)
        {

            XorMask = new byte[Marshal.SizeOf(typeof(T))];

            if (randomized)
            {
                this.Randomized = true;
                Random.Shared.NextBytes(XorMask);
            }

        }

        public Address<T> FromValue(T value)
        {
            return new(Address<T>.HashToBits(value));
        }

        public void Randomize(byte[] data)
        {
            if (Randomized)
            {
                var mask = Address<T>.GetMaskBits_Pooled();
                for (int i = 0; i < XorMask.Length; i++)
                {
                    data[i] ^= (byte)(XorMask[i] & mask[i]);
                }
                ArrayPool<byte>.Shared.Return(mask);
            }
        }

        public Address<T> Get()
        {
            //increment the counter
            uint counter = (uint)Interlocked.Increment(ref _lifetimeAllocation);

            //get a correctly lengthed value
            byte[] data = new byte[Marshal.SizeOf<T>()];
            if (!BitConverter.TryWriteBytes(data, counter))
            {
                BitConverter.TryWriteBytes(data, (ushort)counter);
            }

            Randomize(data);



            //and return the hashed type
            return new(Address<T>.HashToBits(Address<T>.FromByteArray(data)));
        }

        public Address<T> Get(out uint index)
        {
            //increment the counter
            uint counter = (uint)Interlocked.Increment(ref _lifetimeAllocation);

            //get a correctly lengthed value
            byte[] data = new byte[Marshal.SizeOf<T>()];
            if (!BitConverter.TryWriteBytes(data, counter))
            {
                BitConverter.TryWriteBytes(data, (ushort)counter);
            }


            Randomize(data);

            index = counter;

            //and return the hashed type
            return new(Address<T>.HashToBits(Address<T>.FromByteArray(data)));
        }

        public int GetTotalAllocated()
        {
            return _lifetimeAllocation;
        }
    }


}
