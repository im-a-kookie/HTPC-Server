using Cookie.Serializers;

namespace Cookie.ContentLibrary
{
    public class Season : IDictable
    {

        public List<MediaFile> Eps { get; set; } = new List<MediaFile>();

        public void FromDictionary(IDictionary<string, object> dict)
        {
            Eps = (List<MediaFile>)dict["E"];
        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["E"] = Eps;

        }
    }
}
