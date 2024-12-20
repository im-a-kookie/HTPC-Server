using Cookie.ContentLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MediaLibrary.ContetnLibraryContainers
{
    [TestClass]
    public class LibraryContainer
    {
        [TestMethod]
        public void AddTitleTests()
        {
            Library l = new Library();

            Title t = new Title("sample");

            l.FoundSeries.TryAdd(t.id, t);

            if(!l.FoundSeries.TryGetValue(t.id, out var result) || result != t)
            {
                Assert.Fail("Series not added correctly");
            }

        }

    }
}
