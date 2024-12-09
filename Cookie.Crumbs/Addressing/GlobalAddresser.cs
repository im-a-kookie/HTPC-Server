namespace Cookie.Addressing
{
    internal static class GlobalAddresser
    {
        private static AddressProvider<long> GlobalAddresses = new AddressProvider<long>();


        public static Address<long> Get()
        {
            return GlobalAddresses.Get();
        }




    }
}
