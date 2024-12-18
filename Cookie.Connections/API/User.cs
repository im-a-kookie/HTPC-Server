using Cookie.Serializers;

namespace Cookie.Connections.API
{
    public class User : IDictable
    {
        private static DateTime BaseTime = new DateTime(2020, 1, 1);

        public enum PermissionLevel
        {
            LOW,
            MEDIUM,
            HIGH
        }

        public PermissionLevel ReadLevel { get; set; } = PermissionLevel.LOW;
        public PermissionLevel WriteLevel { get; set; } = PermissionLevel.LOW;

        public string UserName { get; set; } = "";

        public string UserHash { get; set; } = "";

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
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        ///  Validates a token from the given string
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static (string name, string hash)? ReadToken(string token)
        {
            var b = Convert.FromBase64String(token);
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
            dict["r"] = (int)ReadLevel;
            dict["w"] = (int)WriteLevel;
        }

        public void FromDictionary(IDictionary<string, object> dict)
        {
            UserName = (string)dict["un"];
            UserHash = (string)dict["uh"];
            ReadLevel = (PermissionLevel)dict["r"];
            WriteLevel = (PermissionLevel)dict["w"];

        }




    }
}
