
/* Unmerged change from project 'Cookie.Crumbs (net9.0-browser)'
Before:
using Cookie.Utils;
using Cookie.Logging;
using System.Reflection;
After:
using Cookie.Logging;
using Cookie.Utils;
using System.Reflection;
*/
using Cookie.Logging;
using Cookie.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;


#if !BROWSER
namespace Cookie.Emission
{

    internal class BuilderContext<ContainerType, TargetDelegate> where TargetDelegate : Delegate where ContainerType : class
    {
        /// <summary>
        /// Whether the target method is static
        /// </summary>
        internal bool IsStatic { get; private set; }

        ///// <summary>
        ///// The calling object, or null (expects null if IsStatic)
        ///// </summary>
        //public Model? Caller { get; private set; }

        /// <summary>
        /// The return type of the entry delegate
        /// </summary>
        internal Type EntryReturn { get; private set; }

        /// <summary>
        /// The return type of the target method
        /// </summary>
        internal Type TargetReturn { get; private set; }

        /// <summary>
        /// The parameter types of the entry delegate
        /// </summary>
        internal Type[] EntryParams { get; private set; }

        /// <summary>
        /// The parameter types of the target method
        /// </summary>
        internal Type[] TargetParams { get; private set; }

        internal string[] EntryNames { get; private set; }

        internal string[] TargetNames { get; private set; }

        /// <summary>
        /// The parameter mappings between entry and target parameters
        /// </summary>
        internal List<Mapping> Mappings { get; private set; }

        /// <summary>
        /// The target being invoked
        /// </summary>
        internal MethodInfo Target;

        internal BuilderContext(MethodInfo target)
        {

            this.Target = target;

            EmissionErrors.IncorrectTargetType.Assert(
                 target.DeclaringType == null,
                "Method: " + target.ToString());

            EmissionErrors.IncorrectTargetType.Assert(
                !target.DeclaringType!.IsAssignableTo(typeof(ContainerType)),
                "Expected type: " + typeof(ContainerType));


            this.IsStatic = target.IsStatic;
            this.Target = target;

            // Extract the information that we need
            var entrySignature = ParameterMapping.GetDelegateSignature(typeof(TargetDelegate));

            EntryParams = entrySignature.parameterTypes;
            EntryReturn = entrySignature.returnType;
            EntryNames = entrySignature.names;

            // And the target information
            TargetParams = target.GetParameters().Select(x => x.ParameterType).ToArray()!;
            TargetNames = target.GetParameters().Select(x => x.Name).ToArray()!;
            TargetReturn = target.ReturnType;

            // Generate the mappings
            Mappings = ParameterMapping.GenerateSignatureMapping(EntryParams, TargetParams);
            // Abide mappings by parameter name similarities
            ApplyNamePrioritization(target);

            // Do a quick validation
            ValidateStaticTargetParams();

        }

        /// <summary>
        /// Applies name prioritization rules to the parameters in this context, such that some vague
        /// allowance exists for like types (e.g query will typically match to request_query over "json").
        /// </summary>
        /// <param name="target"></param>
        internal void ApplyNamePrioritization(MethodInfo target)
        {

            // get every parameter from input and output that has matching type
            var remainEntry = EntryParams.Select((_, index) => index).ToHashSet();
            var remainTarget = TargetParams.Select((_, index) => index).ToHashSet();

            while (remainEntry.Count > 0)
            {
                var n = remainEntry.First();

                var entryMatch = EntryParams
                    .Select((_, index) => index)
                    .Where(x => EntryParams[x] == EntryParams[n]).ToArray();

                var targetMatch = TargetParams
                    .Select((_, index) => index)
                    .Where(x => TargetParams[x] == EntryParams[n]).ToArray();

                // now let's calculate the best pairs
                if (entryMatch.Length > 1 && targetMatch.Length > 0)
                {

                    // Calculate the likeness of every available pairing
                    List<(int a, int b, double sim)> pairs = [];
                    for (int i = 0; i < entryMatch.Length; i++)
                    {
                        int aindex = entryMatch[i];
                        for (int j = 0; j < targetMatch.Length; j++)
                        {
                            int bindex = targetMatch[j];
                            // now get the likeness
                            pairs.Add((aindex, bindex,
                                JaroWinklerDistance.Distance(
                                    EntryNames[aindex],
                                    TargetNames[bindex])));

                        }
                    }

                    // now sort from shortest to longest distance
                    pairs.Sort((a, b) => a.sim.CompareTo(b.sim));

                    // and take the best pairs
                    for (int i = 0; i < pairs.Count; i++)
                    {
                        int a = pairs[i].a;
                        int b = pairs[i].b;

                        // Ensure that we only map parameters that are not mapped yet
                        if (remainEntry.Remove(a) && remainTarget.Remove(b))
                        {
                            for (int j = 0; j < Mappings.Count; j++)
                            {
                                if (Mappings[j].dst == b)
                                {
                                    Mappings[j] = new(a, Mappings[j].dst);
                                }
                            }
                        }
                    }
                }

                // Now clear these entries from the remainders
                foreach (var e in entryMatch) remainEntry.Remove(e);
                foreach (var t in targetMatch) remainTarget.Remove(t);
            }
        }


        /// <summary>
        /// Gets a qualified target name for error/debugging reasons
        /// </summary>
        /// <returns></returns>
        internal string GetQualifiedTargetName()
        {
            StringBuilder sb = new (100);
            sb.Append($"{Target.DeclaringType?.Name ?? "<?>"}.{Target.Name}");
            sb.Append("  ");
            sb.Append($"[{TargetReturn.Name}] ({String.Join(", ", TargetParams.Select(x => x.Name))})");
            return sb.ToString();
        }

        /// <summary>
        ///  Validates static target signatures for total provided information.
        /// </summary>
        private void ValidateStaticTargetParams()
        {
            // virtual methods already provide the model inherently as (this),
            // so the validation here is only relevant to static methods
            if (!IsStatic) return;

            // The model and signal can both refer to the model, so either is adequate
            int indexModel = Array.IndexOf(EntryParams, typeof(ContainerType));

            // The target may not type them, so we should just see if they were mapped
            // (remember as per implementation, the data fills the first object parameter)
            bool mapsEither = false;
            foreach (var map in Mappings)
            {
                if (map.src == indexModel)
                {
                    mapsEither = true;
                    break;
                }
            }

            // We did not find a mapping for either important parameter
            if (!mapsEither)
            {
                // Rather than crashing, we should just give a warning, since this may be intended behaviour
                Messages.StaticMethodNoInstance.Warn($"Target Caller: {GetQualifiedTargetName()}");
            }
        }


        /// <summary>
        /// Creates a stub dynamic method with parameters and returns set according to the
        /// provided caller and target method.
        /// </summary>
        /// <returns>An empty <see cref="DynamicMethod"/> with parameters configured according to this context.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal DynamicMethod CreateDynamicMethod()
        {
            try
            {
                return new DynamicMethod(
                    name: $"Callback_{Target.Name}",
                    returnType: EntryReturn,
                    parameterTypes: EntryParams,
                    m: Target.DeclaringType!.Module,
                    skipVisibility: true
                    );
            }
            catch (Exception e)
            {
                throw EmissionErrors.DynamicMethodCreationFailed.Get(innerException: e);
            }

        }



    }
}
#endif