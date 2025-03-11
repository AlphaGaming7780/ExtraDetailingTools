using Colossal.Serialization.Entities;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ExtraDetailingTools.Structs
{
    public struct TextureStruct<T> where T : struct
    {
        public NativeArray<T> data;
        public int width;
        public int height;

        public TextureStruct(int width, int height)
        {
            this.data = new NativeArray<T>( width * height, Allocator.Persistent );
            this.width = width;
            this.height = height;
        }

        public TextureStruct(Texture2D texture)
        {
            if ((texture.format & (TextureFormat.BC4 | TextureFormat.BC5 | TextureFormat.BC7)) > 0)
            {
                throw new Exception($"The input texture isn't compatible, use Texture.GetPixels32().");
            }
            this.data = texture.GetPixelData<T>(0);
            this.width = texture.width;
            this.height = texture.height;
        }

        //public static TextureStruct<T> FromTexture2D(Texture2D texture)
        //{

        //    if( (texture.format & (TextureFormat.BC4 | TextureFormat.BC5 | TextureFormat.BC7 ) ) > 0)
        //    {
        //        throw new Exception($"The input texture isn't compatible, use Texture.GetPixels32().");
        //    }

        //    TextureStruct<T> textureStruct = default;
        //    textureStruct.data = texture.GetPixelData<T>(0);
        //    textureStruct.width = texture.width;
        //    textureStruct.height = texture.height;
        //    return textureStruct;
        //}


        public void SetValue(int x, int y, T value)
        {
            if (y < 0 || y > height - 1 || x < 0 || x > width - 1)
            {
                throw new IndexOutOfRangeException($"Input x : {x}, max width {width} | Input y : {y}, max width {height}");
            }

            int val = y * width + x;

            if (val >= data.Length)
            {
                throw new IndexOutOfRangeException($"data.Length : {data.Length} | Calculate : {val}");
            }

            data[val] = value;
        }

        public T GetValue(int x, int y)
        {
            if (y < 0 || y > height - 1 || x < 0 || x > width - 1)
            {
                throw new IndexOutOfRangeException($"Input x : {x}, max width {width} | Input y : {y}, max width {height}");
            }

            int val = y * width + x;

            if (val >= data.Length)
            {
                throw new IndexOutOfRangeException($"data.Length : {data.Length} | Calculate : {val}");
            }

            return data[val];
        }

        public void SetValue(int2 int2, T value)
        {
            SetValue(int2.x, int2.y, value);
        }

        public T GetValue(int2 int2)
        {
            return GetValue(int2.x, int2.y);
        }

        public float2 Size()
        {
            return new float2(width, height);
        }
    }
}
