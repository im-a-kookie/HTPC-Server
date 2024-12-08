namespace Cookie
{
    /// <summary>
    /// The hosting mode
    /// </summary>
    public enum HostMode
    {
        /// <summary>
        /// The back end mode, used for the server and content layer
        /// </summary>
        BACKEND = 1,
        /// <summary>
        /// Indicates the head of the application. If the mode is exactly equal to
        /// this, then the head is running on the same system (network) as the backend.
        /// </summary>
        HEAD = 2,
        /// <summary>
        /// Indicates that this instance is running remotely of the "real" backend
        /// </summary>
        REMOTE = 4,
        /// <summary>
        /// Indicates that this is a remote head, or a front-end that is not running
        /// on the same system (network interface) as the backend
        /// </summary>
        REMOTE_HEAD = 6
    }
}
