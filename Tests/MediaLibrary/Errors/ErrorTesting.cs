using Cookie.Logging;
using Cookie.Utils.Exceptions;

namespace Tests.MediaLibrary.Errors
{
    [TestClass]
    public class ErrorTesting
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullException()
        {
            throw GenericErrors.NullArgument.Get("this is a test", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ResourceNotFoundException))]
        public void TestNoResource()
        {
            throw GenericErrors.MissingResource.Get("this is a test", null);
        }

    }
}
