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

    public struct GizmosData : IComponentData
    {
        public GizmoType Type;

        // Points génériques
        public float3 A;
        public float3 B;
        public float3 C;
        public float3 D;

        // Paramètres génériques (radius, size, etc)
        public float4 Params0; // (x,y,z,w) libre usage
        public float4 Params1;

        // Orientation / TRS simplifié
        public float4x4 TRS;

        public Color Color; // float4 au lieu de Color (Burst friendly)

        // Options packées
        public int Segments;
        public int Flags;
    }
}
