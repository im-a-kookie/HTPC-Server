namespace Cookie.Utils.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    }
}
