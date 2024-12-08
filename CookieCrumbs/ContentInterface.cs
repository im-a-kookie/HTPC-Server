using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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

        public static ContentInterface CreateLocalInterface(int ListenPort = 61994)
        {
            var host = new ContentInterface(HostMode.LOCAL);
            host.Port = ListenPort;

            return host;
        }

        public static ContentInterface CreateRemoteInterface(string HostIp = "localhost", int Port = 61994)
        {
            var host = new ContentInterface(HostMode.REMOTE);
            host.Port = Port;

            return host;
        }


        public HostMode Mode { get; private set; }

        public int Port { get; private set; }

        /// <summary>
        /// Creates a new content interface in the given host mode. Remote mode
        /// indicates that this interface will be provided with a link
        /// to a backend interface, and Local mode indicates that this
        /// interface will manage its own access to a filesystem.
        /// </summary>
        /// <param name="mode"></param>
        public ContentInterface(HostMode mode)
        {

        }











        



    }

}
