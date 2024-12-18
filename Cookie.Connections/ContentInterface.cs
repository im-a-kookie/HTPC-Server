using Cookie.ContentLibrary;
using Cookie.Logging;
using Cookie.UDP;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#if !BROWSER
using Cookie.TCP;

namespace Cookie
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
        /// A local creation static to try and alleviate port conflict
        /// </summary>
        public static int LocalCreated = 0;

        /// <summary>
        /// The mode of this content interface
        /// </summary>
        public HostMode Mode { get; private set; }

        /// <summary>
        /// The port for this content interface
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// The client port for this interface
        /// </summary>
        public int ClientPort { get; private set; }

        /// <summary>
        /// A mapping of all known client ports
        /// </summary>
        private ConcurrentDictionary<int, bool> KnownPorts = new();

        /// <summary>
        /// Internal flag denoting whether connection is established with backend
        /// </summary>
        public bool _establishedConnection = false;

        /// <summary>
        /// The backend TCP provider that facilitates most of the connections for
        /// this interface object
        /// </summary>
        public ConnectionProvider? BackendProvider { get; private set; }

        public UDPChannel? MessageChannel { get; private set; }

        public Library LibraryModel { get; private set; }


        public ContentInterface(int backendPort, Library library, HostMode mode, X509Certificate2? ssl = null)
        {
            this.Port = backendPort;
            this.ClientPort = backendPort;
            this.LibraryModel = library;

            // The local model provides a TCP server
            // While the front-end model will typically only request to this server
            if (mode == HostMode.BACKEND)
            {
                ConfigureBackend();
                _establishedConnection = true;
            }
            else
            {
                if (!mode.HasFlag(HostMode.REMOTE))
                {
                    ClientPort = backendPort + Interlocked.Increment(ref LocalCreated);
                    if (LocalCreated >= 2)
                    {
                        Logger.Warn(Messages.InsufficientAddressLength);
                    }
                }

                // Now configure the front and back end components
                ConfigureFrontend();
                if (mode.HasFlag(HostMode.REMOTE))
                {
                    ConfigureRemote();
                }
            }
        }

        /// <summary>
        /// Main configuration method for backend stuff. Configures the 
        /// connection and sets up the callbacks that maintain library integrity
        /// </summary>
        private void ConfigureBackend()
        {
            // Initialize a TCP provider
            BackendProvider = new ConnectionProvider(Port);

            // Setup a message channel
            Logger.Info($"Initializing Backend UDP on {Port}.");
            MessageChannel = new UDPChannel(Port, ClientPort);

            // Set up a simple packet receiver to allow clients
            // to inform us of their ports
            MessageChannel.OnReceive += (s) =>
            {
                if (s.StartsWith("port:"))
                {
                    if (int.TryParse(s.Substring(5).Trim(), out var p))
                    {
                        ClientPort = p;
                        MessageChannel.SendPort = p;
                        KnownPorts.TryAdd(p, true);
                        MessageChannel.Send($"confirm:{ClientPort}");
                    }
                }
            };

            return;

            // Now Register a series update entiry point
            LibraryModel.OnSeriesUpdate += (lib, series) =>
            {
                StringBuilder sb = new StringBuilder();
                List<byte[]> sendData = new List<byte[]>();

                sb.Append("update:");
                for (int i = 0; i < series.Count; ++i)
                {
                    string str = series[i].id + ';';
                    if (sb.Length + str.Length > 450)
                    {
                        sendData.Add(Encoding.ASCII.GetBytes(sb.ToString()));
                        sb.Clear();
                        sb.Append("update:");
                    }
                    sb.Append(series[i].id);
                    sb.Append(';');
                }

                if (sb.Length > 0)
                {
                    sendData.Add(Encoding.ASCII.GetBytes(sb.ToString()));
                }

                //now send all of the infos
                foreach (var d in sendData) MessageChannel.Send(d);

            };
        }

        private void ConfigureFrontend()
        {
            //notify the server
            Logger.Info($"Initializing Frontend. Remote: {Mode.HasFlag(HostMode.REMOTE)}");
            Logger.Info($"Initializing Head UDP on Port {ClientPort}. Server Expected At {Port}");
            MessageChannel = new UDPChannel(ClientPort, Port);

            // set up a simple listener that will catch when the backend responds
            Action<string>? x = null;
            x = (s) =>
            {
                try
                {
                    if (s == $"confirm:{ClientPort}")
                    {
                        Logger.Info("Head established server connection!");
                        _establishedConnection = true;
                        MessageChannel.OnReceive -= x;
                    }
                }
                catch { }
            };

            MessageChannel.OnReceive += x;

            // Set up a loop that looks for the backend
            Task.Run(async () =>
            {
                int delay = 0;
                while (!_establishedConnection)
                {
                    MessageChannel.Send($"port:{ClientPort}");
                    int n = int.Min(delay++ - 5, 0);
                    await Task.Delay(200 + n * n * n * 50);
                }
            });
        }


        private void ConfigureRemote()
        {
            Logger.Info($"Configuring Head in Remote mode.");





        }







    }

}
#endif
