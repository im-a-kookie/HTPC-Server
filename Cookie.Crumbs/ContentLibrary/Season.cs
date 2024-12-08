using Cookie.Serializing;

namespace Cookie.ContentLibrary
{
    public class Season : ICanJson
    {

        public List<MediaFile> Episodes { get; set; } = new();

        public string GetTargetIdentifier(SerializationEngine engine)
        {
            return "Season";
        }
    }
}
