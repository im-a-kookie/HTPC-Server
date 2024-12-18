using System.Reflection;

namespace Cookie.Emission
{
    public class DelegateEmitter
    {
#if !BROWSER
        public static Target GetMapping<Container, Target>(MethodInfo target) where Target : Delegate where Container : class
        {


            return DelegateBuilder.CreateCallbackDelegate<Container, Target>(target, out var _);

        }
#endif
    }
}
