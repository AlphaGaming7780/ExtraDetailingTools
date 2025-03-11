using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools.Structs
{
    public struct ColorRG16 : ISerializable
    {
        public byte r, g;

        public ColorRG16(byte r, byte g)
        {
            this.r = r;
            this.g = g;
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out r);
            reader.Read(out g);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(r);
            writer.Write(g);
        }
    }
}
