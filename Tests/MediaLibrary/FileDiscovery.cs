using Backend.ServerLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MediaLibrary
{
    [TestClass]
    public class FileDiscovery
    {

        public static IEnumerable<(int season, int episode, string show, string path)> GetTestData()
        {
            yield return (1, 1, "scavengers reign", 
                @"S:\Media\Scavengers Reign (2023) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S01E01 - The Signal (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );

            yield return (1, 2, "scavengers reign",
                @"S:\Media\Scavengers Reign (2023) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S01E02 - The Storm (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );

            yield return (1, 3, "scavengers reign",
                @"S:\Media\Scavengers Reign (2023) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S01E03 - The Wall (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );

            yield return (2, 1, "scavengers reign",
                @"S:\Media\Scavengers Reign (2023) Season 2 S02 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S02E01 - Axed!!!! (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );

            yield return (2, 2, "scavengers reign",
                @"S:\Media\Scavengers Reign (2023) Season 2 S02 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S02E02 - Really (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );

            yield return (2, 3, "scavengers reign",
                @"S:\Media\Scavengers Reign (2023) Season 2 S02 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Garshasp)\Scavengers Reign (2023) - S02E03 - WHY TF (1080p AMZN WEB-DL x265 Garshasp).mkv"
            );
        }

        [TestMethod]
        [DynamicData(nameof(GetTestData), DynamicDataSourceType.Method)]
        public void TestCompleteFileReading(int season, int episode, string show, string path)
        {
            var result = Searcher.ParseFileName(path);
            Assert.IsNotNull(result, "The function returned null instead of data!");
            Assert.AreEqual(show.Trim().ToLower(), result.Value.Title.Trim().ToLower());
            Assert.AreEqual(season, result.Value.Season, "The season does not match.");
            Assert.AreEqual(episode, result.Value.Episode, "The season does not match.");
        }

        [TestMethod]
        public void TestEpisodeSuffixed()
        {
            //"S:\Downloads\Doctor Slump (2024)\Doctor Slump 01.mkv"
            int expect = 10;
            string input = $"Doctor Slump {expect} [1080p]";
            var result = Searcher.TryReadEpisode(input);
            Assert.AreEqual(expect, result, $"The episode does not match! {input}");
        }

        [TestMethod]
        public void TestSeasonEpisode()
        {
            int expectSeason = 2;
            int expectEpisode = 10;
            string input = $"Show S{expectSeason.ToString().PadLeft(2, '0')}E{expectEpisode.ToString().PadLeft(2, '0')}";
            var result = Searcher.GetSeasonEpisode(input);

            Assert.AreEqual(expectSeason, result.season, $"Season does not match! {input}");
            Assert.AreEqual(expectEpisode, result.episode, $"Episode does not match!{input}");


        }

    }
}
