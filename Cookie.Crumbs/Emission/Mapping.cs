#if !BROWSER
namespace Cookie.Emission
{
    /// <summary>
    /// Maps parameters from entry->target for delegate construction
    /// </summary>
    internal struct Mapping
    {
        internal readonly int src;
        internal readonly int dst;
        internal Mapping(int source, int destination)
        {
            this.src = source; this.dst = destination;
        }
    }
}
#endif