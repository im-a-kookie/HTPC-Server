using CookieCrumbs.Serializing;

namespace CookieCrumbs.ContentLibrary
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
