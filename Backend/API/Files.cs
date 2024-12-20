using Backend.ServerLibrary;
using Cookie.Connections.API;

namespace Backend.API
{
    [Route("files", "The main path for accessing files from the server")]
    public class Files
    {
        private LibraryProvider Provider;

        public Files(LibraryProvider provider)
        {
            this.Provider = provider;
        }



        [Route("video", "The main endpoint for retrieving a video file")]
        public string? GetVideo(string query)
        {

            var path = query;

            return "";
        }

    }
}
