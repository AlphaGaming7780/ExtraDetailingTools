using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace ExtraDetailingTools.ComponentsData
{
    internal struct TransformObject : IComponentData, IQueryTypeParameter, IEquatable<TransformObject>, ISerializable
    {
        public float3 Scale;

        public TransformObject(float3 Scale)
        {
            this.Scale = Scale;
        }

        public readonly bool Equals(TransformObject other)
        {
            return other.Scale.Equals(Scale);
            
            //return false;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Scale);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Scale);
        }
    }
}
