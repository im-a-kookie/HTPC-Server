using Backend.ServerLibrary;
using Cookie.Connections;
using Cookie.Connections.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.API
{
    [Route("files", "The main path for accessing files from the server")]
    public class Files
    {

        LibraryProvider Provider;

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
