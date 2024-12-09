using Cookie.Addressing;
using Cookie.Utils;
using System.Linq.Expressions;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Cookie.TCP
{
    internal class ConnectionListener : Addressable
    {

        /// <summary>
        /// A counter estimating the number of live requests being processed
        /// </summary>
        public bool Working = false;
        /// <summary>
        /// A counter indicating the number of requests (x1e3). 
        /// <para>
        /// Note: The underlying connection absorbs this value regularly for metrics.
        /// </para>
        /// </summary>
        public int RequestRateCounter = 0;

        /// <summary>
        /// A counter for the number of in-flight calls
        /// </summary>
        public int InFlightCalls = 0;

        /// <summary>
        /// A boolean flag indicating whether this listener is still alive
        /// </summary>
        public bool Alive = true;
        /// <summary>
        /// The cancellation token for this listener
        /// </summary>
        public CancellationToken Token;

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
        public ConnectionListener(ConnectionProvider connection, CancellationToken token)
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
        public async void Listen()
        {
            Interlocked.Increment(ref connection.IdleWorkers);
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
                    Interlocked.CompareExchange(ref Working, false, true);
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
                            using (var stream = await GetClientStream(tcpClient))
                                await Process(stream);

                            tcpClient.Close();
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
                    Interlocked.CompareExchange(ref Working, true, false);
                    Interlocked.Increment(ref connection.IdleWorkers);
                }
            }
            catch { }
            finally
            {
                // now wait for everything to exit
                await Task.WhenAll(active);
                Interlocked.Decrement(ref connection.IdleWorkers);
            }
        }

        /// <summary>
        /// Establishes a client stream, using the given client. If the underlying <see cref="connection"/> is configured
        /// to use SSL, then an SSL stream is returned with <see cref="ConnectionProvider.SSL"/>, otherwise, the naked
        /// TCP stream will be used.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task<Stream> GetClientStream(TcpClient client)
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
        public async Task Process(Stream stream)
        {

            try
            {
                // get the underlying stream
                // Establish a request and response
                var request = new RequestReader(stream);
                await request.Read();

                var response = new ResponseSender(request);
                connection.CallOnRequest(request, response);

            }
            catch(Exception e)
            {
                Logger.Error($"TCP Client Listener threw: {e}");
            }
            finally
            {
                stream?.Close();
            }
        }

        public override void Exit()
        {
            
        }
    }
}
