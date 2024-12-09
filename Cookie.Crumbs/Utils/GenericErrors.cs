using Cookie.Utils;
using Cookie.Utils.Exceptions;

namespace Cookie.Crumbs.Utils
{
    public static class GenericErrors
    {

        /// <summary>
        /// Generic null argument exception
        /// </summary>
        public static Error NullArgument = new(new("Null Argument", "The provided argument was null"), (m, e) => new ArgumentNullException(m, e));

        /// <summary>
        /// Error generator for unlocatable content/resources
        /// </summary>
        public static Error MissingResource = new(new("Resource Not Found", "Resource not found at the given location"), (m, e) => new ResourceNotFoundException(m, e), "Path: ");





    }
}
