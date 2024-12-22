using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using System.Collections.Concurrent;
using static Cookie.Connections.API.Logins.User;

namespace Cookie.Connections.API.Logins
{
    public class SimpleLoginManager<T> : IDictable, ILoginManager<T>
    {

        public ConcurrentDictionary<string, User> NameUsers = [];

        public Controller<T>? Controller { get; set; }

        public SimpleLoginManager(Controller<T>? controller)
        {
            Controller = controller;
        }

        /// <summary>
        /// Gets the controller associated with this login manager
        /// </summary>
        /// <returns></returns>
        public Controller<T>? GetController()
        {
            return Controller;
        }


        /// <summary>
        /// Gets a user from the given username and password hash
        /// </summary>
        /// <param name="username"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public User? GetUser(string username, string password)
        {
            if (NameUsers.TryGetValue(username, out var user))
            {
                if (user.UserHash == CryptoHelper.HashSha1(password, 16))
                {
                    return user;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a user from the given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public User? GetUser(string token)
        {
            var details = ReadToken(token);
            if (details == null) return null;

            if (NameUsers.TryGetValue(details.Value.name, out var user))
            {
                // don't allow users to log in after multiple failed attempts
                if (user.Incorrectness >= 3)
                {
                    if (DateTime.UtcNow < user.Delay)
                        return null;
                }

                // ensure the hash is correct
                if (user.UserHash == details.Value.hash)
                {
                    user.Incorrectness = 0;
                    return user;
                }
                else
                {
                    // flag the incorrectness and continue
                    ++user.Incorrectness;
                    if (user.Incorrectness >= 3)
                    {
                        user.Delay = DateTime.UtcNow.AddSeconds(30);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a user with the given username and hash
        /// </summary>
        /// <param name="username"></param>
        /// <param name="hash"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public User? CreateUser(string username, string password, PermissionLevel level)
        {
            User user = new User();
            user.UserName = username;
            // ensure that passwords are hashed on their way into the user lookup
            user.UserHash = CryptoHelper.HashSha1(password, 16);
            user.Permission = level;

            if (NameUsers.TryAdd(username, user))
            {
                return user;
            }
            return null;
        }

        public int GetUserCount() => NameUsers.Count;

        /// <summary>
        /// Saves this login manager to the disk locally
        /// </summary>
        public virtual void Save()
        {
            try
            {
                File.Delete("users.dat");
            }
            catch { }

            try
            {
                using var f = File.OpenWrite("users.dat");
                Byter.ToBytes(f, ((IDictable)this).MakeDictionary());
            }
            catch { }

        }

        /// <summary>
        /// Loads this login manager from the disk locally
        /// </summary>
        public virtual void Load()
        {
            try
            {
                using var f = File.OpenRead("users.dat");
                var dict = Byter.FromBytes(f);
                if (dict != null)
                {
                    FromDictionary(dict!);
                }

            }
            catch { }
        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            var things = (Dictionary<string, User>)dict["users"];
            NameUsers.Clear();
            foreach (var t in things) NameUsers.TryAdd(t.Key, t.Value);
        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["users"] = NameUsers;
        }

    }
}
