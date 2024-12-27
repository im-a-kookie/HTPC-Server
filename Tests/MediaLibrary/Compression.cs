using Cookie.ContentLibrary;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Tests.MediaLibrary
{
    [TestClass]
    public class Compression
    {


        [TestMethod]
        public void TestCompression()
        {
            Library testLibrary = new("mock-library");

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
            for (int i = 1; i < 24; i++)
            {
                MediaFile file = new MediaFile();
                file.SNo = 1;
                file.EpNo = i;
                file.SetPath(title, $"{directory}\\{name}{i.ToString().PadLeft(2, '0')} {suffix}.mkv");
                realPaths.Add(file.Path);
                mockFiles.Add(file);
                season.Eps.Add(file);

                originalLength += file.Path.Length;
                title.EpisodeList.Add("1x" + i.ToString().PadLeft(2, '0'), file);

            }

            // now let's mock the series into the library
            testLibrary.FoundSeries.TryAdd("mock title", title);
            testLibrary.CompressPaths();

            int compressedLength = 0;
            // Ensure that the files decompress correctly
            for (int i = 0; i < mockFiles.Count; i++)
            {
                var decompressedPath = mockFiles[i].DecompressPath(testLibrary);
                Assert.AreEqual(realPaths[i], decompressedPath, "Compression has corrupted data!");
                compressedLength += mockFiles[i].Path.Length;
            }

            double ratio = (double)compressedLength / (double)originalLength;

            ConsoleOutput.Instance.WriteLine($"Compression Complete. Ratio: {Math.Round(ratio * 100) / 100}.", OutputLevel.Information);

            if (ratio > 0.7d)
            {
                Assert.Fail("Compression is not working appropriately");
            }

            if (ratio > 0.5d)
            {
                ConsoleOutput.Instance.WriteLine("Warning: Ratio is poor.", OutputLevel.Information);
            }



        }








    }
}
