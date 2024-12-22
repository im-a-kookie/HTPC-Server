using Cookie.Connections.API;
using Cookie.ContentLibrary;
namespace Backend.ServerLibrary
{
    public class LibraryProvider
    {

        public FileProvider Provider;

        public Library ProvidedLibrary;

        public LibraryProvider(Library library)
        {
            ProvidedLibrary = library;

            Provider = new();
            Provider.PathTransformer = (x) =>
            {
                int n = -1;
                if (x.StartsWith("v_")) x = x.Substring(2);
                if (int.TryParse(x, out n))
                {
                    if (ProvidedLibrary.targetToFileMap.TryGetValue(n, out var episode))
                    {
                        return episode.DecompressPath(ProvidedLibrary);
                    }
                }
                return null;
            };

        }

        public string? GetPath(string key)
        {
            string? path = key;
            var result = Provider.ProvideFile(null, ref path);
            return path;
        }




    }
}
