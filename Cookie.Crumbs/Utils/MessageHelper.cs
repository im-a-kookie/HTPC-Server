using Cookie.Addressing;

namespace Cookie.Utils
{
    public class MessageHelper
    {
        /// <summary>
        /// A unique address provider for the warning instance
        /// </summary>
        internal static AddressProvider<short> AddressProvider = new AddressProvider<short>(false);

        /// <summary>
        /// A mapping of addresses to the underlying warnings
        /// </summary>
        internal static Dictionary<Address<short>, Message> AddressWarning = [];

        /// <summary>
        /// A mapping of string codes/names to warning objects
        /// </summary>
        internal static Dictionary<string, Message> NameWarning = [];

        internal static Dictionary<string, bool> DisabledByKey = [];


        static MessageHelper()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("--w:"))
                {
                    DisabledByKey.TryAdd(arg.Substring(4), false);
                }
            }
        }

        /// <summary>
        /// Registers this warning
        /// </summary>
        /// <param name="w"></param>
        public static void Register(Message w)
        {
            MessageHelper.AddressWarning.Add(w.Identifier, w);
            MessageHelper.NameWarning.Add(w.Identifier.Text.ToLower(), w);
            MessageHelper.NameWarning.Add(w.Name.ToLower(), w);

            bool flag = true;
            if (DisabledByKey.TryGetValue(w.Name, out flag) || DisabledByKey.TryGetValue(w.Code, out flag))
            {
                w.Enabled = flag;
            }
        }


        /// <summary>
        /// Enables or disables a given warning
        /// </summary>
        /// <param name="warning"></param>
        /// <param name="enabled"></param>
        public void Flag(string warning, bool enabled)
        {
            if (NameWarning.TryGetValue(warning, out Message? warningValue))
            {
                warningValue.Enabled = false;
            }
        }

        /// <summary>
        /// Gets the message to apply to this text object
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Message? Get(string? text)
        {
            if (text == null) return null;

            // First, let's try to get the warning
            int n = text.IndexOf("#");
            if (n != 0) return null;

            int m = text.IndexOf(":", n);
            if (m < 0) return null;

            //Get the warning body
            string? warn = text.Substring(n + 1, m - n - 1);

            // Get the first component, if there is one
            warn = text.Split(' ').FirstOrDefault();
            if (string.IsNullOrWhiteSpace(warn)) return null;

            // Now do a lookup and replace if possible
            if (NameWarning.TryGetValue(warn.ToLower(), out var warning))
            {
                return warning;
            }
            return null;

        }


        /// <summary>
        /// Attempts to inject a warning into the given string, where the warning is administered
        /// by the given name. If the warning is disabled, then a null return is provided.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string? Inject(string? text)
        {
            if (text == null) return null;

            // First, let's try to get the warning
            int n = text.IndexOf("#");
            if (n != 0) return text;

            int m = text.IndexOf(":", n);
            if (m < 0) return text;

            //Get the warning body
            string? warn = text.Substring(n + 1, m - n - 1);

            // Get the first component, if there is one
            warn = text.Split(' ').FirstOrDefault();
            if (string.IsNullOrWhiteSpace(warn)) return text;

            // Now do a lookup and replace if possible
            if (NameWarning.TryGetValue(warn.ToLower(), out var warning))
            {
                if (!warning.Enabled) return null;


                if (text.Length > m + 1)
                    return $"{warning.ToString()}: {text.Substring(m + 1)}";
                else return $"{warning.ToString()}.";

            }
            return text;
        }





    }
}
