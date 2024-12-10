namespace Cookie.Serializers
{
    public interface IDictable
    {

        /// <summary>
        /// Writes this object into the given dictionary
        /// </summary>
        /// <param name="dict"></param>
        public void ToDictionary(IDictionary<string, object> dict);
        
        /// <summary>
        /// Reads this object from the given dictionary. It is expected to produce
        /// an identif
        /// </summary>
        /// <param name="dict"></param>
        public void FromDictionary(IDictionary<string, object> dict);

        /// <summary>
        ///  Default returns a dictionary built from this object
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>();
            ToDictionary(d);
            return d;
        }


    }
}
