using Cookie.Addressing;

namespace Cookie.Logging
{
    /// <summary>
    /// A simple warning binding
    /// </summary>
    public class Message
    {
        /// <summary>
        /// A flag indicating whether this warning is enabled
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// The identifier for this warning
        /// </summary>
        public Address<long> Identifier { get; private set; }
        /// <summary>
        /// The name of this warning
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The message for this warning
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// Gets the code for this warning
        /// </summary>
        public string Code => Identifier.Text;

        /// <summary>
        /// Creates a new warning with the given name and message
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Message"></param>
        public Message(string Name, string Message)
        {
            this.Name = Name;
            this.Body = Message;
            Identifier = MessageHelper.ProvideAddress(this);
            MessageHelper.Register(this);
        }

        public void Warn(string? message = null)
        {
            Logger.Warn(this, message);
        }

        public void Debug(string? message = null)
        {
            Logger.Debug(this, message);
        }

        /// <summary>
        /// Attempts to inject a warning into the given string, where the warning is administered
        /// by the given name. If the warning is disabled, then a null return is provided.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string? Inject(string? text)
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

            if (Enabled) return null;

            if (text.Length > m + 1)
                return $"{ToString()}. {text.Substring(m + 1)}".TrimEnd();
            else return $"{ToString()}.";

        }


        public override string ToString()
        {
            if (Body != null)
                return $"#{Identifier.Text} {Body}";
            else return $"#{Identifier.Text} {Name}";
        }

        public override bool Equals(object? obj)
        {
            return Identifier?.Equals(obj) ?? false;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

    }
}
