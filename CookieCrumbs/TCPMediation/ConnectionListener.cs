using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CookieCrumbs.TCPMediation
{
    internal class ConnectionListener
    {
        public int Total = 0;
        public int Counter = 0;

        public bool Alive = true;
        public CancellationToken Token;
        ConnectionProvider connection;
        public ConnectionListener(ConnectionProvider connection, CancellationToken token)
        {
            this.Token = token;
            this.connection = connection;

            Thread t = new(() =>
            {
                Listen();
                Alive = false;
            });
            t.Start();
        }

        public async void Listen()
        {
            List<Task> active = new();
            while(!Token.IsCancellationRequested)
            {
                //Add an inherent rate limiter under higher load
                active.RemoveAll(x => x.IsCompleted);
                int min = 5;
                if (active.Count >= min)
                {
                    Interlocked.Increment(ref connection.StressedWorkers);
                    connection.Notify();
                    int n = int.Max(0, active.Count - min);
                    int m = int.Max(0, active.Count - (min * 2));
                    await Task.Delay(m * m * m * 10 + n * n * 10);
                    Interlocked.Decrement(ref connection.StressedWorkers);
                }

                connection.barrier.WaitOne();
                if (Token.IsCancellationRequested) break;
                Interlocked.Increment(ref Total);

                var client = connection.listener.AcceptTcpClientAsync(Token);
                await client;

                if(client.IsCompletedSuccessfully)
                {
                    active.Add(Task.Run(() => Process(client.Result)));
                    Interlocked.Add(ref Counter, 1000);
                }
                else Interlocked.Decrement(ref Total);
            }

            await Task.WhenAll(active);
        }

        public void Process(TcpClient? client)
        {
            try
            {
                
                using (NetworkStream networkStream = client!.GetStream())
                using (StreamWriter writer = new StreamWriter(networkStream, Encoding.UTF8))
                {

                    string content =
@"<!DOCTYPE html>
<html lang='en'>
<head>
<title>TCP Response</title>
</head>
<body>
<h1>Hello, World!</h1>
</body>
</html>";

                    string result =
                    @"HTTP/1.1 200 OK
Content-Type: text/html; charset=UTF-8";



                    byte[] data = Encoding.UTF8.GetBytes(content);
                    writer.WriteLine(result);
                    writer.WriteLine("Connection: close");
                    writer.WriteLine($"Content-Length: {data.Length}");
                    writer.WriteLine("");
                    writer.WriteLine(content);
                    writer.Flush();
                    writer.Close();

                    networkStream.Close();
                }

                client.Close();
            }
            finally
            {
                client?.Close();
                Interlocked.Decrement(ref Total);
            }
        }











    }
}
