using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Serializers.Bytewise
{
    internal class Byter
    {
        public short Version = 1;


        public void ToBytes(Stream output, Dictionary<string, object> data)
        {
            using BinaryWriter bw = new BinaryWriter(output);

            //the header only needs to store the integers
            var pos = bw.BaseStream.Position;
            bw.Write(Version);
            bw.Write(0);

            foreach(var item in data)
            {



            }

        }

        public void ToStream(BinaryWriter writer, List<string> data)
        {
            writer.Write(data.Count);
            foreach(var s in data)
            {
                writer.Write(s);
            }
        }

        public void ToStream(BinaryWriter writer, List<object> data)
        {
            writer.Write(data.Count);
            foreach (var s in data)
            {
                writer.Write(s.ToString()!);
            }
        }

    }

}
