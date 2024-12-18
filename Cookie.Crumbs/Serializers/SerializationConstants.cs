namespace Cookie.Serializers
{
    internal class SerializationConstants
    {

        public const char Terminator = ';';

        public const char AltTerminator = '<';

        public const char PropDelim = '*';

        public const char KeyValDelim = '~';

        public const char OpenGroup = '{';

        public const char CloseGroup = '}';

        public const char Tab = '\t';

        /// <summary>
        /// Integer header
        /// </summary>
        public const char Hint = 'i';
        /// <summary>
        /// Float header
        /// </summary>
        public const char Hfloat = 'f';
        /// <summary>
        /// Double header (matches float)
        /// </summary>
        public const char Hdouble = 'f';
        /// <summary>
        /// String header
        /// </summary>
        public const char Hstring = 's';
        /// <summary>
        /// List header
        /// </summary>
        public const char Hlist = 'l';
        /// <summary>
        /// Mapping header
        /// </summary>
        public const char Hmap = 'm';
        /// <summary>
        /// Mapping header
        /// </summary>
        public const char Hdict = 'd';
        /// <summary>
        /// Byte array header
        /// </summary>
        public const char Hbyte = 'b';
        /// <summary>
        /// Encoded (base128<->string) header
        /// </summary>
        public const char Hcoded = 'c';

        /// <summary>
        /// Encoded (base128<->string) header
        /// </summary>
        public const char Hobj = 'o';

    }
}
