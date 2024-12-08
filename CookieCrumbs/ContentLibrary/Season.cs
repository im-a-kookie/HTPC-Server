using CookieCrumbs.ContentLibrary;
using CookieCrumbs.Serializing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
