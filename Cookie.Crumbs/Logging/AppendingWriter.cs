using System.Text;

namespace Cookie.Logging
{
    public class AppendingWriter : TextWriter
    {

        private readonly string _filePath;
        public AppendingWriter(string filePath)
        {
            _filePath = filePath;

            //validate the path
            File.AppendAllText(filePath, "");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The log file {filePath} could not be generated!");
            }

        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            using (var writer = new StreamWriter(_filePath, append: true))
            {
                writer.Write(value);
            }
        }

        public override void Write(string? value)
        {
            using (var writer = new StreamWriter(_filePath, append: true))
            {
                writer.Write(value);
            }
        }

        public override void WriteLine(string? value)
        {
            using (var writer = new StreamWriter(_filePath, append: true))
            {
                writer.WriteLine(value);
            }
        }
    }
}