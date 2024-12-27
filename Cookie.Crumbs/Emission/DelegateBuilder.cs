using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

#if !BROWSER
namespace Cookie.Emission
{
    /// <summary>
    /// Provides methods for building delegates that map delegate to an arbitrary function.
    /// </summary>
    internal class DelegateBuilder
    {

        internal static void DWrite(string s)
        {
            Debug.WriteLine(s);
        }

        /// <summary>
        /// Generates a delegate of the declared type, which maps to the parameters of the provided method
        /// using: <see cref="ParameterMapping.GenerateSignatureMapping(Type[], Type[], bool)"/>
        /// 
        /// <para>For instance methods, <typeparamref name="DelegateTarget"/> must provide assignable instance as first parameter.</para>
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        internal static DelegateTarget CreateCallbackDelegate<ContainerType, DelegateTarget>(
            MethodInfo target,
            out BuilderContext<ContainerType, DelegateTarget>? context
        )
        where DelegateTarget : Delegate
        where ContainerType : class
        {
            // first, ensure input validity

            // Build the context for the creation
            context = new(target);
            //now get the il generator
            var dynamicMethod = context.CreateDynamicMethod();
            var il = dynamicMethod.GetILGenerator();

            // Instance methods require <this.> referencing
            if (!target.IsStatic)
            {
                // Find argument matching the container or otherwise an object
                int n = Array.IndexOf(context.EntryParams, typeof(ContainerType));
                if (n < 0) n = Array.IndexOf(context.EntryParams, typeof(object));
                if (n >= 0)
                {
                    il.Emit(OpCodes.Ldarg_S, n);
                    il.Emit(OpCodes.Castclass, context.Target.DeclaringType!);
                }
                else
                {
                    throw EmissionErrors.NoVirtualInstance.Get($"{typeof(DelegateTarget)}");
                }
            }

            foreach (var map in context.Mappings)
            {

                // If the parameter is unmapped, then we give it null or 0
                if (map.src < 0)
                {
                    EmitNullOrDefault(il, context.TargetParams[map.dst]);
                }
                else
                {
                    // It's mapped, so load it with appropriate casting
                    LoadParameter(il, context.EntryParams[map.src], context.TargetParams[map.dst], map.src);
                }
            }

            // whewwww now we can make the call
            il.EmitCall(OpCodes.Call, context.Target, null);
            EmitReturnType(il, context);

            // -- We have fully generated the IL for the dynamic method --

            // So create the delegate and send it back
            // The delegate itself is statically typed
            // And uses the initial parameter (if needed) to do the thing
            return (DelegateTarget)dynamicMethod.CreateDelegate(typeof(DelegateTarget));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="il"></param>
        /// <param name="context"></param>
        private static void EmitReturnType<M, T>(ILGenerator il, BuilderContext<M, T> context)
            where T : Delegate
            where M : class
        {
            // If we do not have a return type from the delegate
            // Then we need to clear the stack from any target calls
            if (context.EntryReturn == typeof(void))
            {
                if (context.TargetReturn != typeof(void))
                {
                    il.Emit(OpCodes.Pop); //bonk
                }
                il.Emit(OpCodes.Ret); //and return
                return;
            }

            // So we do have a return, we need to figure out what it is
            if (context.TargetReturn == typeof(void))
            {
                // Null is null
                il.Emit(OpCodes.Ldnull);            }
            else if (context.TargetReturn.IsValueType)
            {
                // value types, we can box into an object
                il.Emit(OpCodes.Box, context.TargetReturn);
            }
            else
            {
                // ref types, we can cast into objects
                il.Emit(OpCodes.Castclass, typeof(object));
            }
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Loads a parameter to the stack from the given index in the entry delegate, and matching it to the Target type
        /// based on the given EntryType.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetTypes"></param>
        /// <param name="srcIndex"></param>
        private static void LoadParameter(ILGenerator il, Type entryType, Type targetType, int srcIndex)
        {

            il.Emit(OpCodes.Ldarg_S, srcIndex); // load
            //DWrite($"Ldarg.S   {srcIndex}");

            // Handle ref types like In and Out
            if (targetType.IsByRef)
            {
                il.Emit(OpCodes.Ldind_Ref); // Dereference the argument (for ref/out parameters)
            }
            else if (targetType.IsValueType) // Handle structs or value types
            {
                HandleValueType(il, targetType, srcIndex);
            }
            else
            {
                // Only bother casting if the target isn't going to easily absorb the input
                // In theory, the only polymorphic types are model and and data
                // So we can just validate their typing elsewhere
                if (targetType != entryType && targetType != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, targetType);
                    //DWrite($"Castclass {targetType.Name}");
                }
            }
        }

        /// <summary>
        /// Handle the emission of value type parameters
        /// </summary>
        /// <param name="il"></param>
        /// <param name="paramType"></param>
        /// <param name="srcIndex"></param>
        private static void HandleValueType(ILGenerator il, Type paramType, int srcIndex)
        {
            var underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                HandleNullableValueType(il, underlyingType, srcIndex);
            }
            else
            {
                il.Emit(OpCodes.Unbox_Any, paramType);
            }
        }

        /// <summary>
        /// Handles the emission of nullable types. Should only be called for non-null underlying type of a Nullable{}
        /// </summary>
        /// <param name="il"></param>
        /// <param name="underlyingType"></param>
        /// <param name="srcIndex"></param>
        private static void HandleNullableValueType(ILGenerator il, Type underlyingType, int srcIndex)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
            var nullConstructor = nullableType.GetConstructor([underlyingType])!;

            if (!underlyingType.IsPrimitive)
            {
                // This case is simple
                il.Emit(OpCodes.Unbox_Any, nullableType);
                //DWrite($"Unbox.Any {nullableType.Name}");
            }
            else
            {
                var labelNotNull = il.DefineLabel();
                var labelDone = il.DefineLabel();
                var local = il.DeclareLocal(nullableType);

                // Check if we retrieved "null" from the data entry
                il.Emit(OpCodes.Brtrue_S, labelNotNull); //>---------┐
                /*                                                   |
                // This means the argument given was null            |
                *///so we need a zero                                |
                var code = GetZeroForPrimitive(underlyingType);//    |
                il.Emit(code);//                                     |
                il.Emit(OpCodes.Newobj, nullConstructor);//          |
                il.Emit(OpCodes.Stloc, local); //                    |
                //                                                   |
                //                                                   |
                // now jump to boxing                                |
                il.Emit(OpCodes.Br_S, labelDone); //>----------┐     |
                //                                             |     |
                // Non-null value                              |     |
                il.MarkLabel(labelNotNull);         // <-------)-----┘    
                il.Emit(OpCodes.Ldarg_S, srcIndex); //         |
                il.Emit(OpCodes.Unbox_Any, underlyingType); // |
                il.Emit(OpCodes.Stloc, local);//               |
                //                                             |
                //                                             |
                il.MarkLabel(labelDone); //<-------------------┘
                il.Emit(OpCodes.Ldloc, local);

            }
        }

        /// <summary>
        /// Gets an opcode for a zero value for a primitive type 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static OpCode GetZeroForPrimitive(Type type)
        {

            OpCode code = OpCodes.Ldc_I4_0; // Default for most types (int, bool, etc.)
            if (type == typeof(float))
                code = OpCodes.Ldc_R4; // Default value for float
            else if (type == typeof(double))
                code = OpCodes.Ldc_R8; // Default value for double
            return code;
        }


        /// <summary>
        /// Creates a null or zero/default OpCode for the given target type.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetType"></param>
        public static void EmitNullOrDefault(ILGenerator il, Type targetType)
        {
            //If this is a value type, then the logic becomes... annoying
            if (targetType.IsValueType)
            {
                // If it's nullable, then even more annoying
                var underlyingType = Nullable.GetUnderlyingType(targetType);

                // Primitives can simply emit a zero value and box it for nulls
                if (targetType.IsPrimitive)
                {
                    il.Emit(GetZeroForPrimitive(targetType));
                    if (underlyingType != null) il.Emit(OpCodes.Unbox_Any, underlyingType); // Emit the unboxing

                }
                else
                {
                    // In this case it's a struct, so we need to make a new copy
                    // Luckily structs have default constructors, so we can just call new() into a local
                    var local = il.DeclareLocal(targetType);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Initobj, targetType);
                    il.Emit(OpCodes.Ldloc, local);

                    // Nullable requires the struct to be boxed into a Nullable<T>
                    if (underlyingType != null)
                    {
                        // So we should get the constructor for Nullable<T> and instantiate it yay
                        var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
                        var nullConstructor = nullableType.GetConstructor([underlyingType])!;
                        il.Emit(OpCodes.Newobj, nullConstructor); // And make it

                    }
                }
            }
            else
            {
                // It's just a boring ref type so emit a nullref
                il.Emit(OpCodes.Ldnull);
            }
        }


    }
}
#endif