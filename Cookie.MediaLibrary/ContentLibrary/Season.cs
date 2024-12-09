namespace Cookie.ContentLibrary
{
    public class Season : BasicSerial
    {

        public List<MediaFile> Eps { get; set; } = new();

        public override void Read(Dictionary<string, string> data)
        {
            if (data.TryGetValue("Eps", out var result))
            {
                var list = ReadList(result);
                foreach (var str in list)
                {
                    var file = new MediaFile();
                    file.Read((Dictionary<string, string>)BasicSerial.Read(str)!);
                    Eps.Add(file);
                }
            }
        }

        public override Dictionary<string, string> Write()
        {
            return new() { { "Eps", Condense(Eps.ToList<object>()) } };
        }



    }
}
