using Colossal.Serialization.Entities;
using ExtraDetailingTools.Structs;
using ExtraDetailingTools.Systems.Tools;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace ExtraDetailingTools.Prefabs
{
    public struct GrassData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public SerializableTextureStruct<ColorRG16> textureStruct;

        public GrassData(SerializableTextureStruct<ColorRG16> textureStruct)
        {
            this.textureStruct = textureStruct;
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out textureStruct);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(textureStruct);
        }
    }
}
