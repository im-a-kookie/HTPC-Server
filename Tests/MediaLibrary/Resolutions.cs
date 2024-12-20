using System.Drawing;

namespace Tests.MediaLibrary
{

    [TestClass]
    public class ResolutionTests
    {

        /// <summary>
        /// Validates that resolution names are retrieved correctly.
        /// <para>Asserts methodological correctness for subsequent tests.</para>
        /// </summary>
        [TestMethod]
        public void ValidateResolutionDetails()
        {
            List<string> inputs = ["2160p", "1080p", "720p", "w43vtvgr"];
            List<string> expectNames = ["2160p", "1080p", "720p", "Unknown"];
            List<int> expectSizes = [2160, 1080, 720, -1];

            for (int i = 0; i < inputs.Count; i++)
            {
                // setup
                var testValue = inputs[i];
                var expectName = expectNames[i];
                var expectSize = expectSizes[i];

                var fileName = $"video_{testValue}.mp4";
                // get values
                var index = Resolutions.Match(fileName);
                var result = Resolutions.GetName(index);
                var size = Resolutions.GetSize(index);

                Assert.AreEqual(result, (string)expectName, $"Should match {expectName} resolution index.");
                Assert.AreEqual(size, expectSize, $"Size should match {expectSize}.");

            }
        }

        /// <summary>
        /// Tests that the resolution matching works relatively well
        /// against various different resolutions that may be provided.
        /// 
        /// <para>This test is not exhaustive, as the underlying method
        /// is generally fairly flexible.</para>
        /// </summary>
        [TestMethod]
        public void MatchNearbyResolutions()
        {
            Dictionary<Size, List<string>> inputs = new()
            {
                // make sure the standard resolutions work
                { new(1920, 1080), ["1080p", "FHD"] },
                { new(1280, 720), ["720p", "HD"] },
                { new(3840, 2160), ["2160p", "4K", "UHD"] },
                // some random 720p values from ~near~ 720p
                { new(1280, 682), ["720p", "HD"] },
                { new(1280, 704), ["720p", "HD"] },
                { new(1280, 725), ["720p", "HD"] },
                // some fake 720p wannabe values that are really 480p
                { new(1280, 536), ["480p", "SD"] },
                { new(1280, 528), ["480p", "SD"] }
            };

            foreach (var input in inputs)
            {
                var index = Resolutions.Match(input.Key);
                var result = Resolutions.GetName(index);
                CollectionAssert.Contains(input.Value, result, $"Incorrect match: {input.Key}, expected: [{string.Join(". ", input.Value)}], got: {result}");
            }



        }


    }
}
