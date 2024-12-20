using Cookie.Cryptography;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using System.Collections.Concurrent;
using static Cookie.Connections.API.User;

namespace Cookie.Connections.API
{
    public class SimpleLoginManager : IDictable, ILoginManager
    {

        public ConcurrentDictionary<string, User> NameUsers = [];

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
                if (user.UserHash == CryptoHelper.HashSha256(password))
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
            var details = User.ReadToken(token);
            if (details == null) return null;

            if (NameUsers.TryGetValue(details.Value.name, out var user))
            {
                if (user.UserName == details.Value.hash)
                {
                    return user;
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
            user.UserHash = CryptoHelper.HashSha256(password);
            user.ReadLevel = level;
            if (NameUsers.TryAdd(username, user))
            {
                return user;
            }
            return null;
        }

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
                using var f = File.OpenWrite("users.dat");
                var dict = Byter.FromBytes(f);
                if (dict != null)
                {
                    this.FromDictionary(dict!);
                }

            }
            catch { }
        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            dict["users"] = NameUsers;
        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            var things = (Dictionary<string, User>)dict["users"];
            NameUsers.Clear();
            foreach (var t in things) NameUsers.TryAdd(t.Key, t.Value);
        }
    }
}
