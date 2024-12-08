using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CookieCrumbs.UDP
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
    internal class UDPChannel
    {

        public CancellationTokenSource Cancellation = new();

        public int ListenPort;
        
        public async void Listen()
        {
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
                        string s = Encoding.UTF8.GetString(connection.Buffer);
                        Logger.Default.Info($"UDP: {s}");
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Default.Warn($"Error occured in UDP Listener: {e}");
            }
            finally
            {
                udc.Dispose();
            }


        }






    }

}
