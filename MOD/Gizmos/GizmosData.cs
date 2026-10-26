using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Gizmos
{
    // Order must match GizmosRaycastType's bit order (GizmosRaycastSystem.MatchType shifts by (int)Type).
    public enum GizmoType
    {
        Line,
        Bezier,
        Arrow,
        ArrowHead,
        Sphere,
        Cube,
        WireArc,
        Cylinder,
        Cone,
        Capsule,
        Frustum,
        CapsuleConic,
    }

    [Flags]
    public enum GizmoFlags
    {
        None = 0,
        // WireArc: the half behind its center, seen from the camera, is drawn faded and ignored by the raycast.
        CullBackHalf = 1 << 0,
    }

    public struct GizmosData : IComponentData
    {
        public GizmoType Type;

        public float3 A;
        public float3 B;
        public float3 C;
        public float3 D;

        public float4 Params0; // Shape parameters (radius, size, ...)
        public float4 Params1;

        public float4x4 TRS;

        public Color Color;

        public int Segments;
        public int Flags;
    }
}
