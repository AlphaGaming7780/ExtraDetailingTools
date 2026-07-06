using Colossal.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools.Gizmos
{
    [Flags]
    public enum GizmosRaycastType : uint
    {
        Line =         1 << 0,
        Bezier =       1 << 1,
        Arrow =        1 << 2,
        ArrowHead =    1 << 3,
        Sphere =       1 << 4,
        Cube =         1 << 5,
        WireArc =      1 << 6,
        Cylinder =     1 << 7,
        Cone =         1 << 8,
        Capsule =      1 << 9,
        Frustum =      1 << 10,
        CapsuleConic = 1 << 11,

        ALL = uint.MaxValue
    }

    public struct GizmosRaycastInput
    {
        public GizmosRaycastType m_Type;

        public Line3.Segment m_Line;

        public float m_Tolerance;

        public bool m_Debug;
    }
}
