using Cookie.Connections;
using Cookie.Connections.API;
using Cookie.Connections.API.Logins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.API
{

    [Route("remote", "Defines the endpoint container for controlling the HTPC remotely")]
    public class Remote
    {
        /// <summary>
        /// Enumeration of valid/recognized inputs
        /// </summary>
        public enum InputType
        {
            TEXT =      (1 << 0),
            OK =        (1 << 1),
            CANCEL =    (1 << 2),
            BACK =      (1 << 3),
            FORWARD =   (1 << 4),
            UP =        (1 << 5),
            DOWN =      (1 << 6),
            LEFT =      (1 << 7),
            RIGHT =     (1 << 8),
            PAGE_UP =   (1 << 9),
            PAGE_DOWN = (1 << 10),
            PLAY =      (1 << 11),
            PAUSE =     (1 << 12),
            STOP =      (1 << 13),
        }

        static Dictionary<string, InputType> _lookup = new();

        /// <summary>
        /// Setup the class
        /// </summary>
        static Remote()
        {
            foreach(var v in Enum.GetValues(typeof(InputType)))
            {
                var str = v.ToString() ?? "null";
                _lookup.TryAdd(str.ToLowerInvariant(), (InputType)v);
            }
        }

        /// <summary>
        /// The state string that is provided by <see cref="GetState(User?, string)"/>
        /// </summary>
        public string StateString = "{\"empty\":true}";

        /// <summary>
        /// Delegate for handling user inputs at input route
        /// </summary>
        /// <param name="input"></param>
        /// <param name="data"></param>
        public delegate void InputHandler(InputType input, string? data);

        /// <summary>
        /// Subscribable event called when an input signal is received here
        /// </summary>
        public event InputHandler? OnInput;

        /// <summary>
        /// Simple wrapper object for interpreting JSON
        /// </summary>
        public class InputWrapper
        {
            public string? Input { get; set; }
        }

        [Route("input", "Send a control input to this instance, allowing control over an attached head.\n" +
            "\tExpects: Input layout, $INPUT=...|$INPUT|\n" +
            "\tReturns: OK")]
        public Response SubmitInput(User? user, string json)
        {
            if (user == null) return new Response().NotAuthorized();
            // Now process the input
            InputWrapper? result = JsonSerializer.Deserialize<InputWrapper>(json);
            if(result != null && result.Input != null)
            {
                foreach(string input in result.Input.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    var test = input.Trim();
                    if (test.StartsWith("$"))
                    {
                        var key = test[1..];
                        var pos = test.IndexOf('=');
                        if (pos != -1)
                        {
                            key = test[..pos];
                            test = test[(1 + pos)..];
                        }
                        if(_lookup.TryGetValue(key.Trim().ToLowerInvariant(), out var code))
                        {
                            OnInput?.Invoke(code, test);
                        }
                    }
                }
            }
            return new Response().SetSuccessJson();
        }

        /// <summary>
        /// Gets the current state object from the controller, as a string
        /// </summary>
        /// <param name="user"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("state", "Provides a json string describing the current state/page of the server.")]
        public Response GetState(User? user, string query)
        {
            if (user == null) return new Response().NotAuthorized();
            return new Response().SetJson(StateString).SetResult(HttpStatusCode.OK);
        }


    }
}
