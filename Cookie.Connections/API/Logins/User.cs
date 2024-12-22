using Cookie.Cryptography;
using Cookie.Serializers;
using System.Text;

namespace Cookie.Connections.API.Logins
{
    public class User : IDictable
    {
        private static DateTime BaseTime = new DateTime(2020, 1, 1);

        public PermissionLevel Permission { get; set; } = new(Level.MED);

        public string UserName { get; set; } = "";

        public string UserHash { get; set; } = "";

        public int Incorrectness = 0;

        public DateTime Delay = DateTime.MinValue;


        /// <summary>
        /// Generates a bas64 API token for this user
        /// </summary>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public string GenerateToken(TimeSpan expiry)
        {
            using var ms = new MemoryStream();
            using var tw = new BinaryWriter(ms);
            tw.Write(UserName);
            tw.Write(UserHash);
            var dtn = DateTime.UtcNow + expiry;
            tw.Write((int)Math.Ceiling((dtn - BaseTime).TotalMinutes));
            return Convert.ToBase64String(CryptoHelper.Encrypt(ms.ToArray()));
        }

        /// <summary>
        ///  Validates a token from the given string
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static (string name, string hash)? ReadToken(string token)
        {
            var b = Convert.FromBase64String(token);
            b = CryptoHelper.Decrypt(b);
            using var ms = new MemoryStream(b);
            using var sr = new BinaryReader(ms);

            var name = sr.ReadString();
            var hash = sr.ReadString();
            int time = sr.ReadInt32();

            DateTime dtn = DateTime.UtcNow;

            var dte = BaseTime.AddMinutes(time);

            if (dte >= dtn) return (name, hash);
            return null;
        }

        public void ToDictionary(IDictionary<string, object> dict)
        {
            dict["un"] = UserName;
            dict["uh"] = UserHash;
            dict["in"] = Incorrectness;
            dict["d"] = Delay.Ticks;
            dict["r"] = Permission.ToInt();
        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            UserName = (string)dict["un"];
            UserHash = (string)dict["uh"];
            Incorrectness = (int)dict["in"];
            Delay = DateTime.FromBinary((long)dict["d"]);
            Permission = new((int)dict["r"]);

        }




    }
}
