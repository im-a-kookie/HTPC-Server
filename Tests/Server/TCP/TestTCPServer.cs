using Cookie.Connections;
using Cookie.Logging;
using Cookie.TCP;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Server.TCP
{
    [TestClass]
    public class TestTCPServer
    {
        static int Port = 12345;
        static int Timeout = 5000;
        static HttpStatusCode ExpectedResult = HttpStatusCode.OK;

        /// <summary>
        /// Validates that the TCP HTTP server can be intereacted with using standard
        /// HTTP client methods.
        /// </summary>
        [TestMethod]
        public void TestTCPServerHandlesRequests()
        {

            using ConnectionProvider server = new ConnectionProvider(Port);
            using ManualResetEvent signal = new(false);

            // setup a cancellation
            using var canceller = new CancellationTokenSource();
            canceller.CancelAfter(Timeout);

            //now we have a test server running, we should test that it can respond
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            server.OnRequest += async (x) =>
            {
                ConsoleOutput.Instance.WriteLine("Request Callback invoked!", OutputLevel.Information);
                signal.Set();
                return new Response().NotFound().SetResult(ExpectedResult);
            };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            // setup a task to send the HTTP request
            var task = Task.Run(async () =>
            {
                try
                {
                    // give it a quick moment to warm up
                    await Task.Delay(50);

                    using var client = new HttpClient();
                    var response = await client.GetAsync($"http://localhost:{Port}/", canceller.Token);

                    // now validate a correct response
                    ConsoleOutput.Instance.WriteLine("HTTP Response Received!", OutputLevel.Information);
                    return response.StatusCode == ExpectedResult;
                }
                catch
                {
                    return false;

                }
            }, canceller.Token);

            // backup timeout function
            var timeout = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Timeout, canceller.Token);
                    return true;
                }
                catch 
                {
                    return false;
                }

            }, canceller.Token);

            // Wait for either cancellation or completion of request sending
            Task.WaitAny(timeout, task);

            // also ensure that we processed the OnRequest callback
            bool callbackSignalSet = signal.WaitOne(100);

            // We're done so ensure cancellation of all junk
            canceller.Cancel();
            Task.WaitAll(timeout, task);

            // validate all of the results
            Assert.IsTrue(callbackSignalSet, "Callback was not invoked!");
            Assert.IsTrue(task.IsCompletedSuccessfully, "Failure sending request!");
            Assert.IsTrue(task.Result, "Failure sending request and receiving correct response!");
        }
    }
}
