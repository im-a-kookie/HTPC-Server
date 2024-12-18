using Cookie.Logging;

#if !BROWSER
using Cookie.TCP;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
#endif

namespace Cookie.Connections.API
{
    public class Controller<Program>
    {
        private static class Errors
        {
            public static Error InvalidControllerError = new("Invalid Controller", "The controller requires Endpoint or Controller attributes", (m, e) => new ArgumentException(m, e));

            public static Error NoConstructor = new("Cannot Create Host", "The endpoint class could not be constructed!", (m, e) => new ArgumentException(m, e));

        }

        private static class Warnings
        {

            public static Message NoEndpoints = new("No Declared Endpoints", "The class did not declare endpoints!");

            public static Message NoRequestParams = new("No Request Parameters", "The method provides no parameters for inspecting the request!");

        }

        public Program Instance { get; private set; }

        public Controller(Program owner)
        {
            Instance = owner;
        }

#if !BROWSER

        private ConnectionProvider? Server = null;

        /// <summary>
        /// Sets the provider for this controller
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public Controller<Program> SetProvider(FileProvider provider)
        {
            this.Fileserver = provider;
            return this;
        }

        /// <summary>
        /// Starts an HTTP server from this controller
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        /// <returns></returns>
        public Controller<Program> StartHttp(int port, X509Certificate2 ssl)
        {
            lock (this)
            {
                if (Server != null)
                {
                    Server = new ConnectionProvider(port, ssl);
                    Server.OnRequest += async (request, response) =>
                    {
                        ReceiveRequest(request, response);
                        return;
                    };
                }
            }
            return this;
        }

        /// <summary>
        /// Provides an awaitable task for awaiting server closure
        /// </summary>
        /// <returns></returns>
        public async Task AwaitServerClosureAsync()
        {
            if (Server == null) return;
            await Server.ClosureAwaitable;
            return;
        }

        /// <summary>
        /// The fileserver for this connection
        /// </summary>
        public FileProvider? Fileserver { get; private set; }

        /// <summary>
        /// The internal callback dictionary that maps endpoints to their processors
        /// </summary>
        public Dictionary<string, (object? caller, ApiDelegate callback)> Callbacks = new();

        public Response ReceiveRequest(Request request, Response response)
        {

            string path = request.Target.ToLower();
            if (Callbacks.TryGetValue(path, out var call))
            {
                // let's try to read the body of the request as a string
                bool doText = false;
                bool doJson = false;
                if (request.Headers.TryGetValue("Content-Type", out var type))
                {
                    if (type.Contains("json"))
                    {
                        doJson = true;
                        doText = true;
                    }
                    else if (type.Contains("text"))
                    {
                        doText = true;
                    }
                }

                // read the body as text if required
                string? body = doText ? Encoding.UTF8.GetString(request.RequestData).Trim() : "";

                // Now let's invoke the callback
                var result = call.callback(
                    call.caller,
                    request,
                    response,
                    request.Parameters,
                    (doJson) ? body : "",
                    body,
                    request.RequestData,
                    request.Method);

                switch (result)
                {
                    case Response r:
                        response = r;
                        break;

                    case HttpStatusCode code:
                        response.SetResult(code);
                        break;

                    case int code:
                        response.SetResult((HttpStatusCode)code);
                        break;

                    case string data:
                        HandleString(response, data);
                        break;
                }
            }

            // return a response yes
            return response;
        }

        /// <summary>
        /// Handles a simple string return from a callback, determines whether the return
        /// is a filepath/target, a json, or an html result.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="input"></param>
        public void HandleString(Response r, string input)
        {
            input = input.Trim();

            // Examine the fileserver first
            if (Fileserver != null)
            {
                var attempt = input;
                var result = Fileserver.ProvideFile(new(), ref attempt);
                if (result == HttpStatusCode.OK && File.Exists(attempt))
                {
                    r.SetFile(attempt);
                    return;
                }
            }

            // Now see if it's a json
            if (input.StartsWith("{") || input.StartsWith("["))
            {
                r.SetJson(input);
            }
            else //if (input.StartsWith("<"))
            {
                // it's probably HTML then, and HTML will be understood anyway
                r.SetHtml(input);
            }
        }


        /// <summary>
        /// Discovers the given type into this API using the <see cref="Route"/> attribute and class/method 
        /// structure to organize the API endpoints.
        /// 
        /// <para>Generates delegates into <see cref="Callbacks"/> for invocation through <see cref="ReceiveRequest(Request)"/></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Discover<T>() where T : class
        {
            Discover<T>(null);
        }

        /// <summary>
        /// Discovers the given type into this API using the <see cref="Route"/> attribute and class/method 
        /// structure to organize the API endpoints.
        /// 
        /// <para>Generates delegates into <see cref="Callbacks"/> for invocation through <see cref="ReceiveRequest(Request)"/></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Discover<T>(T? container) where T : class
        {
            var t = typeof(T);

            string name = t.Name;

            // get the alias if present from a route attribute
            var attributes = t.GetCustomAttributes(true);
            foreach (var attrib in attributes)
            {
                if (attrib is Route controller)
                {
                    if (controller.Alias != null) name = controller.Alias;
                }
            }

            // Walk up the type stack to rebuild the call hierarchy
            var tt = t.DeclaringType;
            while (tt != null)
            {
                string innerName = tt.Name;
                attributes = tt.GetCustomAttributes(true);
                foreach (var attrib in attributes)
                {
                    if (attrib is Route controller)
                    {
                        if (controller.Alias != null) innerName = controller.Alias;
                    }
                }
                // append the name and walk back again
                name = innerName + "/" + name;
                tt = tt.DeclaringType;
            }
            // clear bad slashes
            while (name.Contains("//")) name = name.Replace("//", "/");


            // accumulate methods and cache the name/alias
            List<MethodInfo> validMethods = new();
            List<string> methodAlias = new();
            MethodInfo? creator = null;

            bool hasInstanceMethods = false;

            // Now go through every method
            foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                attributes = method.GetCustomAttributes(true);
                foreach (var attrib in attributes)
                {
                    if (attrib is Route controller)
                    {
                        // Now prune based on signature
                        if (method.GetParameters().Any(x => x.IsOut || x.IsRetval)) continue;
                        validMethods.Add(method);
                        // and register the name also
                        methodAlias.Add(controller.Alias ?? method.Name);

                        if (!method.IsStatic) hasInstanceMethods = true;

                    }
                    else if (attrib is Creator create
                        && method.IsStatic
                        && method.ReturnType == typeof(T)
                        && method.GetParameters().Count() == 0)
                    {
                        creator = method;
                    }
                }
            }

            // Now validate that we have a calling instance that we can use
            //T? container = null;
            if (hasInstanceMethods)
            {
                container = CreateContainer<T>(creator);
                // It's still null, so lets trying to use the constructor
                if (container == null)
                {
                    throw Errors.NoConstructor.Get(typeof(T).FullName);
                }
            }


            // and ensure that we have things in this endpoint
            if (validMethods.Count == 0)
            {
                Warnings.NoEndpoints.Warn(t.FullName);
            }

            // Now we will try tp map the calling functions
            for (int i = 0; i < validMethods.Count; ++i)
            {
                var method = validMethods[i];
                // Let's create a mapping callback
                var del = Emission.DelegateEmitter.GetMapping<T, ApiDelegate>(method);
                if (del == null) continue;
                // Now append the alias of the endpoint handler
                string target = $"/{name}/{methodAlias[i]}".ToLower();
                // and clean it
                while (target.Contains("//")) target = target.Replace("//", "/");

                // now generate a callback
                if (method.IsStatic) Callbacks.Add(target, (null, del));
                else Callbacks.Add(target, (container, del));

            }


            // Now look inside this type for more api calls
            foreach (var child in t.GetNestedTypes() ?? [])
            {
                if (t.IsClass)
                {
                    try
                    {
                        var myType = GetType();
                        var myMeth = myType.GetMethod("Discover", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
                        var myGeny = myMeth.MakeGenericMethod(child);
                        myGeny.Invoke(this, []);
                    }
                    catch
                    {
                        //uuuugh
                    }
                }

            }

        }

        /// <summary>
        ///  Attempts to create a container instance from the given typed container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="creator"></param>
        internal T? CreateContainer<T>(MethodInfo? creator) where T : class
        {
            var t = typeof(T);
            T? container = null;
            if (creator != null) container = (T?)creator!.Invoke(null, null) ?? null;
            if (container == null)
            {
                // Try to use the program type first
                var c = t.GetConstructor([typeof(Program)]);
                if (c != null) container = (T?)c.Invoke([Instance]) ?? null;
                else
                {
                    // use this controller first
                    c = t.GetConstructor([typeof(Controller<Program>)]);
                    if (c != null) container = (T?)c.Invoke([this]) ?? null;
                    else
                    {
                        // use a default object/wildcard
                        c = t.GetConstructor([typeof(object)]);
                        if (c != null) container = (T?)c.Invoke([Instance]) ?? null;
                        else
                        {
                            // Or use an empty constructor
                            c = t.GetConstructor(Type.EmptyTypes);
                            if (c != null) container = (T?)c.Invoke(null) ?? null;
                        }
                    }
                }
            }
            return container;
        }


        public void Discover(Type t)
        {
            typeof(Controller<Program>).GetMethod("Discover", BindingFlags.Public, Type.EmptyTypes)!.MakeGenericMethod(t).Invoke(this, []);
        }


        public void Discover(object instance)
        {
            typeof(Controller<Program>).GetMethod("Discover", BindingFlags.Public, [instance.GetType()])!.MakeGenericMethod(instance.GetType()).Invoke(this, [instance]);
        }

#endif


    }
}
