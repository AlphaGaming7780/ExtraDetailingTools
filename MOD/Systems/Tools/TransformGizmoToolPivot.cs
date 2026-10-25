using Unity.Collections;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool
    {
        // See docs/TransformGizmo-TODO.md, section 5. Three independent settings instead of one
        // combinatorial enum. Values marked "later" are accepted but fall back to a V1 behaviour.
        public enum PivotSource
        {
            Origin = 0,
            GeometryCenter,     // later, falls back to Origin
            GeometryBottom,     // later, falls back to Origin
        }

        public enum PivotAggregation
        {
            Median = 0,
            BoundsCenter,
            Primary,
            Individual,
            Custom,             // later, falls back to Median
        }

        public enum PivotOrientation
        {
            Global = 0,
            LocalPrimary,
            LocalIndividual,    // later, falls back to LocalPrimary
        }

        public struct PivotSettings
        {
            public PivotSource Source;
            public PivotAggregation Aggregation;
            public PivotOrientation Orientation;
        }

        // Pure math on purpose (no EntityManager, no managed types) so it can move into the Burst snap job
        // later (step 5 bis). Called in Idle to place the gizmo, and once at StartDragging where the result
        // is frozen until StopDragging - recomputing it while dragging would make the pivot follow the
        // objects it's moving.
        internal static void ComputePivots(NativeArray<Transform> transforms, int primaryIndex, PivotSettings settings, out float3 gizmoPos, out quaternion gizmoRot, NativeArray<float3> pivotPerEntity)
        {
            gizmoPos = default;
            gizmoRot = quaternion.identity;

            int count = transforms.Length;
            if (count == 0) return;

            primaryIndex = math.clamp(primaryIndex, 0, count - 1);
            Transform primary = transforms[primaryIndex];

            // Source: V1 only has Origin (Transform.m_Position, the base of the object).
            float3 groupPivot;
            switch (settings.Aggregation)
            {
                case PivotAggregation.Primary:
                case PivotAggregation.Individual: // The gizmo is drawn on the primary entity.
                    groupPivot = primary.m_Position;
                    break;

                case PivotAggregation.BoundsCenter:
                    {
                        float3 min = transforms[0].m_Position;
                        float3 max = min;
                        for (int i = 1; i < count; i++)
                        {
                            min = math.min(min, transforms[i].m_Position);
                            max = math.max(max, transforms[i].m_Position);
                        }
                        groupPivot = (min + max) * 0.5f;
                        break;
                    }

                default: // Median, Custom
                    {
                        float3 sum = float3.zero;
                        for (int i = 0; i < count; i++)
                        {
                            sum += transforms[i].m_Position;
                        }
                        groupPivot = sum / count;
                        break;
                    }
            }

            for (int i = 0; i < count && i < pivotPerEntity.Length; i++)
            {
                pivotPerEntity[i] = settings.Aggregation == PivotAggregation.Individual ? transforms[i].m_Position : groupPivot;
            }

            gizmoPos = groupPivot;
            gizmoRot = settings.Orientation == PivotOrientation.Global ? quaternion.identity : primary.m_Rotation;
        }
    }
}
