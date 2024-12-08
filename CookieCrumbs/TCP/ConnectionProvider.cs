using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace CookieCrumbs.TCP
{
    public class ConnectionProvider : IDisposable
    {

        public int MaxThreads { get; set; } = 8;

        /// <summary>
        /// The connection cancellation source
        /// </summary>
        private CancellationTokenSource connectionCanceller = new();

        /// <summary>
        /// An awaitable task for whether the connection is cancelled
        /// </summary>
        private Task CancellationAwaitable;


        /// <summary>
        /// The number of connections that are currently stressed
        /// </summary>
        public int StressedWorkers = 0;

        /// <summary>
        /// The TCP listener for this connection
        /// </summary>
        public TcpListener listener;

        /// <summary>
        /// The listeners that are currently alive and doing things
        /// </summary>
        private List<(ConnectionListener listener, CancellationTokenSource token)> LiveListeners = new();

        /// <summary>
        /// A signal that can be used to pause/resume the connection monitor
        /// </summary>
        public AutoResetEvent monitorSignal = new(false);

        /// <summary>
        /// A barrier that can be used to pause/resume the listener threads
        /// </summary>
        public ManualResetEvent listenerSignal = new(true);

        /// <summary>
        /// The SSL certificate to use on this connection. If null, then HTTP is used.
        /// </summary>
        public X509Certificate2? SSL { get; private set; }

        /// <summary>
        /// Create a new connection provider on the given port
        /// </summary>
        /// <param name="port"></param>
        public ConnectionProvider(int port, X509Certificate2? ssl = null)
        {
            // Set up the cancellation and awaitable subsytems
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CancellationAwaitable = tcs.Task;
            connectionCanceller.Token.Register(() =>
            {
                // indicate that we are done
                tcs.SetResult(true);
                // kill all the things
                foreach (var l in LiveListeners)
                {
                    l.token.Cancel();
                }
            });

            // Create the TCP connection

            // Configure SSL. If null, ignore later
            this.SSL = ssl;

            // Start the listener system-wide.
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"TCP Running on port: {port}");
            StartListener(); //obligatory start a listener
        }

        /// <summary>
        /// Start a listener on this connection
        /// </summary>
        public void StartListener()
        {
            var canceller = new CancellationTokenSource();
            var listener = new ConnectionListener(this, canceller.Token);
            LiveListeners.Add((listener, canceller));

            // Run the maintainer
            Task.Run(Maintain);
        }

        /// <summary>
        /// Internal method for maintaining the pool
        /// </summary>
        public async void Maintain()
        {
            Stopwatch s = Stopwatch.StartNew();
            double movingAverage = 0;
            while (!connectionCanceller.IsCancellationRequested)
            {
                // Do a quick tally of the total rate
                double totalRate = 0;
                foreach (var l in LiveListeners)
                {
                    int val = l.listener.RequestRateCounter;
                    totalRate += val;
                    // absorb the value
                    Interlocked.Add(ref l.listener.RequestRateCounter, -val);
                }

                // Calculate the adjusted rate
                totalRate /= (s.Elapsed.TotalMilliseconds);
                movingAverage = (movingAverage * 5 + totalRate) / (5 + s.Elapsed.TotalSeconds);
                s.Restart(); // Reset counter

                // If there are stressed workers, then we need a new listener
                if (LiveListeners.Count == 0 || (StressedWorkers > 0 && LiveListeners.Count < MaxThreads))
                {
                    StartListener();
                    // arbitrarily increase the moving average,
                    // This biases the request counter such that threads will be closed lazily
                    movingAverage += LiveListeners.Count; // use 'count' to allow for *count multiplier below

                }
                else
                {
                    listenerSignal.Reset(); //temporarily block here
                    while ((movingAverage < 0.05 * LiveListeners.Count) && (LiveListeners.Count > 1))
                    {
                        for (int i = 0; i < LiveListeners.Count; i++)
                        {
                            // Find and bonk the first idle thread
                            if (LiveListeners[i].listener.EstimatedLiveRequests <= 0)
                            {
                                var l = LiveListeners[i];
                                LiveListeners.RemoveAt(i);
                                l.token.Cancel();
                                break;
                            }
                        }
                    }
                    // and allow the listeners to continue
                    listenerSignal.Set();
                }

                // Now await the monitor signal or a short delay
                var t = Task.Run(() => monitorSignal.WaitOne(500));
                await Task.WhenAny(t, CancellationAwaitable);
            }
        }

        /// <summary>
        /// Notifies this thread to perform a monitoring sweep immediately.
        /// </summary>
        public void Notify()
        {
            monitorSignal.Set();
        }

        /// <summary>
        /// Closes this connection provider immediately.
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            // First, tell everything to close
            connectionCanceller.Cancel();
            listener?.Stop();

            List<Task> tasks = new List<Task>();

            //Now let's just wait
            foreach (var l in LiveListeners)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (l.listener.Alive)
                    {
                        await Task.Delay(5);
                    }
                }));
            };

            // now wait for them to be done
            await Task.WhenAll(tasks);

            Dispose();
        }


        /// <summary>
        /// Dispose this connection
        /// </summary>
        public void Dispose()
        {
            // Stop everything and go bonk yay
            listener?.Stop();
            listener?.Dispose();
            connectionCanceller.Cancel();

            // Now await their closures
            foreach (var l in LiveListeners)
            {
                while (l.listener.Alive)
                {
                    Thread.Sleep(1);
                }
                l.token.Dispose();
            }

            monitorSignal.Dispose();
        }


    }
}
