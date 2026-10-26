using Colossal.Mathematics;
using Unity.Collections;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool
    {
        // Three independent settings. Unimplemented values fall back to the noted one.
        public enum PivotSource
        {
            Origin = 0,
            GeometryCenter,
            GeometryBottom,
        }

        public enum PivotAggregation
        {
            Median = 0,
            BoundsCenter,
            Primary,
            Individual,
            Custom,             // Falls back to Median
        }

        public enum PivotOrientation
        {
            Global = 0,
            LocalPrimary,
            LocalIndividual,    // Falls back to LocalPrimary
        }

        public struct PivotSettings
        {
            public PivotSource Source;
            public PivotAggregation Aggregation;
            public PivotOrientation Orientation;
        }

        // World point of an entity used as its pivot. localBounds: ObjectGeometryData.m_Bounds, default without geometry (origin).
        internal static float3 GetSourcePoint(Transform transform, Bounds3 localBounds, PivotSource source)
        {
            float3 center = MathUtils.Center(localBounds);
            float3 local = source switch
            {
                PivotSource.GeometryCenter => center,
                PivotSource.GeometryBottom => new float3(center.x, localBounds.min.y, center.z),
                _ => float3.zero,
            };
            return transform.m_Position + math.rotate(transform.m_Rotation, local);
        }

        // Burst compatible. Frozen during a drag, otherwise the pivot would follow the moved objects.
        // localBounds: one per transform, or empty to use the origins.
        internal static void ComputePivots(NativeArray<Transform> transforms, NativeArray<Bounds3> localBounds, int primaryIndex, PivotSettings settings, out float3 gizmoPos, out quaternion gizmoRot, NativeArray<float3> pivotPerEntity)
        {
            gizmoPos = default;
            gizmoRot = quaternion.identity;

            int count = transforms.Length;
            if (count == 0) return;

            primaryIndex = math.clamp(primaryIndex, 0, count - 1);
            Transform primary = transforms[primaryIndex];

            NativeArray<float3> points = new(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
            {
                points[i] = i < localBounds.Length ? GetSourcePoint(transforms[i], localBounds[i], settings.Source) : transforms[i].m_Position;
            }

            float3 groupPivot;
            switch (settings.Aggregation)
            {
                case PivotAggregation.Primary:
                case PivotAggregation.Individual: // The gizmo is drawn on the primary entity.
                    groupPivot = points[primaryIndex];
                    break;

                case PivotAggregation.BoundsCenter:
                    {
                        float3 min = points[0];
                        float3 max = min;
                        for (int i = 1; i < count; i++)
                        {
                            min = math.min(min, points[i]);
                            max = math.max(max, points[i]);
                        }
                        groupPivot = (min + max) * 0.5f;
                        break;
                    }

                default: // Median, Custom
                    {
                        float3 sum = float3.zero;
                        for (int i = 0; i < count; i++)
                        {
                            sum += points[i];
                        }
                        groupPivot = sum / count;
                        break;
                    }
            }

            for (int i = 0; i < count && i < pivotPerEntity.Length; i++)
            {
                pivotPerEntity[i] = settings.Aggregation == PivotAggregation.Individual ? points[i] : groupPivot;
            }
            points.Dispose();

            gizmoPos = groupPivot;
            gizmoRot = settings.Orientation == PivotOrientation.Global ? quaternion.identity : primary.m_Rotation;
        }
    }
}
