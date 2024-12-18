using Cookie.Addressing;
using Cookie.Connections;
using Cookie.Logging;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;


#if !BROWSER
namespace Cookie.TCP
{
    public class ConnectionListener : Addressable
    {

        /// <summary>
        /// A counter estimating the number of live requests being processed
        /// </summary>
        internal int Working = 0;
        /// <summary>
        /// A counter indicating the number of requests (x1e3). 
        /// <para>
        /// Note: The underlying connection absorbs this value regularly for metrics.
        /// </para>
        /// </summary>
        internal int RequestRateCounter = 0;

        /// <summary>
        /// A counter for the number of in-flight calls
        /// </summary>
        internal int InFlightCalls = 0;


        public bool QuietExit = false;

        /// <summary>
        /// A boolean flag indicating whether this listener is still alive
        /// </summary>
        internal bool Alive = true;
        /// <summary>
        /// The cancellation token for this listener
        /// </summary>
        internal CancellationToken Token;

        /// <summary>
        /// The underlying connection for this listener
        /// </summary>
        private ConnectionProvider connection;
        private Task cancellationTask;


        /// <summary>
        /// Creates a new listener atop the given connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="token"></param>
        internal ConnectionListener(ConnectionProvider connection, CancellationToken token)
        {
            this.Token = token;
            this.connection = connection;

            // Set up a cancellation task
            TaskCompletionSource<bool> tcs = new();
            cancellationTask = tcs.Task;
            Token.Register(() => tcs.SetResult(true));

            // Start the actual listening aspect of this connection
            Thread t = new(() =>
            {
                Listen();
                Alive = false;
            });
            t.Start();
        }

        /// <summary>
        /// The main listener entry point for this listener
        /// </summary>
        internal async void Listen()
        {
            List<Task> active = new();
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    // Allow pausing from the underlying connection
                    connection.listenerSignal.WaitOne();
                    if (Token.IsCancellationRequested) break;

                    if (InFlightCalls > 3)
                    {
                        await Task.Delay(InFlightCalls * InFlightCalls * 50);
                    }

                    // Await a response
                    var client = connection.listener.AcceptTcpClientAsync(Token);
                    await client;
                    // We are now counted as a live thread
                    Interlocked.Increment(ref InFlightCalls);
                    Interlocked.Decrement(ref connection.IdleWorkers);

                    connection.Notify();

                    // Run the task if we completed
                    if (client.IsCompletedSuccessfully)
                    {
                        var tcpClient = client.Result;
                        active.Add(Task.Run(async () =>
                        {
                            //using (tcpClient) // Ensures cleanup of the TcpClient
                            var stream = await GetClientStream(tcpClient);
                            await Process(tcpClient, stream);
                            Interlocked.Decrement(ref InFlightCalls);
                        }));
                        // Whatever
                        Interlocked.Add(ref RequestRateCounter, 1000);
                    }
                    else
                    {
                        Interlocked.Decrement(ref InFlightCalls);
                        if (client.Result != null) client.Result.Close();
                    }

                    // This thread is no longer live
                    Interlocked.Increment(ref connection.IdleWorkers);

                    if (QuietExit) return;
                }
            }
            catch { }
            finally
            {
                // now wait for everything to exit
                await Task.WhenAll(active);
                Interlocked.Decrement(ref connection.IdleWorkers);
            }
            Logger.Log($"Listener closed: {Address}");

        }

        /// <summary>
        /// Establishes a client stream, using the given client. If the underlying <see cref="connection"/> is configured
        /// to use SSL, then an SSL stream is returned with <see cref="ConnectionProvider.SSL"/>, otherwise, the naked
        /// TCP stream will be used.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        internal async Task<Stream> GetClientStream(TcpClient client)
        {
            if (connection.SSL != null)
            {
                // Create an SSL stream for secure communication.
                SslStream sslStream = new SslStream(
                    client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => true),
                    null
                );

                // Authenticate the server using the SSL certificate.
                await sslStream.AuthenticateAsServerAsync(connection.SSL, false, SslProtocols.Tls12, true);
                Logger.Debug($"Established SSL connection on {client.Client.RemoteEndPoint}");
                return sslStream;
            }
            else
            {
                return client.GetStream();
            }
        }



        /// <summary>
        /// Processes the input client request.
        /// </summary>
        /// <param name="client"></param>
        internal async Task Process(TcpClient client, Stream stream)
        {

            try
            {
                // get the underlying stream
                // Establish a request and response
                var request = new Request();
                await request.ReadAsync(stream);
                var response = new Response(request);




            }
            catch (Exception e)
            {
                Logger.Error($"TCP Client Listener threw: {e}");
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }

        public override void Exit()
        {

        }
    }
}
#endif