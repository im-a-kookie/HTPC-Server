namespace Cookie.Addressing
{
    /// <summary>
    /// Defines an interface for providing unique addresses based on a given struct type <typeparamref name="T"/>.
    /// The generated addresses are ASCII/UTF7-readable strings with a 1:1 mapping between bytes and characters.
    /// </summary>
    /// <typeparam name="T">
    /// The underlying struct type that backs the address. This type must be a value type (`struct`).
    /// </typeparam>
    /// <remarks>
    /// Unspecified behaviour may occur if the <typeparamref name="T"/> is not Short, Int, or Long
    /// </remarks>
    public interface IAddressProvider<T> where T : struct
    {
        /// <summary>
        /// Generates and retrieves a unique address for the current provider.
        /// Each call to this method is expected to produce a new, unique address.
        /// </summary>
        /// <returns>An <see cref="Address{T}"/> instance representing a unique address.</returns>
        Address<T> Get();

        /// <summary>
        /// Generates and retrieves a unique address for the current provider and outputs its index.
        /// Each call to this method is expected to produce a new, unique address.
        /// </summary>
        /// <param name="index">
        /// An output parameter that receives the unique index of the generated address. This index is expected
        /// to be unique, but provides no guarantees about sequentiality.
        /// </param>
        /// <returns>An <see cref="Address{T}"/> instance representing a unique address.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provider has reached capacity.</exception>
        Address<T> Get(out uint index);

        /// <summary>
        /// Creates an address from a specified value of type <typeparamref name="T"/>.
        /// This allows mapping an existing value to its corresponding address representation.
        /// </summary>
        /// <param name="value">
        /// The value of type <typeparamref name="T"/> to convert into an address.
        /// </param>
        /// <returns>An <see cref="Address{T}"/> instance representing the provided value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided value exceeds the bit-density of the provider</exception>
        Address<T> FromValue(T value);

        /// <summary>
        /// Returns the total number of addresses that this provider has allocated. This value is not
        /// necessarily threadsafe and is expected to be used for general diagnostic/inspection purposes
        /// only.
        /// </summary>
        /// <returns></returns>
        int GetTotalAllocated();

    }
}
