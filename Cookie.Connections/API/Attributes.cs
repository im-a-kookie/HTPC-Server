namespace Cookie.Connections.API
{
    /// <summary>
    /// Defines an API Route. The name/alias defines the path on a class, and the endpoint on a method.
    /// 
    /// <para>
    /// Method parameters are matched flexibly by type and name from the <see cref="ApiDelegate"/> signature;
    /// <list type="bullet">
    /// <item><see cref="Request"/> request. Incoming request.</item>
    /// <item><see cref="Response"/> response. Output response. </item>
    /// <item><see cref="string"/> query. ?extra suffix of URL target</item>
    /// <item><see cref="string"/> json. Json representation of body (if valid)</item>
    /// <item><see cref="string"/> text. Text representation of body (if valid)</item>
    /// <item><see cref="byte"/>[] data. Raw byte representation of data.</item>
    /// <item><see cref="HttpMethod"/> method. REST method of request.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class Route : Attribute
    {
        public string? Alias { get; set; }
        public string? Description { get; set; }

        public Route() { }
        public Route(string alias) { this.Alias = alias; }
        public Route(string alias, string description) { this.Alias = alias; this.Description = description; }

    }

    [AttributeUsage(AttributeTargets.Method)]

    public class Creator : Attribute
    {

    }


}
