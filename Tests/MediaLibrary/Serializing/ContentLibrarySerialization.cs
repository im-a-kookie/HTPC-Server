using Cookie.ContentLibrary;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;

namespace Tests.MediaLibrary.Serializing
{
    /// <summary>
    /// Asserts serialization of Library, Title, MediaRile, Season
    /// </summary>
    [TestClass]
    public class ContentLibrarySerialization
    {

        [TestMethod]
        public void TestLibrarySerialize()
        {
            try
            {
                Directory.Delete("mock_library", true);
            }
            catch { }

            Library testLibrary = new("mock_library");


            // set up some values here
            string directory = @"C:\Movies\has a long messy prefix with random junk";
            string name = "Random Thing S01E";
            string suffix = "[long messy]suffix[x265][720p] with lots of junk";

            List<MediaFile> mockFiles = [];
            List<string> realPaths = [];
            var season = new Season();
            var title = new Title("mock title");
            testLibrary.FoundSeries.TryAdd(title.ID, title);

            title.Owner = testLibrary;

            title.Eps.Add(1, season);
            int originalLength = 0;

            // Now let's generate the episodes
            for (int i = 1; i < 25; i++)
            {
                MediaFile file = new()
                {
                    SNo = 1,
                    EpNo = i
                };

                file.SetPath(null, $"{directory}\\{name}{i.ToString().PadLeft(2, '0')} {suffix}.mkv");
                realPaths.Add(file.Path);
                mockFiles.Add(file);
                season.Eps.Add(file);

                originalLength += file.Path.Length;
                title.EpisodeList.Add("1x" + i.ToString(), file);

            }

            // Now let's complete the construction
            testLibrary.CompressPaths();
            testLibrary.RefreshTargetFileMaps();

            // write it to a stream
            using MemoryStream output = new();
            Byter.ToBytes(output, testLibrary.MakeFullDictionary());
            output.Seek(0, SeekOrigin.Begin);

            // now read it back from the stream
            var resultDictionary = Byter.FromBytes(output);
            Assert.IsNotNull(resultDictionary, "The deserialization was null!");
            Library result = new("mock_library");
            result.FromDictionary(resultDictionary!);

            if (result.FoundSeries.TryGetValue(title.ID, out var resultSeries))
            {
                int count = 0;
                foreach (var episode in resultSeries.EpisodeList)
                {
                    bool has = false;
                    foreach (var e in mockFiles)
                    {
                        if (e.Path == episode.Value.Path)
                        {
                            Assert.AreEqual(e.Data, episode.Value.Data, "Episode data invalid!");
                            Assert.AreEqual(e.DecompressPath(result), episode.Value.DecompressPath(result), "Episode data invalid!");

                            has = true;
                            ++count;
                            break;
                        }
                    }
                    if (!has)
                        Assert.Fail("Failed to load episodes!");
                }
                if (count != mockFiles.Count)
                    Assert.Fail("Failed to load episodes!");
            }
            else Assert.Fail("Did not find series!");

            if(Directory.Exists("mock_library"))
            {
                Assert.Fail("The library should not write to disk without explicit instruction!");
            }

        }
    }
}
