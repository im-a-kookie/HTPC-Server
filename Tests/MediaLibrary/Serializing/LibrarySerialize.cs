using Cookie.ContentLibrary;
using Cookie.Serializers;
using Cookie.Serializers.Bytewise;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MediaLibrary.Serializing
{
    [TestClass]
    public class LibrarySerialize
    {

        [TestMethod]
        public void TestLibrarySerialize()
        {
            Library testLibrary = new Library();

            // set up some values here
            string directory = @"C:\Movies\has a long messy prefix with random junk";
            string name = "Random Thing S01E";
            string suffix = "[long messy]suffix[x265][720p] with lots of junk";

            List<MediaFile> mockFiles = new();
            List<string> realPaths = new();
            var season = new Season();
            var title = new Title("mock title");
            title.Eps.Add(1, season);
            int originalLength = 0;

            // Now let's generate the episodes
            for (int i = 1; i < 25; i++)
            {
                MediaFile file = new MediaFile();
                file.SNo = 1;
                file.EpNo = i;
                file.Path = $"{directory}\\{name}{i.ToString().PadLeft(2, '0')} {suffix}.mkv";
                realPaths.Add(file.Path);
                mockFiles.Add(file);
                season.Eps.Add(file);

                originalLength += file.Path.Length;
                title.EpisodeList.Add("1x" + i.ToString(), file);

            }

            // now let's mock the series into the library
            testLibrary.FoundSeries.TryAdd("mock title", title);
            testLibrary.CompressPaths();

            // write it to a stream
            using MemoryStream output = new MemoryStream();
            Byter.ToBytes(output, ((IDictable)testLibrary).MakeDictionary());
            output.Seek(0, SeekOrigin.Begin);

            // now read it back from the stream
            var resultDictionary = Byter.FromBytes(output);
            Assert.IsNotNull(resultDictionary, "The deserialization was null!");
            Library result = new();
            result.FromDictionary(resultDictionary!);

            if (result.FoundSeries.TryGetValue(title.id, out var resultSeries))
            {
                int count = 0;
                foreach(var episode in resultSeries.EpisodeList)
                {
                    bool has = false;
                    foreach(var e in mockFiles)
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

                if(count != mockFiles.Count)
                    Assert.Fail("Failed to load episodes!");


            }
            else Assert.Fail("Did not find series!");


        }

    }
}
