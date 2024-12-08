using System.Reflection;

namespace CookieCrumbs.Utils
{
    internal class ResourceTool
    {
        /// <summary>
        /// Gets the embedded file from the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string? GetResource(string path)
        {

            // Get the assembly that contains the embedded resource
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    Console.WriteLine("Resource not found.");
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }



    }
}
