using Cookie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Emission
{
    public static class EmissionErrors
    {

        /// <summary>
        /// Thrown if a target method does not have a declaring type
        /// </summary>
        public static Error IncorrectTargetType = new("No Declaring Type", "The target method must have a declaring type", (m, e) => new NotSupportedException(m, e));

        public static Error InvalidDeclarer = new("Invalid Declaring Type", "The target type association must declare the target method", (m, e) => new ArgumentException(m, e));

        public static Error NullMethodParameter = new("Null Parameter Type", "Parameter types cannot be null", (m, e) => new ArgumentNullException(m, e));

        public static Error NullMethodGroup = new("Null Method Group", "Provided delegate or method group cannot be null", (m, e) => new ArgumentNullException(m, e));

        public static Error EntryNotDelegate = new("Non Delegate Type", "Entry method must be a Delegate type", (m, e) => new ArgumentNullException(m, e));

        public static Error MappingInvalidIndex = new("Invalid Mapping Index", "The mapping index is out of range", (m, e) => new ArgumentOutOfRangeException(m, e));

        public static Error TargetAlreadyMapped = new("Target Already Mapped", "The target parameter has already been mapped", (m, e) => new ArgumentException(m, e));

        public static Error UnmappedTargets = new("Unmapped Targets", "The mapping does not map all targets", (m, e) => new InvalidOperationException(m, e));

    }
}
