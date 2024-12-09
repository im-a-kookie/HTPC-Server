using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cookie.Utils.MessageHelper;

namespace Cookie.Utils
{
    public static class Messages
    {

        public static Message InsufficientAddressLength = new Message("Insufficient Address", "The size of the address is too small");
        public static Message MultipleLocalHead = new Message("Multiple Local Heads", "There should be only one local head instance");
        public static Message StaticMethodNoInstance = new Message("Static Method Instanceless", "The static method does not provide instance data");


    }
}
