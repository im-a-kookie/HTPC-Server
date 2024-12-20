using System.Net;

namespace Cookie.Connections.API
{
    public class FileProvider
    {
        /// <summary>
        /// A permission level representing this provider
        /// </summary>
        public User.PermissionLevel ReadLevel { get; set; } = User.PermissionLevel.LOW;

        /// <summary>
        /// A permission level representing this provider
        /// </summary>
        public User.PermissionLevel WriteLevel { get; set; } = User.PermissionLevel.HIGH;

        /// <summary>
        /// A delegate for validating access to the file provider
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public delegate bool AccessValidator(FileProvider provider, User? user, string? target);

        /// <summary>
        ///  Predicate for validating whether the given user can read from this file provider
        /// </summary>
        public AccessValidator ValidateRead = (x, y, target) => x.ReadLevel <= (y?.ReadLevel ?? 0);
        /// <summary>
        /// Predicate for validating whether the given user can write to this file provider
        /// </summary>
        public AccessValidator ValidateWrite = (x, y, target) => x.WriteLevel <= (y?.ReadLevel ?? 0);

        /// <summary>
        /// A predicate function that transforms requested paths into actual filepaths
        /// </summary>
        public Func<string, string?> PathTransformer = (x) => x;

        /// <summary>
        /// Attempts to load a file from a string target to an absolute filepath. Returns
        /// null if the user does not have permission to access the given file.
        /// </summary>
        /// <param name="requester"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public HttpStatusCode ProvideFile(User? requester, ref string? path)
        {
            path ??= "/";

            if (!ValidateRead(this, requester, path))
            {
                path = null;
                return HttpStatusCode.Unauthorized;
            }
            path = PathTransformer(path);
            if (path == null) return HttpStatusCode.NotFound;
            return HttpStatusCode.OK;
        }



    }
}
