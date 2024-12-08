using System.Security.Cryptography.X509Certificates;

namespace Backend.SSL
{
    internal class CertificateCache
    {

        public Dictionary<string, X509Certificate2> availableCertificates = [];

        public CertificateCache()
        {
            LoadCertificates(Directory.GetCurrentDirectory(), availableCertificates);
        }

        public X509Certificate2? GetOrNull(string key)
        {
            if (availableCertificates.TryGetValue(key, out var cert)) return cert;
            return null;
        }

        public X509Certificate2? FirstOrNull()
        {
            if (availableCertificates.Count > 0) return availableCertificates.First().Value;
            return null;
        }

        public static void LoadCertificates(string directoryPath, Dictionary<string, X509Certificate2> availableCertificates)
        {
            try
            {
                // Enumerate all files in the specified directory.
                foreach (var filePath in Directory.EnumerateFiles(directoryPath))
                {
                    try
                    {
                        // Get the filename without the extension and its extension.
                        var filename = Path.GetFileNameWithoutExtension(filePath);
                        var extension = Path.GetExtension(filePath)?.ToLower();

                        // Only process .pfx files.
                        if (extension == ".pfx")
                        {
                            string keyFilePath = Path.Combine(Path.GetDirectoryName(filePath), $"{filename}.key");
                            string key = null;

                            // Attempt to read the key file, ignoring any errors.
                            if (File.Exists(keyFilePath))
                            {
                                try
                                {
                                    key = File.ReadAllText(keyFilePath);
                                }
                                catch (IOException ex)
                                {
                                    Console.WriteLine($"Failed to read key file '{keyFilePath}': {ex.Message}");
                                }
                            }

                            // Load the certificate with or without the key.
                            X509Certificate2 cert;
                            if (!string.IsNullOrEmpty(key))
                            {
                                try
                                {
                                    cert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(filePath), key);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to load certificate '{filePath}' with key: {ex.Message}");
                                    continue; // Skip to the next file if loading fails.
                                }
                            }
                            else
                            {
                                try
                                {
                                    cert = X509CertificateLoader.LoadCertificateFromFile(filePath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to load certificate '{filePath}': {ex.Message}");
                                    continue; // Skip to the next file if loading fails.
                                }
                            }

                            // Add the certificate to the dictionary if loading succeeded.
                            availableCertificates[filename] = cert;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while processing file '{filePath}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while loading certificates from directory '{directoryPath}': {ex.Message}");
            }
        }


    }




}
