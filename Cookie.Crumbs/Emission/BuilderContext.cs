using Cookie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Emission
{

    public class BuilderContext<ContainerType, TargetDelegate> where TargetDelegate : Delegate where ContainerType : class
    {
        /// <summary>
        /// Whether the target method is static
        /// </summary>
        public bool IsStatic { get; private set; }

        ///// <summary>
        ///// The calling object, or null (expects null if IsStatic)
        ///// </summary>
        //public Model? Caller { get; private set; }

        /// <summary>
        /// The return type of the entry delegate
        /// </summary>
        public Type EntryReturn { get; private set; }

        /// <summary>
        /// The return type of the target method
        /// </summary>
        public Type TargetReturn { get; private set; }

        /// <summary>
        /// The parameter types of the entry delegate
        /// </summary>
        public Type[] EntryParams { get; private set; }

        /// <summary>
        /// The parameter types of the target method
        /// </summary>
        public Type[] TargetParams { get; private set; }

        /// <summary>
        /// The parameter mappings between entry and target parameters
        /// </summary>
        public List<Mapping> Mappings { get; private set; }

        /// <summary>
        /// The target being invoked
        /// </summary>
        public MethodInfo Target;

        public BuilderContext(MethodInfo target)
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

            // And the target information
            TargetParams = target.GetParameters().Select(x => x.ParameterType).ToArray()!;
            TargetReturn = target.ReturnType;

            // Generate the mappings
            Mappings = ParameterMapping.GenerateSignatureMapping(EntryParams, TargetParams);

            // Do a quick validation

            ValidateStaticTargetParams();

        }

        /// <summary>
        /// Gets a qualified target name for error/debugging reasons
        /// </summary>
        /// <returns></returns>
        public string GetQualifiedTargetName()
        {
            StringBuilder sb = new StringBuilder(100);
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
        public DynamicMethod CreateDynamicMethod()
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
