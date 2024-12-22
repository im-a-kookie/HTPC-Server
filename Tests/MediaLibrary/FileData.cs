using Cookie.ContentLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MediaLibrary
{
    [TestClass]
    public class FileData
    {

        [TestMethod]
        public void TestMediaFileSetting()
        {

            int season = 4;
            int episode = 9;

            MediaFile mf = new();
            mf.SNo = season;
            Assert.AreEqual(season, mf.SNo, "The season is not set correctly!");

            mf.EpNo = episode;
            Assert.AreEqual(episode, mf.EpNo, "The season is not set correctly!");
            Assert.AreEqual(season, mf.SNo, "Setting episode broke season!");


        }
    }
}
