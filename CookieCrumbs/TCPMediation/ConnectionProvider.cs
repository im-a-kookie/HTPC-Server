using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CookieCrumbs.TCPMediation
{
    public class ConnectionProvider : IDisposable
    {

        public int MaxThreads { get; set; } = 8;

        /// <summary>
        /// The connection cancellation source
        /// </summary>
        CancellationTokenSource connectionCanceller = new();

        /// <summary>
        /// An awaitable task for whether the connection is cancelled
        /// </summary>
        Task CancellationAwaitable;


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
        List<(ConnectionListener listener, CancellationTokenSource token)> LiveListeners = new();


        public AutoResetEvent signal = new(false);

        public ManualResetEvent barrier = new(true);


        /// <summary>
        /// Create a new connection provider on the given port
        /// </summary>
        /// <param name="port"></param>
        public ConnectionProvider(int port)
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
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"TCP Running on port: {port}");
            StartListener();
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
            while(!connectionCanceller.IsCancellationRequested)
            {
                double totalRate = 0;
                foreach (var l in LiveListeners)
                {
                    int val = l.listener.Counter;
                    totalRate += val;
                    Interlocked.Add(ref l.listener.Counter, -val);
                }

                // Calculate the adjusted rate
                totalRate /= (s.Elapsed.TotalMilliseconds);
                movingAverage = (movingAverage * 5 + totalRate) / (5 + s.Elapsed.TotalSeconds);

                // If there are stressed workers, then we need a new listener
                if (StressedWorkers > 0 && LiveListeners.Count < MaxThreads)
                {
                    StartListener();
                    // arbitrarily increase the moving average,
                    // This biases the request counter such that threads will be closed lazily
                    movingAverage += LiveListeners.Count; // use the count, to account for LiveListener.Count below
                }
                else
                {
                    barrier.Set();
                    while ((movingAverage < 0.05 * LiveListeners.Count) && (LiveListeners.Count > 1))
                    {
                        //find and bonk one
                        for (int i = 0; i < LiveListeners.Count; i++)
                        {
                            if (LiveListeners[i].listener.Total <= 0)
                            {
                                var l = LiveListeners[i];
                                LiveListeners.RemoveAt(i);
                                l.token.Cancel();
                                break;
                            }
                        }
                    }
                    barrier.Reset();
                }

                // Await either the signal, or a cancellation request
                var t = Task.Run(() =>
                {
                    signal.WaitOne(100);
                });
                await Task.WhenAny(t, CancellationAwaitable);
                s.Restart();
            }
        }

        /// <summary>
        /// Notify that we should do process the pool
        /// </summary>
        public void Notify()
        {
            signal.Set();
        }


        /// <summary>
        /// Dispose this connection
        /// </summary>
        public void Dispose()
        {
            listener?.Stop();
            listener?.Dispose();
            connectionCanceller.Cancel();

            // Now await their closures
            foreach(var l in LiveListeners)
            {
                while(l.listener.Alive)
                {
                    Thread.Sleep(1);
                }
                l.token.Dispose();
            }

            signal.Dispose();
        }


    }
}
