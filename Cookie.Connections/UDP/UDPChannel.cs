using Cookie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.UDP
{


    /// <summary>
    /// The UDP channel allows UDP messages to be sent back and forth between instances. This allows quick
    /// simple communication without the fuss of TCP.
    /// 
    /// <para>
    /// In short, a remote host model contacts a backend model initially, which can be done through a known port/etc,
    /// or can be done through the TCP connection provider. The client is then allocated a UDP channel on which
    /// the remote server can message it.
    /// </para>
    /// 
    /// <para>
    /// The client can then send and receive UDP packets using these parameters, allowing for quick and easy
    /// simple messages to be communicated.
    /// </para>
    /// </summary>
    public class UDPChannel
    {


        public event Action<string>? OnReceive;

        public CancellationTokenSource Cancellation = new();

        public int ListenPort;
        public int SendPort;

        public UDPChannel(int listenPort, int sendPort)
        {
            this.ListenPort = listenPort;
            this.SendPort = sendPort;
            new Thread(Listen).Start();
        }

        /// <summary>
        /// Sends a UDP message to the given port
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            using UdpClient udpClient = new UdpClient();
            udpClient.Connect("localhost", SendPort);
            udpClient.Send(Encoding.ASCII.GetBytes(message));
        }

        /// <summary>
        /// Sends a UDP message to the given port
        /// </summary>
        /// <param name="message"></param>
        public void Send(byte[] message)
        {
            using UdpClient udpClient = new UdpClient();
            udpClient.Connect("localhost", SendPort);
            udpClient.Send(message);
        }

        public async void Listen()
        {
            Logger.Debug("UDP Start on " + ListenPort);
            UdpClient udc = new UdpClient(ListenPort);
            try
            {
                while (!Cancellation.IsCancellationRequested)
                {
                    var result = udc.ReceiveAsync(Cancellation.Token);
                    await result;
                    if (result.IsCompletedSuccessfully)
                    {
                        //process the result
                        var connection = result.Result;
                        string s = Encoding.ASCII.GetString(connection.Buffer);
                        Logger.Debug($"UDP ({ListenPort}): {s}");
                        OnReceive?.Invoke(s);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Warn($"Error occured in UDP Listener: {e}");
            }
            finally
            {
                if (Cancellation.IsCancellationRequested)
                {
                    udc.Dispose();
                }
                else
                {
                    new Thread(Listen).Start();
                }
            }


        }






    }

}
