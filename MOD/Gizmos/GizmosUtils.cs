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

        public static GizmosData DrawBezier(Bezier4x3 bezier, Color color, float length = 1f, int segmentsCount = 16)
        {
            return new GizmosData()
            {
                Type = GizmoType.Bezier,
                A = bezier.a,
                B = bezier.b,
                C = bezier.c,
                D = bezier.d,
                Params0 = new float4(length, 0, 0, 0),
                Color = color,
                Segments = segmentsCount
            };
        }

        public static GizmosData DrawArrowHead(float3 pos, float3 dir, Color color, float headLength = 0.4f, float headAngle = 25f, int circleSegmentsCount = 16)
        {
            return new GizmosData()
            {
                Type = GizmoType.ArrowHead,
                A = pos,
                B = dir,
                Params0 = new float4(headLength, headAngle, 0, 0),
                Color = color,
                Segments = circleSegmentsCount
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

        public static GizmosData DrawWireCube(float3 center, float3 size, Color color)
        {
            return DrawWireCube(float4x4.identity, center, size, color);
        }

        public static GizmosData DrawWireCube(float4x4 trs, float3 center, float3 size, Color color)
        {
            return new GizmosData()
            {
                Type = GizmoType.Cube,
                TRS = trs,
                A = center,
                Params0 = new float4(size, 0),
                Color = color
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

        public static GizmosData DrawWireCylinder(float3 center, float radius, float height, Color color, int circleSegmentsCount = 36)
        {
            return DrawWireCylinder(float4x4.identity, center, radius, height, color, circleSegmentsCount);
        }

        public static GizmosData DrawWireCylinder(float4x4 trs, float3 center, float radius, float height, Color color, int circleSegmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.Cylinder,
                TRS = trs,
                A = center,
                Params0 = new float4(radius, height, 0, 0),
                Color = color,
                Segments = circleSegmentsCount
            };
        }

        public static GizmosData DrawWireCone(float3 a, float radiusA, float3 b, float radiusB, Color color, int circleSegmentsCount = 36)
        {
            return DrawWireCone(float4x4.identity, a, radiusA, b, radiusB, color, circleSegmentsCount);
        }

        public static GizmosData DrawWireCone(float4x4 trs, float3 a, float radiusA, float3 b, float radiusB, Color color, int circleSegmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.Cone,
                TRS = trs,
                A = a,
                B = b,
                Params0 = new float4(radiusA, radiusB, 0, 0),
                Color = color,
                Segments = circleSegmentsCount
            };
        }

        public static GizmosData DrawWireCapsule(float3 center, float radius, float height, Color color, int circleSegmentsCount = 36)
        {
            return DrawWireCapsule(float4x4.identity, center, radius, height, color, circleSegmentsCount);
        }

        public static GizmosData DrawWireCapsule(float4x4 trs, float3 center, float radius, float height, Color color, int circleSegmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.Capsule,
                TRS = trs,
                A = center,
                Params0 = new float4(radius, height, 0, 0),
                Color = color,
                Segments = circleSegmentsCount
            };
        }

        public static GizmosData DrawWireCapsuleConic(float3 a, float radiusA, float3 b, float radiusB, Color color, int circleSegmentsCount = 36)
        {
            return DrawWireCapsuleConic(float4x4.identity, a, radiusA, b, radiusB, color, circleSegmentsCount);
        }

        public static GizmosData DrawWireCapsuleConic(float4x4 trs, float3 a, float radiusA, float3 b, float radiusB, Color color, int circleSegmentsCount = 36)
        {
            return new GizmosData()
            {
                Type = GizmoType.CapsuleConic,
                TRS = trs,
                A = a,
                B = b,
                Params0 = new float4(radiusA, radiusB, 0, 0),
                Color = color,
                Segments = circleSegmentsCount
            };
        }

        public static GizmosData DrawWireFrustum(float4x4 trs, float fov, float minRange, float maxRange, float aspect, Color color)
        {
            return new GizmosData()
            {
                Type = GizmoType.Frustum,
                TRS = trs,
                Params0 = new float4(fov, minRange, maxRange, aspect),
                Color = color
            };
        }
    }
}
