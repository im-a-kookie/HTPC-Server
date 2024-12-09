namespace Cookie.Addressing
{
    public abstract class Addressable
    {
        public Address<long> Address;

        /// <summary>
        /// Creates the new addressable
        /// </summary>
        public Addressable()
        {
            Address = GlobalAddresser.Get();
        }

        public abstract void Exit();

    }
}
