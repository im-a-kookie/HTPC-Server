using Cookie.Logging;
using System.Text.RegularExpressions;

namespace Tests.MediaLibrary.Logging
{
    [TestClass]
    public class LoggerTest
    {

        /// <summary>
        /// Simple container for streams
        /// </summary>
        public class StreamContainer : IDisposable
        {
            public Stream UnderlyingStream;
            public TextWriter Writer;
            public TextReader Reader;

            public StreamContainer()
            {
                UnderlyingStream = new MemoryStream();
                Writer = new StreamWriter(UnderlyingStream);
                Reader = new StreamReader(UnderlyingStream);
            }

            public void Dispose()
            {
                Writer.Dispose();
                Reader.Dispose();
                UnderlyingStream.Dispose();
            }
        }

        /// <summary>
        /// Tests printing messages at every level, and ensure that the messages are printed (or not) with awareness
        /// of the configured logger level
        /// </summary>
        [TestMethod]
        public void TestAllLevels()
        {

            List<LogLevel> levels = Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>().OrderBy(x => (int)x).ToList();
            // go from Debug -> Info -> etc
            foreach (var initial in levels)
            {
                using var container = new StreamContainer();
                var logger = new LoggerStream(initial, null, [container.Writer]);
                // print every message
                foreach (var e in levels)
                {
                    logger.Log($"test <{e.ToString() ?? "void"}>", (LogLevel)e);
                }

                container.UnderlyingStream.Seek(0, SeekOrigin.Begin);
                var str = container.Reader.ReadToEnd();
                str = str.Replace("\r", "\n");

                // Now go through every level, and ensure it is, or is not, printed correctly
                foreach (var test in levels)
                {
                    var searcher = Regex.Escape($"[{test.ToString()}]: test <{test.ToString() ?? "void"}>\n");
                    if (test < initial)
                    {
                        StringAssert.DoesNotMatch(str, new Regex(searcher));
                    }
                    else
                    {
                        StringAssert.Matches(str, new Regex(searcher));
                    }
                }
            }
        }

        /// <summary>
        /// Ensures that the logger prints to a file, if specified
        /// </summary>
        [TestMethod]
        public void TestFileOutput()
        {
            string testLogPath = "testlog.txt";

            try
            {
                File.Delete(testLogPath);
            }
            catch { }

            var logger = new LoggerStream(LogLevel.Debug, testLogPath);

            Logger.SetTarget(logger);
            Logger.Info("Bananas");
            Logger.ResetTarget();

            logger.Dispose();

            // terminate the logger
            var result = File.ReadAllText(testLogPath);

            StringAssert.Contains(result, "[Info]");
            StringAssert.Contains(result, "Bananas");

            try
            {
                File.Delete(testLogPath);
            }
            catch { }
        }

        /// <summary>
        /// Validates that messages/warnings are correctly formatted
        /// </summary>
        [TestMethod]
        public void TestMessageInjection()
        {
            try
            {
                var m = Messages.StaticMethodNoInstance;
                string code = m.Code;
                string name = m.Name;
                string body = m.Name;

                using var container = new StreamContainer();
                var logger = new LoggerStream(LogLevel.Debug, null, [container.Writer]);

                Logger.SetTarget(logger);

                // Check that it can warn
                m.Warn("Custom Message");

                container.UnderlyingStream.Seek(0, SeekOrigin.Begin);
                var result = container.Reader.ReadToEnd();

                StringAssert.Contains(result, "[Warn]");
                StringAssert.Contains(result, code);
                StringAssert.Contains(result, name);
                StringAssert.Contains(result, "Custom Message");

                // Check that it can debug
                m.Debug("New Message");
                container.UnderlyingStream.Seek(0, SeekOrigin.Begin);
                result = container.Reader.ReadToEnd();

                StringAssert.Contains(result, "[Debug]");
                StringAssert.Contains(result, code);
                StringAssert.Contains(result, name);
                StringAssert.Contains(result, "New Message");


            }
            finally
            {
                Logger.ResetTarget();
            }
        }



    }
}
