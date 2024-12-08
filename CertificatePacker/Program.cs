

using CookieCrumbs;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;

foreach (var certificate in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.pfx"))
{
    string name = Path.GetFileName(certificate);

    Console.WriteLine("Loading Certificate: " + certificate);
    while(true)
    {
        Console.Write("Enter Password: ");
        string key = Console.ReadLine()!;
        try
        {
            var serverCertificate = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(certificate), key, X509KeyStorageFlags.DefaultKeySet);

            byte[] data = File.ReadAllBytes(certificate);
            byte[] password = Encoding.UTF8.GetBytes(key);

            // Now we need to generate the thingimy-whatsit
            try
            {
                File.Delete($"{name}.archive");
            }
            catch { }

            using var stream = File.OpenWrite($"{name}.archive");
            using BinaryWriter bw = new BinaryWriter(stream);

            var edata = AesHelper.Encrypt(data);
            var ekey = AesHelper.Encrypt(password);

            bw.Write(edata.Length);
            bw.Write(edata);
            bw.Write(ekey.Length);
            bw.Write(ekey);





        }
        catch { }

    }


}