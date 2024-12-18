#if !BROWSER
namespace Cookie.Emission
{
    /// <summary>
    /// Helper class that provides useful methods for investigating the parameters of methods.
    /// </summary>
    internal partial class ParameterMapping
    {

        /// <summary>
        /// Checks type assignability of the given types.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="target"></param>
        /// <returns>A boolean indicating the compabitility of the given types</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool CheckTypeCompabitility(Type entry, Type target)
        {
            EmissionErrors.NullMethodParameter.AssertNotNull(entry, "Delegate Entry");
            EmissionErrors.NullMethodParameter.AssertNotNull(target, "Delegate Target");

            return target.IsAssignableTo(entry) || entry.IsAssignableTo(target) || target == typeof(object);
        }

        /// <summary>
        /// Gets the signature of a delegate from the given type
        /// </summary>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static (Type returnType, Type[] parameterTypes, string[] names) GetDelegateSignature(Type delegateType)
        {

            EmissionErrors.EntryNotDelegate.Assert(!typeof(Delegate).IsAssignableFrom(delegateType));

            // Get the Invoke method of the delegate
            var invokeMethod = delegateType.GetMethod("Invoke");

            EmissionErrors.NullMethodGroup.AssertNotNull(invokeMethod);

            // Get the return type
            var returnType = invokeMethod.ReturnType;

            // Get the parameter types
            var pars = invokeMethod.GetParameters();

            var parameterTypes = pars
                .Select(p => p.ParameterType)
                .ToArray();

            var parameterNames = pars
                .Select(p => p.Name ?? "")
                .ToArray();


            return (returnType, parameterTypes, parameterNames);
        }


        /// <summary>
        /// Gets the signature of a delegate from the given delegate
        /// </summary>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static (Type returnType, Type[] parameterTypes, string[] names) GetDelegateSignature(Delegate delegateType)
        {
            return GetDelegateSignature(delegateType.GetType());
        }

        /// <summary>
        /// The return type of the delegate
        /// </summary>
        private static Type? _returnType;

        /// <summary>
        /// The input parameters of the delegate
        /// </summary>
        private static List<Type>? _cachedTypes;


        /// <summary>
        /// Generates a signature mapping between the given entry and target types, returning a list of mappings,
        /// ordered by their target parameter index, for DynamicInvoke and delegate construction
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="target"></param>
        /// <param name="allowTargetHigherSpecificity"></param>
        /// <returns></returns>
        internal static List<Mapping> GenerateSignatureMapping(Type[] entry, Type[] target, bool allowTargetHigherSpecificity = true)
        {
            MappingContext context = new(entry, target);
            context.ReversibleAssignability = allowTargetHigherSpecificity;

            // First, map like-like types
            PerformTargetEntry(context, MapDirectlyAssignable);
            // And now map wildcard types
            PerformTargetEntry(context, MapWildcards);

            // Now done, compute the result
            return context.ComputeSortedMapping(true);
        }


        /// <summary>
        /// Simple delegate definition for mapping predication
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <param name="e"></param>
        /// <param name="ti"></param>
        /// <param name="ei"></param>
        private delegate void MappingDelegate(MappingContext context, Type t, Type e, int ti, int ei);

        /// <summary>
        /// Wrapper callback for providing delegates that loop though every entry, for every target, in this order (foreach target -> foreach entry).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="predicate"></param>
        private static void PerformTargetEntry(MappingContext context, MappingDelegate predicate)
        {
            // For each parameter, try to fill it from entry params
            for (int ti = 0; ti < context.TargetParameters.Length; ++ti)
            {
                // Get and ensure availability of target
                if (context.SolvedTargets[ti]) continue;
                var t = context.TargetParameters[ti];
                for (int ei = 0; ei < context.EntryParameters.Length; ++ei)
                {
                    // Get and ensure availability of entry
                    if (context.SolvedEntries[ei]) continue;
                    var e = context.EntryParameters[ei];
                    predicate(context, t, e, ti, ei);

                    if (context.SolvedTargets[ti]) break;
                }
            }
        }

        /// <summary>
        /// Internal logic for determining if two types (target and entry) are directly assignable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <param name="e"></param>
        /// <param name="ti"></param>
        /// <param name="ei"></param>
        internal static void MapDirectlyAssignable(MappingContext context, Type t, Type e, int ti, int ei)
        {
            if (t == typeof(object))
            {
                // Objects are mapped aggressively
                if (e == typeof(object)) context.Map(ti, ei);
                // Target "object" is otherwise reserved for wildcarding
            }
            else
            {
                // Map if e is a subtype of t (always valid)
                if (e.IsAssignableTo(t))
                    context.Map(ti, ei);
                // Or if permitted; t is a subtype of e (implementation specific)
                else if (context.ReversibleAssignability && e != typeof(object) && t.IsAssignableTo(e))
                    context.Map(ti, ei);
            }
        }

        /// <summary>
        /// Delegate callback for mapping parameters to wildcards (aka anything mapped into object?)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <param name="e"></param>
        /// <param name="ti"></param>
        /// <param name="ei"></param>
        internal static void MapWildcards(MappingContext context, Type t, Type e, int ti, int ei)
        {
            if (t == typeof(object))
            {
                context.Map(ti, ei);
            }
        }


        /// <summary>
        /// Fills unmapped output parameters with -1 indicating null/0
        /// </summary>
        /// <param name="context"></param>
        internal static void FillUnmappedOutputs(MappingContext context)
        {
            for (int o = 0; o < context.TargetParameters.Length; o++)
            {
                if (!context.SolvedTargets[o])
                {
                    // The output parameter was not mapped, so a null/default will be calculated
                    // We can find the correct nullref/0 parameter during IL gen
                    context.Map(o, -1);
                }
            }
        }



    }
}
#endif