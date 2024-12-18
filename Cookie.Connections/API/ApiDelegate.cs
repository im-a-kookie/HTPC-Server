namespace Cookie.Connections.API
{
#if !BROWSER
    public delegate object? ApiDelegate(
        object? caller,
        Request request,
        Response response,
        string? query,
        string? json,
        string? text,
        byte[]? body,
        HttpMethod method);
#endif

}
