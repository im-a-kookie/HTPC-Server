using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Emission
{
    public class DelegateEmitter
    {
        public static Target GetMapping<Container, Target>(MethodInfo target) where Target : Delegate where Container : class
        {
#if BROWSER
            throw new Exception("Cannot perform Delegate remapping on web target!");
#else
            return DelegateBuilder.CreateCallbackDelegate<Container, Target>(target, out var _);
#endif
        }
    }
}
