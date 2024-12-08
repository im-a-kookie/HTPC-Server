using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace CookieCrumbs.TCP
{
    internal class ConnectionListener
    {

        /// <summary>
        /// A counter estimating the number of live requests being processed
        /// </summary>
        public int EstimatedLiveRequests = 0;
        /// <summary>
        /// A counter indicating the number of requests (x1e3). 
        /// <para>
        /// Note: The underlying connection absorbs this value regularly for metrics.
        /// </para>
        /// </summary>
        public int RequestRateCounter = 0;

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
            List<Task> active = new();
            while (!Token.IsCancellationRequested)
            {
                //Add an inherent rate limiter under higher load
                active.RemoveAll(x => x.IsCompleted);
                int min = 5;
                if (active.Count >= min)
                {
                    // Increment the stressed-worker counter because yes
                    try
                    {
                        Interlocked.Increment(ref connection.StressedWorkers);
                        connection.Notify();
                        // Let's calculate a simple scaling formula for load balancing
                        int n = int.Max(0, active.Count - min);
                        int m = int.Max(0, active.Count - (min * 2));
                        int len = n * n + m * m * m;
                        len *= 10;
                        len = int.Min(1000, len);
                        // As this operation may become very long
                        // Allow it to be cancelled
                        await Task.WhenAny(Task.Delay(len), cancellationTask);
                    }
                    catch { }
                    finally
                    {
                        // We're done waiting, so we can exit out of here
                        Interlocked.Decrement(ref connection.StressedWorkers);
                    }
                }

                // Allow pausing from the underlying connection
                connection.listenerSignal.WaitOne();
                if (Token.IsCancellationRequested) break;

                // Await a response
                var client = connection.listener.AcceptTcpClientAsync(Token);
                await client;

                // We are now counted as a live thread
                Interlocked.Increment(ref EstimatedLiveRequests);

                // Run the task if we completed
                if (client.IsCompletedSuccessfully)
                {
                    active.Add(Task.Run(() => Process(client.Result)));
                    // Counter is stuck as an int, but this is no biggie
                    // We can just scale it up here, and back down later
                    Interlocked.Add(ref RequestRateCounter, 1000);
                }
                // This thread is no longer live
                else Interlocked.Decrement(ref EstimatedLiveRequests);
            }
            // now wait for everything to exit
            await Task.WhenAll(active);
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
                Console.WriteLine("SSL handshake completed.");
                return sslStream;
            }
            else
            {
                return client!.GetStream();
            }
        }

        /// <summary>
        /// Processes the input client request.
        /// </summary>
        /// <param name="client"></param>
        public async void Process(TcpClient client)
        {
            try
            {
                // get the underlying stream
                using Stream? underlyingStream = await GetClientStream(client!);

                // Establish a request and response
                var request = new RequestReader(underlyingStream);
                var response = new ResponseSender(request);

                // Now we need to handle the request parameters
                if (request.Target.StartsWith("stream:"))
                {
                    //read the target and stream it
                }
                else
                {
                    //see if it corresponds to a file in the delivery directory

                }

                underlyingStream.Close();
                client.Close();
            }
            catch
            {
                // Log the error?
            }
            finally
            {
                client?.Close();
                Interlocked.Decrement(ref EstimatedLiveRequests);
            }
        }











    }
}
