using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Snapping
{
    // Mirrors ObjectToolSystemExtraSnap.ObjectSideSnapMode (MOD/ExtraSnap/ObjectTool.cs) - moved here so any
    // tool's SnapJob can select a mode without depending on ObjectTool's own types.
    public enum ObjectSideSnapMode
    {
        FreeMove,
        SnapToCenter,
        SnapToCorner,
    }

    // Standalone port of the Object Side snap logic that lives in ObjectToolSystemExtraSnap
    // (MOD/ExtraSnap/ObjectTool.cs) - extracted and parameterized on ComponentLookups/entities instead of
    // instance fields, so any tool's SnapJob (ObjectTool's own, TransformToolSnapJob, future ones, ...) can
    // call it. ObjectTool.cs is NOT wired to this file yet - it still runs its own copy of these functions;
    // this is only scaffolding for reuse.
    public static class SnapObjectSide
    {
        // Finds the nearest object by bounds proximity and snaps controlPoint's position/rotation against
        // it in the given mode, writing the result into bestSnapPosition via ToolUtils.AddSnapPosition.
        //
        // The search already excludes any entity currently Overridden (e.g. something with an active
        // Temp/preview elsewhere, including the very entity being dragged by TransformGizmoTool, which the
        // game marks Overridden while its own Temp exists) - see BoundsObjectIterator.
        //
        // placedRotation is the caller's current desired rotation for the placed/dragged object (ObjectTool
        // reads this from its own RotationRef; TransformGizmoTool would use its raw candidate rotation).
        public static void Apply(
            ObjectSideSnapMode mode,
            ControlPoint controlPoint,
            ref ControlPoint bestSnapPosition,
            quaternion placedRotation,
            ObjectGeometryData placedGeometryData,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree,
            ComponentLookup<Owner> ownerData,
            ComponentLookup<Transform> transformData,
            ComponentLookup<PrefabRef> prefabRefData,
            ComponentLookup<ObjectGeometryData> objectGeometryData)
        {
            Entity targetEntity = FindNearbySideTarget(
                controlPoint,
                placedGeometryData,
                objectSearchTree,
                ownerData,
                transformData,
                prefabRefData,
                objectGeometryData
            );

            if (!transformData.HasComponent(targetEntity))
            {
                return;
            }

            // ===== Prefab & target bounds =====
            Bounds3 placedBounds = ObjectUtils.GetBounds(placedGeometryData);

            PrefabRef targetPrefabRef = prefabRefData[targetEntity];
            Entity targetPrefab = targetPrefabRef.m_Prefab;

            Transform targetTransform = transformData[targetEntity];
            Bounds3 targetBounds = ObjectUtils.GetBounds(objectGeometryData[targetPrefab]);

            float3 placedSize = placedBounds.max - placedBounds.min;

            // ===== Target base quad =====
            Quad2 targetQuad = ObjectUtils.CalculateBaseCorners(targetTransform.m_Position, targetTransform.m_Rotation, targetBounds).xz;

            // ===== Rotation handling (relative to target object) =====
            quaternion relativeRotation = math.mul(math.inverse(targetTransform.m_Rotation), placedRotation);
            quaternion snappedRelativeRotation = ClampTo90(relativeRotation);
            quaternion snappedWorldRotation = math.mul(targetTransform.m_Rotation, snappedRelativeRotation);
            float snappedYaw = math.Euler(snappedWorldRotation).y;

            // ===== Snap validity test (shared by FreeMove and SnapToCenter - both pick among the target's
            // edges based on cursor proximity/overlap) =====
            Quad2 placedQuadAtTarget = ObjectUtils.CalculateBaseCorners(
                targetTransform.m_Position,
                targetTransform.m_Rotation,
                new float2(placedSize.x, placedSize.z)
            ).xz;

            Quad2 placedQuadAtHit = ObjectUtils.CalculateBaseCorners(
                controlPoint.m_HitPosition,
                snappedWorldRotation,
                placedSize.xz * 0.5f
            ).xz;

            bool allowSnap =
                MathUtils.Intersect(placedQuadAtTarget, placedQuadAtHit) &&
                MathUtils.Intersect(targetQuad, controlPoint.m_HitPosition.xz);

            switch (mode)
            {
                case ObjectSideSnapMode.SnapToCenter:
                {
                    // Still an edge snap, like FreeMove - just fixed at the middle of whichever edge is
                    // closest instead of sliding with the cursor.
                    CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.a, targetQuad.b), snappedYaw, allowSnap);
                    CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.b, targetQuad.c), snappedYaw, allowSnap);
                    CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.c, targetQuad.d), snappedYaw, allowSnap);
                    CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.d, targetQuad.a), snappedYaw, allowSnap);
                    break;
                }

                case ObjectSideSnapMode.SnapToCorner:
                {
                    // Same idea as FreeMove's per-edge check, but for the 4 corners: offset outward from
                    // the corner (away from the target's center) by the placed object's own half-extent
                    // along that diagonal, so it tucks flush against both edges meeting there. Whichever
                    // corner ends up closest to the cursor wins, via the same AddSnapPosition priority
                    // comparison.
                    float2 targetCenterXZ = (targetQuad.a + targetQuad.c) * 0.5f;
                    CheckSnapCorner(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, targetQuad.a, targetCenterXZ, snappedYaw);
                    CheckSnapCorner(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, targetQuad.b, targetCenterXZ, snappedYaw);
                    CheckSnapCorner(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, targetQuad.c, targetCenterXZ, snappedYaw);
                    CheckSnapCorner(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, targetQuad.d, targetCenterXZ, snappedYaw);
                    break;
                }

                default: // FreeMove
                {
                    // ===== Iterate over all target edges =====
                    CheckSnapLineGeneric(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.a, targetQuad.b), snappedYaw, allowSnap);
                    CheckSnapLineGeneric(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.b, targetQuad.c), snappedYaw, allowSnap);
                    CheckSnapLineGeneric(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.c, targetQuad.d), snappedYaw, allowSnap);
                    CheckSnapLineGeneric(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.d, targetQuad.a), snappedYaw, allowSnap);
                    break;
                }
            }
        }

        public static Entity FindNearbySideTarget(
            ControlPoint controlPoint,
            ObjectGeometryData placedGeometryData,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree,
            ComponentLookup<Owner> ownerData,
            ComponentLookup<Transform> transformData,
            ComponentLookup<PrefabRef> prefabRefData,
            ComponentLookup<ObjectGeometryData> objectGeometryData)
        {
            BoundsObjectIterator iterator = new BoundsObjectIterator
            {
                m_ControlPoint = controlPoint,
                m_Bounds = ObjectUtils.CalculateBounds(controlPoint.m_Position, controlPoint.m_Rotation, placedGeometryData),
                m_PlacedObjectGeometryData = placedGeometryData,
                m_BestEntity = Entity.Null,
                m_BestOverlap = float.MaxValue,
                m_OwnerData = ownerData,
                m_TransformData = transformData,
                m_PrefabRefData = prefabRefData,
                m_ObjectGeometryData = objectGeometryData,
            };
            objectSearchTree.Iterate(ref iterator);
            return iterator.m_BestEntity;
        }

        // Adapted from Game.Tools.ObjectToolSystem.ParentObjectIterator's non-building/asset-stamp branch -
        // the generic "does this candidate's volume overlap the placed object's volume" test, stripped of
        // the building/asset-stamp special-casing that's specific to vanilla's AutoParent feature. Finds
        // the closest nearby object by volume proximity instead of requiring the raycast to hit its
        // geometry.
        private struct BoundsObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public ControlPoint m_ControlPoint;
            public Bounds3 m_Bounds;
            public ObjectGeometryData m_PlacedObjectGeometryData;

            public Entity m_BestEntity;
            public float m_BestOverlap;

            [ReadOnly] public ComponentLookup<Owner> m_OwnerData;
            [ReadOnly] public ComponentLookup<Transform> m_TransformData;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly] public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                // Entities with an active Temp/preview elsewhere (e.g. something else currently being
                // dragged/edited) stay in the tree but lose this mask bit instead of being removed - same
                // check Game.Objects.ValidationHelpers uses. Free: the mask is already on the bounds we're
                // handed, no extra lookup.
                if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
                {
                    return false;
                }
                return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
                {
                    return;
                }
                if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz))
                {
                    return;
                }
                PrefabRef prefabRef = m_PrefabRefData[item];
                if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
                {
                    return;
                }
                Transform targetTransform = m_TransformData[item];
                ObjectGeometryData targetGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
                if ((targetGeometryData.m_Flags & Game.Objects.GeometryFlags.Physical) == 0 && m_OwnerData.HasComponent(item))
                {
                    return;
                }

                float num = m_BestOverlap;

                float3 center = MathUtils.Center(bounds.m_Bounds);
                quaternion q = math.inverse(m_ControlPoint.m_Rotation);
                quaternion q2 = math.inverse(targetTransform.m_Rotation);
                float3 placedLocal = math.mul(q, m_ControlPoint.m_Position - center);
                float3 targetLocal = math.mul(q2, targetTransform.m_Position - center);

                if ((m_PlacedObjectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                {
                    Cylinder3 cylinder = new Cylinder3
                    {
                        circle = new Circle2(m_PlacedObjectGeometryData.m_Size.x * 0.5f - 0.01f, placedLocal.xz),
                        height = new Bounds1(0.01f, m_PlacedObjectGeometryData.m_Size.y - 0.01f) + placedLocal.y,
                        rotation = m_ControlPoint.m_Rotation
                    };
                    if ((targetGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                    {
                        Cylinder3 cylinder2 = new Cylinder3
                        {
                            circle = new Circle2(targetGeometryData.m_Size.x * 0.5f - 0.01f, targetLocal.xz),
                            height = new Bounds1(0.01f, targetGeometryData.m_Size.y - 0.01f) + targetLocal.y,
                            rotation = targetTransform.m_Rotation
                        };
                        float3 pos = default;
                        if (Game.Objects.ValidationHelpers.Intersect(cylinder, cylinder2, ref pos))
                        {
                            num = math.distance(pos, m_ControlPoint.m_Position);
                        }
                    }
                    else
                    {
                        Box3 box = default;
                        box.bounds = MathUtils.Expand(targetGeometryData.m_Bounds + targetLocal, -0.01f);
                        box.rotation = targetTransform.m_Rotation;
                        if (MathUtils.Intersect(cylinder, box, out var cylinderIntersection, out var boxIntersection))
                        {
                            float3 start = math.mul(cylinder.rotation, MathUtils.Center(cylinderIntersection));
                            float3 end = math.mul(box.rotation, MathUtils.Center(boxIntersection));
                            num = math.distance(center + math.lerp(start, end, 0.5f), m_ControlPoint.m_Position);
                        }
                    }
                }
                else
                {
                    Box3 box2 = default;
                    box2.bounds = MathUtils.Expand(m_PlacedObjectGeometryData.m_Bounds + placedLocal, -0.01f);
                    box2.rotation = m_ControlPoint.m_Rotation;
                    if ((targetGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                    {
                        Cylinder3 cylinder3 = new Cylinder3
                        {
                            circle = new Circle2(targetGeometryData.m_Size.x * 0.5f - 0.01f, targetLocal.xz),
                            height = new Bounds1(0.01f, targetGeometryData.m_Size.y - 0.01f) + targetLocal.y,
                            rotation = targetTransform.m_Rotation
                        };
                        if (MathUtils.Intersect(cylinder3, box2, out var cylinderIntersection2, out var boxIntersection2))
                        {
                            float3 start2 = math.mul(box2.rotation, MathUtils.Center(boxIntersection2));
                            float3 end2 = math.mul(cylinder3.rotation, MathUtils.Center(cylinderIntersection2));
                            num = math.distance(center + math.lerp(start2, end2, 0.5f), m_ControlPoint.m_Position);
                        }
                    }
                    else
                    {
                        Box3 box3 = default;
                        box3.bounds = MathUtils.Expand(targetGeometryData.m_Bounds + targetLocal, -0.01f);
                        box3.rotation = targetTransform.m_Rotation;
                        if (MathUtils.Intersect(box2, box3, out var intersection4, out var intersection5))
                        {
                            float3 start3 = math.mul(box2.rotation, MathUtils.Center(intersection4));
                            float3 end3 = math.mul(box3.rotation, MathUtils.Center(intersection5));
                            num = math.distance(center + math.lerp(start3, end3, 0.5f), m_ControlPoint.m_Position);
                        }
                    }
                }

                if (num < m_BestOverlap)
                {
                    m_BestEntity = item;
                    m_BestOverlap = num;
                }
            }
        }

        private static void CheckSnapLineGeneric(
            Bounds3 bounds,                    // Bounds of the PLACED prefab
            Transform targetTransform,          // Transform of the TARGET object
            ControlPoint controlPoint,
            ref ControlPoint bestPosition,
            Line2 line,
            float angle,
            bool forceSnap)
        {
            // Rotation of the placed object (world)
            quaternion rotation = quaternion.RotateY(angle);

            float3 center = (bounds.min + bounds.max) * 0.5f;
            float2 centerOffset = math.mul(rotation, center).xz;

            // Edge direction & outward normal
            float2 edgeDir = math.normalize(line.b - line.a);
            float2 normal = new float2(-edgeDir.y, edgeDir.x);

            // Half-size of placed object (local space)
            float3 size = bounds.max - bounds.min;
            float2 halfSize = size.xz * 0.5f;

            // Local axes of the placed object in world space
            float2 axisX = math.normalize(math.mul(rotation, new float3(1f, 0f, 0f)).xz);
            float2 axisZ = math.normalize(math.mul(rotation, new float3(0f, 0f, 1f)).xz);

            // Project OBB onto edge normal
            float offset =
                math.abs(math.dot(axisX, normal)) * halfSize.x +
                math.abs(math.dot(axisZ, normal)) * halfSize.y;

            // Project OBB onto edge direction (PARALLEL side length)
            float halfLength =
                 math.abs(math.dot(axisX, edgeDir)) * halfSize.x +
                 math.abs(math.dot(axisZ, edgeDir)) * halfSize.y;

            // Project mouse position onto edge. controlPoint.m_HitPosition (not m_Position) - RaycastSystem
            // sets m_Position to the HIT ENTITY's own transform position when the raycast hits an object
            // directly, while m_HitPosition is the real ray/surface intersection point.
            MathUtils.Distance(line, controlPoint.m_HitPosition.xz, out float t);

            float lineLength = math.distance(line.a, line.b);
            t *= lineLength;
            t = math.clamp(t, 0f - halfLength, lineLength + halfLength);

            // Position along the edge (THIS is what allows sliding)
            float2 pointOnLine = math.lerp(line.a, line.b, t / lineLength);

            // pointOnLine + normal * offset is where the placed object's own GEOMETRIC CENTER needs to end
            // up (pushed out from the line by its own half-extent so its near face is flush with it).
            // m_Position is the object's PIVOT, not its center, so convert center -> pivot by subtracting
            // the full centerOffset vector (not just its normal component - the tangential part matters
            // too whenever the prefab's local bounds aren't centered on its pivot).
            float2 worldCenter = pointOnLine + normal * offset;
            float2 snappedXZ = worldCenter - centerOffset;

            ControlPoint snapPosition = controlPoint;
            snapPosition.m_OriginalEntity = Entity.Null;
            snapPosition.m_Position.xz = snappedXZ;
            snapPosition.m_Position.y = targetTransform.m_Position.y;

            // Rotation aligned to target edge
            snapPosition.m_Direction = math.mul(rotation, new float3(0f, 0f, 1f)).xz;
            snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);

            float level = forceSnap ? 1f : 0f;
            snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(
                level,
                1f,
                0f,
                controlPoint.m_HitPosition * 0.5f,
                snapPosition.m_Position * 0.5f,
                snapPosition.m_Direction
            );

            ToolUtils.AddSnapPosition(ref bestPosition, snapPosition);
        }

        // Same OBB-onto-edge-normal projection as CheckSnapLineGeneric, but the position along the edge is
        // fixed at the midpoint instead of following the cursor - still an edge snap, just without the
        // sliding. Whichever of the target's 4 edges ends up closest to the cursor wins via the same
        // AddSnapPosition priority comparison FreeMove uses.
        private static void CheckSnapLineCenter(
            Bounds3 bounds,                    // Bounds of the PLACED prefab
            Transform targetTransform,          // Transform of the TARGET object
            ControlPoint controlPoint,
            ref ControlPoint bestPosition,
            Line2 line,
            float angle,
            bool forceSnap)
        {
            quaternion rotation = quaternion.RotateY(angle);

            float3 center = (bounds.min + bounds.max) * 0.5f;
            float2 centerOffset = math.mul(rotation, center).xz;

            float2 edgeDir = math.normalize(line.b - line.a);
            float2 normal = new float2(-edgeDir.y, edgeDir.x);

            float3 size = bounds.max - bounds.min;
            float2 halfSize = size.xz * 0.5f;

            float2 axisX = math.normalize(math.mul(rotation, new float3(1f, 0f, 0f)).xz);
            float2 axisZ = math.normalize(math.mul(rotation, new float3(0f, 0f, 1f)).xz);

            float offset =
                math.abs(math.dot(axisX, normal)) * halfSize.x +
                math.abs(math.dot(axisZ, normal)) * halfSize.y;

            // Fixed at the edge's midpoint - no cursor projection/clamping like CheckSnapLineGeneric.
            float2 pointOnLine = (line.a + line.b) * 0.5f;

            // See CheckSnapLineGeneric - full centerOffset subtraction, not just its normal component.
            float2 worldCenter = pointOnLine + normal * offset;
            float2 snappedXZ = worldCenter - centerOffset;

            ControlPoint snapPosition = controlPoint;
            snapPosition.m_OriginalEntity = Entity.Null;
            snapPosition.m_Position.xz = snappedXZ;
            snapPosition.m_Position.y = targetTransform.m_Position.y;

            snapPosition.m_Direction = math.mul(rotation, new float3(0f, 0f, 1f)).xz;
            snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);

            float level = forceSnap ? 1f : 0f;
            snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(
                level,
                1f,
                0f,
                controlPoint.m_HitPosition * 0.5f,
                snapPosition.m_Position * 0.5f,
                snapPosition.m_Direction
            );

            ToolUtils.AddSnapPosition(ref bestPosition, snapPosition);
        }

        private static void CheckSnapCorner(
            Bounds3 bounds,                    // Bounds of the PLACED prefab
            Transform targetTransform,          // Transform of the TARGET object
            ControlPoint controlPoint,
            ref ControlPoint bestPosition,
            float2 corner,
            float2 targetCenterXZ,
            float angle)
        {
            quaternion rotation = quaternion.RotateY(angle);

            float3 center = (bounds.min + bounds.max) * 0.5f;
            float2 centerOffset = math.mul(rotation, center).xz;

            float3 size = bounds.max - bounds.min;
            float2 halfSize = size.xz * 0.5f;

            float2 axisX = math.normalize(math.mul(rotation, new float3(1f, 0f, 0f)).xz);
            float2 axisZ = math.normalize(math.mul(rotation, new float3(0f, 0f, 1f)).xz);

            // Direction pointing away from the target's center, through this corner - the placed object
            // gets tucked into the corner along this diagonal.
            float2 outward = math.normalizesafe(corner - targetCenterXZ, new float2(1f, 0f));

            // Project the placed object's OBB half-extent onto that outward diagonal, same technique
            // CheckSnapLineGeneric uses to project onto an edge normal.
            float offset =
                math.abs(math.dot(axisX, outward)) * halfSize.x +
                math.abs(math.dot(axisZ, outward)) * halfSize.y;

            // corner + outward * offset is the placed object's GEOMETRIC CENTER; subtract the full
            // centerOffset to convert to its pivot (m_Position) - see CheckSnapLineGeneric for why the
            // full vector (not just one projected component) is needed.
            float2 worldCenter = corner + outward * offset;
            float2 snappedXZ = worldCenter - centerOffset;

            ControlPoint snapPosition = controlPoint;
            snapPosition.m_OriginalEntity = Entity.Null;
            snapPosition.m_Position.xz = snappedXZ;
            snapPosition.m_Position.y = targetTransform.m_Position.y;

            snapPosition.m_Direction = math.mul(rotation, new float3(0f, 0f, 1f)).xz;
            snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);

            snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(
                1f,
                1f,
                0f,
                controlPoint.m_HitPosition * 0.5f,
                snapPosition.m_Position * 0.5f,
                snapPosition.m_Direction
            );

            ToolUtils.AddSnapPosition(ref bestPosition, snapPosition);
        }

        public static quaternion ClampTo90(quaternion q)
        {
            float3 forward = math.mul(q, new float3(0, 0, 1));
            float yaw = math.atan2(forward.x, forward.z);
            float snappedYaw = math.round(yaw / (math.PI / 2f)) * (math.PI / 2f);
            return quaternion.RotateY(snappedYaw);
        }
    }
}
