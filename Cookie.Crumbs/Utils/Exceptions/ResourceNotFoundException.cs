using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Utils.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    }
}
