using Cookie.Connections.API;
using Cookie.ContentLibrary;
namespace Backend.ServerLibrary
{
    internal class LibraryProvider
    {

        public FileProvider Provider;

        public Library ProvidedLibrary;

        public LibraryProvider(Library library)
        {
            ProvidedLibrary = library;

            Provider = new();
            Provider.PathTransformer = (x) =>
            {
                if (ProvidedLibrary.targetToFileMap.TryGetValue(x, out var episode))
                {
                    return episode.DecompressPath(ProvidedLibrary);
                }
                return null;
            };

        }




    }
}
