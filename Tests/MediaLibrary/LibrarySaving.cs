using Cookie.ContentLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MediaLibrary
{
    [TestClass]
    public class LibrarySaving
    {

        [TestMethod]
        public void TestLibrarySerialize()
        {
            bool didNotify = false;
            try
            {
                Directory.Delete("mock_library", true);
            }
            catch { }

            Library testLibrary = new Library("mock_library");

            testLibrary.OnSeriesUpdate += (x, y) => didNotify = true;

            // set up some values here
            string directory = @"C:\Movies\has a long messy prefix with random junk";
            string name = "Random Thing S01E";
            string suffix = "[long messy]suffix[x265][720p] with lots of junk";

            List<MediaFile> mockFiles = new();
            List<string> realPaths = new();
            var season = new Season();
            var title = new Title("mock title");
            testLibrary.FoundSeries.TryAdd(title.ID, title);

            title.Owner = testLibrary;

            title.Eps.Add(1, season);
            int originalLength = 0;

            // Now let's generate the episodes
            for (int i = 1; i < 25; i++)
            {
                MediaFile file = new MediaFile();
                file.SNo = 1;
                file.EpNo = i;
                file.SetPath(title, $"{directory}\\{name}{i.ToString().PadLeft(2, '0')} {suffix}.mkv");
                realPaths.Add(file.Path);
                mockFiles.Add(file);
                season.Eps.Add(file);

                originalLength += file.Path.Length;
                title.EpisodeList.Add("1x" + i.ToString(), file);

            }

            Assert.IsTrue(didNotify, "The series update event is not triggered!");

            // Now let's complete the construction
            testLibrary.CompressPaths();
            testLibrary.RefreshTargetFileMaps();
            testLibrary.Save();
            testLibrary.StoreCache();

            var result = new Library(testLibrary.RootPath);

            if (result.FoundSeries.TryGetValue(title.ID, out var resultTitle))
            {
                Assert.AreEqual(resultTitle, title, "The stored title does not match!");

                foreach (var ep2 in mockFiles)
                {
                    bool has = false;
                    foreach (var ep1 in resultTitle.EpisodeList)
                    {
                        if (ep1.Value.Path == ep2.Path &&
                            ep1.Value.EpNo == ep2.EpNo &&
                            ep1.Value.SNo == ep2.SNo)
                        {
                            has = true;
                            break;
                        }
                    }
                    Assert.IsTrue(has, "Did not find episode " + ep2.SNo + "x" + ep2.EpNo);
                }
            }
            else Assert.Fail("The series is missing!");

            if (result.NameToSeries.TryGetValue(title.Name, out resultTitle))
            {
                Assert.AreEqual(title, resultTitle, "The name mapped series does not match!");
            }
            else Assert.Fail("The series is not mapped by name!");



        }

    }
}
