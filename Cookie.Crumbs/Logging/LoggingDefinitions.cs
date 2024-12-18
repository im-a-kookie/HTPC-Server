namespace Cookie.Logging
{
    /// <summary>
    /// Denotes that the marked class contains logging attributes. Allows these attributes to be loaded
    /// into the runtime consistently with their declaration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class LoggingDefinitions : Attribute
    {
    }
}
