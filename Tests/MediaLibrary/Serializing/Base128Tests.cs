using Cookie.Serializers;
using System.Text;

namespace Tests.MediaLibrary.Serializing
{

    [TestClass]
    public class Base128Tests
    {

        public static readonly string TestString = @"abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""
§ $%& /() =?* '<> #|; ²³~ @`´ ©«» ¤¼× {} abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?*
'<> #|; ²³~ @`´ ©«» ¤¼× {} abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?* '<> #|; ²³~ @`
´ ©«» ¤¼× {} abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?* '<> #|; ²³~ @`´ ©«» ¤¼× {} 
abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?* '<> #|; ²³~ @`´ ©«» ¤¼× {} abc def ghi jkl
mno pqrs tuv wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?* '<> #|; ²³~ @`´ ©«» ¤¼× {} abc def ghi jkl mno pqrs tuv
wxyz ABC DEF GHI JKL MNO PQRS TUV WXYZ !""§ $%& /() =?* '<> #|; ²³~ @`´ ©«» ¤¼× {}abc def ghi jkl mno pqrs tuv wxyz ABC DEF GHI";

        [TestMethod]
        public void Base128ConvertsRoundTrip()
        {
            // Arrange
            string input = TestString;
            string base128 = Base128.ToBase128(Encoding.UTF8.GetBytes(input));
            string result = Encoding.UTF8.GetString(Base128.FromBase128(base128));

            // ensure they match
            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void ExtensionMethods()
        {
            // Arrange
            string input = TestString;
            string base128 = Encoding.UTF8.GetBytes(input).ToBase128();
            string result = Encoding.UTF8.GetString(base128.ToBytesBase128());

            // ensure they match
            Assert.AreEqual(input, result);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ToBase128_NullInputThrowsArgumentNullException()
        {
            string result = Base128.ToBase128((byte[])null);
        }

        [TestMethod]
        public void FromBase128_InvalidCharacterThrowsException()
        {
            // Arrange
            string input = "Invalid@String";

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => Base128.FromBase128(input));
        }


    }
}


