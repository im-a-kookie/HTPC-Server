# Connections Library

The **Connections** library is a simple and powerful library for handling UDP and HTTP(S) over TCP connections. It makes it easy to set up a server that can handle HTTP requests and send responses using a straightforward API.

## Features

- **HTTP(S) Support**: Handles incoming HTTP and HTTPS requests.
- **UDP Support**: Provides basic UDP connectivity.
- **Connection Provider**: Instantiates a server to manage incoming connections.
- **Request and Response Handling**: Easily catch and respond to packets
- **Custom Certificate**: Provides easy implementation of SSL for TLS
- **Multithreaded**: TCP/HTTP Server hosted on dynamic threadpool for higher throughput

## Installation

Add the `Connections` library to your project by referencing the compiled DLL or using a package manager if available.

## Usage

### 1. Creating a Connection Provider

To start handling HTTP(S) requests, instantiate a `ConnectionProvider` on a specific port. Optionally, provide an SSL certificate for secure connections.

```csharp
using Connections;
using System.Security.Cryptography.X509Certificates;

// Create a new connection provider on port 8080 with an optional SSL certificate
var connectionProvider = new ConnectionProvider(8080, new X509Certificate2("path/to/your/certificate.pfx", "password"));
```

The ConnectionProvider exposes cancellation tokens and standard Close() and Dispose() methods for easy shutdown.

### 2. Reply to HTTP Requests via Event using the Response argument

```csharp
connectionProvider.OnRequest += (request, response) =>
{
    Response.Deliver(filepath);
};
```

Some default responses are available;

```csharp
connectionProvider.OnRequest += (request, response) =>
{
    if(redirecting)
    {
        Response.Redirect(newUrl);
    }
    else if (!File.Exists(filePath))
    {
        Response.NotFound();
    }
    else Deliver(filePath);
};
```


### 3. Request provides Headers and Endpoint information

Headers are provided via a Dictionary mapping, while endpoint and REST method are provided internally;

```csharp
// The HTTP method
HttpMethod = request.Method;

// Header and endpoint information
var headers = request.Headers;
if (headers.ContainsKey("Content-Type"))
{
    string contentType = headers["Content-Type"];
}

// Easily get endpoints
var endpoint = request.Target;
response.ProvideJson(YourApiManager.GetDataFor(endpoint));

```

### Licensing, Contribution, Usage

MIT License. Free free to fork, clone, submit PR, or otherwise use however you want.