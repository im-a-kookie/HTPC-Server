using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Logging
{
    /// <summary>
    /// Denotes that the marked class contains logging attributes. Allows these attributes to be loaded
    /// into the runtime consistently with their declaration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class LoggingDefinitions : Attribute
    {
    }
}
