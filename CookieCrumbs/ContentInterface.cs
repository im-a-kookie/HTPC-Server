using CookieCrumbs.TCP;
using System.Security.Cryptography.X509Certificates;

namespace CookieCrumbs
{
    /// <summary>
    /// Provides a local TCP/socket interface for connecting content interfaces together.
    /// 
    /// <para>
    /// In general, a content interface should be declared from the backend library, and a 
    /// content interface should be declared at the front-end. This interface also provides the streaming
    /// interface for file requests over TCP,
    /// </para>
    /// 
    /// <para>
    /// If running a single application, it is still recommended to host the local and remote instances
    /// within the same server process, configured to use the localhost interface.
    /// </para>
    /// </summary>
    public class ContentInterface
    {

        /// <summary>
        /// The mode of this content interface
        /// </summary>
        public HostMode Mode { get; private set; }

        /// <summary>
        /// The port for this content interface
        /// </summary>
        public int Port { get; private set; }

        public ConnectionProvider? BackendProvider { get; private set; }



        /// <summary>
        /// Creates a new content interface in the given host mode. Remote mode
        /// indicates that this interface will be provided with a link
        /// to a backend interface, and Local mode indicates that this
        /// interface will manage its own access to a filesystem.
        /// </summary>
        /// <param name="mode"></param>
        public ContentInterface(int port, HostMode mode, X509Certificate2? ssl = null)
        {

            // The local model provides a TCP server
            // While the front-end model will typically only request to this server
            if(mode == HostMode.LOCAL)
            {
                BackendProvider = new ConnectionProvider(port);
            }




        }















    }

}
