using Cookie.Cryptography;
using Cookie.Serializers;
using System.Text;

namespace Cookie.Connections.API.Logins
{
    public class User : IDictable
    {
        /// <summary>
        /// The read/write permission of the user. By default, a non-user is assumed to have low permissions,
        /// and a standard user has Medium permissions.
        /// </summary>
        public PermissionLevel Permission { get; set; } = new(Level.MED);
        /// <summary>
        /// The name of the current user
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// The hash of the user's password
        /// </summary>
        public string UserHash { get; set; } = "";
        /// <summary>
        /// The number of consecutive incorrect login attemps
        /// </summary>
        public int Incorrectness { get; set; }
        /// <summary>
        /// A Date Time until which point the user is locked out
        /// </summary>
        public DateTime Lockout { get; set; } = DateTime.MinValue;

        private static DateTime BaseTime = new(2020, 1, 1);

        /// <summary>
        ///  Validates a token from the given stringified token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static (string name, string hash)? ReadToken(string token)
        {
            // get and decrypt the token
            var b = Convert.FromBase64String(token);
            b = CryptoHelper.Decrypt(b);
            using var ms = new MemoryStream(b);
            using var sr = new BinaryReader(ms);

            // get the parts out of it
            var name = sr.ReadString();
            var hash = sr.ReadString();
            int time = sr.ReadInt32();

            DateTime dtn = DateTime.UtcNow;

            var dte = BaseTime.AddMinutes(time);

            if (dte >= dtn) return (name, hash);
            return null;
        }



        /// <summary>
        /// Generates a bas64 API token for this user that expires after the given duration.
        /// </summary>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public string GenerateToken(TimeSpan expiry)
        {
            // Let's just write the details to a stream and encrypt it
            using var ms = new MemoryStream();
            using var tw = new BinaryWriter(ms);
            tw.Write(UserName);
            tw.Write(UserHash);
            // Get the minute of the expiration, rounded up
            var dtn = DateTime.UtcNow + expiry;
            tw.Write((int)Math.Ceiling((dtn - BaseTime).TotalMinutes));
            // now pack it into a short token
            return Convert.ToBase64String(CryptoHelper.Encrypt(ms.ToArray()));
        }

        /// <summary>
        /// provides this user as a dictionary
        /// </summary>
        /// <param name="dict"></param>
        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["un"] = UserName;
            dict["uh"] = UserHash;
            dict["in"] = Incorrectness;
            dict["d"] = Lockout.Ticks;
            dict["r"] = Permission.ToInt();
        }

        /// <summary>
        /// Retrieves this user from a dictionary
        /// </summary>
        /// <param name="dict"></param>
        public void FromDictionary(IDictionary<string, object> dict)
        {
            UserName = (string)dict["un"];
            UserHash = (string)dict["uh"];
            Incorrectness = (int)dict["in"];
            Lockout = DateTime.FromBinary((long)dict["d"]);
            Permission = new((int)dict["r"]);

        }

    }
}
