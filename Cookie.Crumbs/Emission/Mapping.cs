namespace Cookie.Emission
{
    /// <summary>
    /// Maps parameters from entry->target for delegate construction
    /// </summary>
    public struct Mapping
    {
        public readonly int src;
        public readonly int dst;
        public Mapping(int source, int destination)
        {
            this.src = source; this.dst = destination;
        }
    }
}
