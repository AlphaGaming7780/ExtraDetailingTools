using Colossal.Internal.Gizmos;
using Colossal.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtraDetailingTools.Gizmos
{
    public static class GizmosUtils
    {
        public static GizmosData DrawLine(float3 a, float3 b, Color color)
        {
            return new GizmosData()
            {
                Type = GizmoType.Line,
                A = a,
                B = b,
                Color = color
            };
        }

        public static GizmosData DrawArrow(float3 a, float3 b, Color color, float headLength = 0.4f, float headAngle = 25f, int circleSegmentsCount = 16)
        {
            return new GizmosData()
            {
                Type = GizmoType.Arrow,
                A = a,
                B = b,
                Color = color,
                Params0 = new float4(headLength, headAngle, 0, 0),
                Segments = circleSegmentsCount
            };
        }

        public static GizmosData DrawWireSphere(float3 center, float radius, Color color, int slicesX = 1, int slicesY = 2, int slicesZ = 0, int segmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.Sphere,
                A = center,
                Params0 = new float4(radius, slicesX, slicesY, slicesZ),
                Color = color,
                Segments = segmentsCount
            };
        }

        public static GizmosData DrawWireArc(float3 center, float3 normal, float3 from, float angle, float radius, Color color, int segmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.WireArc,
                A = center,
                B = normal,
                C = from,
                Params0 = new float4(angle, radius, 0, 0),
                Color = color,
                Segments = segmentsCount
            };
        }
    }
}
