﻿using Cookie.Addressing;
using Cookie.Cryptography;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cookie.Logging
{
    public class Error
    {
        public delegate Exception ExceptionBuilder(string? message, Exception? inner = null);

        public delegate bool Assertion();

        public Address<int> Id;

        public Message InnerMessage;

        public ExceptionBuilder Generator;

        public string? MessagePrepend = null;

        public Exception Exception => Get(null, null);

        public Error(Message message, ExceptionBuilder inner, string? prepend = null)
        {
            this.InnerMessage = message;
            this.Generator = inner;
            this.MessagePrepend = prepend;
            Id = new(CryptoHelper.GetStringHash(message + prepend));
        }

        public Error(string error, string? message, ExceptionBuilder generator, string? prepend = null) : this(new Message(error, message ?? ""), generator, prepend) { }


        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public void Assert(bool assertion, string? details = null)
        {
            if (assertion)
            {
                Throw(details, null);
            }
        }

        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public void AssertNotNull<T>([NotNull] T? test, string? details = null) where T : notnull
        {
            if (test == null)
            {
                throw Get(details);
            }
        }


        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public void Assert(Func<bool> assertion, string? details = null)
        {
            if (assertion())
            {
                throw Get(details);
            }
        }

        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public bool IsTrue(bool assertion, out Exception? e, string? details = null)
        {
            if (assertion)
            {
                e = Get(details, null);
                return true;
            }
            e = null;
            return false;
        }

        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public bool IsNull<T>([NotNullWhen(false)] T? test, out Exception? e, string? details = null)
        {
            if (test == null)
            {
                e = Get(details, null);
                return true;
            }
            e = null;
            return false;
        }


        /// <summary>
        /// Asserts the error using the given assertion function
        /// </summary
        /// <param name="details"></param>
        public bool IsTrue(Func<bool> assertion, out Exception? e, string? details = null)
        {
            if (assertion())
            {
                e = Get(details, null);
                return true;
            }
            e = null;
            return false;
        }


        /// <summary>
        /// Throws this error as a new exception
        /// </summary>
        /// <param name="details"></param>
        /// <param name="innerException"></param>
        public Exception Get(string? details = null, Exception? innerException = null)
        {
            Logger.Fatal(this, details ?? String.Empty);

            // Generate the error message
            StringBuilder sb = new();
            sb.Append("Error: " + InnerMessage.ToString());
            if (details == null) sb.Append('.');
            else
            {
                sb.Append(". ");
                if (MessagePrepend != null) sb.Append(MessagePrepend);
                sb.Append(details);
            }
            return Generator(sb.ToString(), innerException);
        }


        /// <summary>
        /// Throws this error as a new exception
        /// </summary>
        /// <param name="details"></param>
        /// <param name="innerException"></param>
        public void Throw(string? details = null, Exception? innerException = null)
        {
            Logger.Fatal(this, details ?? String.Empty);
            var e = Get(details, innerException);
            throw e;
        }




    }
}
