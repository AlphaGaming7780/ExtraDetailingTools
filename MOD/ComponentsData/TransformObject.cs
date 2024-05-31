using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace ExtraDetailingTools.ComponentsData
{
    internal struct TransformObject : IComponentData, IQueryTypeParameter, IEquatable<TransformObject>, ISerializable
    {
        public float3 m_Scale;

        public TransformObject(float3 Scale = new())
        {
            m_Scale = Scale;
        }

        public readonly bool Equals(TransformObject other)
        {
            return other.m_Scale.Equals(m_Scale);
            
            //return false;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Scale);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Scale);
        }
    }
}
