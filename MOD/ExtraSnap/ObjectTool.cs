using Colossal.Collections;
using Colossal.Entities;
using ExtraLib;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static ExtraDetailingTools.ExtraSnap.ObjectToolSystemExtraSnap;
using static Game.Tools.ObjectToolSystem;
using Colossal.Mathematics;
using Colossal.UI.Binding;


#if RELEASE
using Unity.Burst;
#endif


namespace ExtraDetailingTools.ExtraSnap
{
    public class ObjectToolSystemExtraSnap : ExtraSnapBase<ObjectToolSystem, ObjectToolExtraSnap>
    {
        [Flags]
        public enum ObjectToolExtraSnap : uint
        {
            None,
            ObjectSurface   = 1 << 0,
            Upright         = 1 << 1,
            ObjectSide      = 1 << 2,
            ALL             = uint.MaxValue,
        }

        public enum ObjectSideSnapMode
        {
            FreeMove,
            SnapToCenter,
            SnapToCorner,
        }

        readonly Traverse traverse;
        readonly WaterSystem m_WaterSystem;
        readonly TerrainSystem m_TerrainSystem;
        readonly SearchSystem m_ObjectSearchSystem;


        ObjectSideSnapMode m_ObjectSideSnapMode = ObjectSideSnapMode.FreeMove;


        // Trigger Bindings
        readonly TriggerBinding<ObjectSideSnapMode> m_ObjectSideSnap;

        // Address of ObjectToolSystem's own persistent-allocated NativeReference<Rotation> data, so the
        // job can read/write the tool's real rotation state directly instead of a throwaway copy. Cached
        // once: Allocator.Persistent allocations don't move for the tool's lifetime. Kept as a plain
        // IntPtr (not Rotation*) so nothing outside SnapJob's own RotationRef property needs unsafe code.
        readonly IntPtr m_RotationPtr;

        public ObjectToolSystemExtraSnap() : base()
        {
            traverse = Traverse.Create(m_Tool);
            m_WaterSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TerrainSystem>();
            m_ObjectSearchSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SearchSystem>();
            m_RotationPtr = GetRotationPtr();
            m_SelectedSnap = ObjectToolExtraSnap.ALL;
            m_SelectedSnap &= ~(ObjectToolExtraSnap.ObjectSide);

            m_ObjectSideSnap = AddTriggerBinding<ObjectSideSnapMode>("SetObjectSideSnapMode", (v) => SetObjectSideSnapMode(v), new EnumReader<ObjectSideSnapMode>());
        }

        private void SetObjectSideSnapMode(ObjectSideSnapMode v)
        {
            m_ObjectSideSnapMode = v;
            MarkDirty();
        }

        protected override void GetAvailableSnapMask(out ObjectToolExtraSnap onMask, out ObjectToolExtraSnap offMask)
        {
            ObjectPrefab prefab = GetObjectPrefab();
            if (prefab != null)
            {
                bool isBuilding = m_PrefabSystem.HasComponent<BuildingData>(prefab);
                bool isAssetStamp = !isBuilding && m_PrefabSystem.HasComponent<AssetStampData>(prefab);
                m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(prefab, out var component);
                GetAvailableSnapMask(component, IsEditor, isBuilding, isAssetStamp, m_Tool.actualMode, out onMask, out offMask);
            }
            else
            {
                base.GetAvailableSnapMask(out onMask, out offMask);
            }
        }

        private void GetAvailableSnapMask(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, ObjectToolSystem.Mode mode, out ObjectToolExtraSnap onMask, out ObjectToolExtraSnap offMask)
        {
            onMask = ObjectToolExtraSnap.Upright;
            offMask = ObjectToolExtraSnap.None;

            if (mode != ObjectToolSystem.Mode.Create) return;

            if ((prefabPlaceableData.m_Flags & PlacementFlags.OwnerSide) == PlacementFlags.None)
            {
                onMask |= ObjectToolExtraSnap.ObjectSide;
                offMask |= ObjectToolExtraSnap.ObjectSide;
            }

            if (!isBuilding && (prefabPlaceableData.m_Flags & (PlacementFlags.OwnerSide | PlacementFlags.RoadSide | PlacementFlags.Shoreline | PlacementFlags.Floating | PlacementFlags.Hovering | PlacementFlags.RoadNode | PlacementFlags.RoadEdge)) == PlacementFlags.None)
            {
                onMask |= ObjectToolExtraSnap.ObjectSurface | ObjectToolExtraSnap.Upright;
                offMask |= ObjectToolExtraSnap.ObjectSurface | ObjectToolExtraSnap.Upright;
            }
        }

        protected override void InitializeRaycast()
        {
            ObjectPrefab prefab = GetObjectPrefab();

            if (prefab != null)
            {

                if (m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(prefab, out var placeableObjectData))
                {

                }

                ObjectToolExtraSnap snap = GetActualSnap();

                if ((snap & ObjectToolExtraSnap.ObjectSurface) != ObjectToolExtraSnap.None)
                {
                    m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    if (m_ToolSystem.actionMode.IsEditor())
                    {
                        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }

                    if (!m_PrefabSystem.HasComponent<BuildingData>(prefab))
                    {
                        if (m_Tool.underground)
                        {
                            m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
                            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.PartialSurface;
                        }
                        else
                        {
                            m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
                            if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
                            {
                                m_ToolRaycastSystem.typeMask |= TypeMask.Water;
                            }
                            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
                            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                        }
                    }
                }

                if ((snap & ObjectToolExtraSnap.ObjectSide) != ObjectToolExtraSnap.None)
                {
                    m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    if (IsEditor)
                    {
                        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }
                }
            }
        }

        protected override JobHandle SnapControlPoint(JobHandle inputDeps)
        {
            NativeList<ControlPoint> controlPoints = traverse.Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>();
            Entity selected = ((m_Tool.actualMode == Mode.Move) ? traverse.Field("m_MovingObject").GetValue<Entity>() : GetUpgradable(m_ToolSystem.selected));

            m_Tool.GetPrefab();

            WaterSurfaceData<SurfaceWater> waterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterDeps);
            NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out JobHandle searchTreeDeps);

            JobHandle jobHandle = IJobExtensions.Schedule(new SnapJob
            {
                m_EditorMode = IsEditor,
                m_Snap = GetActualSnap(),
                m_ObjectSideSnapMode = m_ObjectSideSnapMode,
                m_Mode = m_Tool.actualMode,
                m_Prefab = m_PrefabSystem.GetEntity(traverse.Field("m_Prefab").GetValue<PrefabBase>()),
                m_Selected = selected,
                m_LastRaycastPoint = traverse.Field("m_LastRaycastPoint").GetValue<ControlPoint>(),
                m_Rotation = m_RotationPtr,
                m_ControlPoints = controlPoints,

                m_OwnerData = m_Tool.GetComponentLookup<Owner>(true),
                m_TransformData = m_Tool.GetComponentLookup<Transform>(true),
                m_LocalTransformCacheData = m_Tool.GetComponentLookup<LocalTransformCache>(true),
                m_ServiceUpgradeData = m_Tool.GetComponentLookup<Game.Buildings.ServiceUpgrade>(true),
                m_ObjectGeometryData = m_Tool.GetComponentLookup<ObjectGeometryData>(true),
                m_PrefabRefData = m_Tool.GetComponentLookup<PrefabRef>(true),
                m_PlaceableObjectData = m_Tool.GetComponentLookup<PlaceableObjectData>(true),
                m_BuildingData = m_Tool.GetComponentLookup<BuildingData>(true),
                m_StackData = m_Tool.GetComponentLookup<StackData>(true),
                m_MovingObjectData = m_Tool.GetComponentLookup<MovingObjectData>(true),
                m_WaterSurfaceData = waterSurfaceData,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
                m_ObjectSearchTree = objectSearchTree,
            }, JobHandle.CombineDependencies(JobHandle.CombineDependencies(inputDeps, waterDeps), searchTreeDeps));

            m_WaterSystem.AddSurfaceReader(jobHandle);
            m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);

            return jobHandle;
        }

        public override void OnWrite(IJsonWriter writer)
        {
            writer.PropertyName("ObjectSideSnapMode");
            writer.Write((uint)m_ObjectSideSnapMode);
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct SnapJob : IJob
        {
            [ReadOnly]  public bool m_EditorMode;

            [ReadOnly] public ObjectToolExtraSnap m_Snap;

            [ReadOnly] public ObjectSideSnapMode m_ObjectSideSnapMode;

            [ReadOnly] public Mode m_Mode;

            [ReadOnly] public Entity m_Prefab;

            [ReadOnly] public Entity m_Selected;

            [ReadOnly] public ControlPoint m_LastRaycastPoint;

            public NativeList<ControlPoint> m_ControlPoints;

            // Component lookups
            [ReadOnly] public ComponentLookup<Transform> m_TransformData;

            [ReadOnly] public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly] public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

            [ReadOnly] public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

            [ReadOnly] public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly] public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

            [ReadOnly] public ComponentLookup<BuildingData> m_BuildingData;

            [ReadOnly] public ComponentLookup<StackData> m_StackData;

            [ReadOnly] public ComponentLookup<MovingObjectData> m_MovingObjectData;

            [ReadOnly] public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

            [ReadOnly] public TerrainHeightData m_TerrainHeightData;

            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

            public IntPtr m_Rotation;

            // The only unsafe code needed for the rotation write-back - everywhere else uses this via
            // plain dot syntax, no unsafe context required at those call sites.
            private unsafe ref Rotation RotationRef => ref UnsafeUtility.AsRef<Rotation>((void*)m_Rotation);

            public void Execute()
            {
                ControlPoint controlPoint = m_LastRaycastPoint;
                ControlPoint bestSnapPosition = m_ControlPoints[m_ControlPoints.Length - 1];

                if ((m_Snap & ObjectToolExtraSnap.ObjectSurface) != ObjectToolExtraSnap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    int parentMesh = controlPoint.m_ElementIndex.x;
                    Entity entity2 = controlPoint.m_OriginalEntity;
                    while (m_OwnerData.HasComponent(entity2))
                    {
                        if (m_LocalTransformCacheData.HasComponent(entity2) && !m_ServiceUpgradeData.HasComponent(entity2))
                        {
                            parentMesh = m_LocalTransformCacheData[entity2].m_ParentMesh;
                            parentMesh += math.select(1000, -1000, parentMesh < 0);
                        }
                        entity2 = m_OwnerData[entity2].m_Owner;
                    }
                    if (m_TransformData.HasComponent(entity2))
                    {
                        SnapSurface(controlPoint, ref bestSnapPosition, entity2, parentMesh);
                    }
                }

                if ((m_Snap & ObjectToolExtraSnap.ObjectSide) != ObjectToolExtraSnap.None && m_ObjectGeometryData.HasComponent(m_Prefab))
                {
                    Entity targetEntity = FindNearbySideTarget(controlPoint);

                    if (m_TransformData.HasComponent(targetEntity))
                    {
                        // ===== Prefab & target bounds =====

                        Bounds3 placedBounds = ObjectUtils.GetBounds(m_ObjectGeometryData[m_Prefab]);

                        PrefabRef targetPrefabRef = m_PrefabRefData[targetEntity];
                        Entity targetPrefab = targetPrefabRef.m_Prefab;

                        Transform targetTransform = m_TransformData[targetEntity];
                        Bounds3 targetBounds = ObjectUtils.GetBounds(m_ObjectGeometryData[targetPrefab]);

                        float3 placedSize = placedBounds.max - placedBounds.min;
                        float3 targetSize = targetBounds.max - targetBounds.min;

                        // ===== Target base quad =====
                        Quad2 targetQuad = ObjectUtils.CalculateBaseCorners(
                            targetTransform.m_Position,
                            targetTransform.m_Rotation,
                            targetBounds
                        ).xz;


                        // ===== Rotation handling (relative to target object) =====
                        // Compute rotation of placed object relative to target
                        quaternion relativeRotation =
                            math.mul(
                                math.inverse(targetTransform.m_Rotation),
                                RotationRef.m_Rotation
                            );

                        // Clamp rotation in target local space
                        quaternion snappedRelativeRotation = ClampTo90(relativeRotation);

                        // Convert back to world space
                        quaternion snappedWorldRotation =
                            math.mul(
                                targetTransform.m_Rotation,
                                snappedRelativeRotation
                            );

                        // Extract final yaw used for snapping
                        float snappedYaw = math.Euler(snappedWorldRotation).y;


                        // ===== Snap validity test (shared by FreeMove and SnapToCenter - both pick
                        // among the target's edges based on cursor proximity/overlap) =====
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

                        switch (m_ObjectSideSnapMode)
                        {
                            case ObjectSideSnapMode.SnapToCenter:
                            {
                                // Still an edge snap, like FreeMove - just fixed at the middle of
                                // whichever edge is closest instead of sliding with the cursor.
                                CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.a, targetQuad.b), snappedYaw, allowSnap);
                                CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.b, targetQuad.c), snappedYaw, allowSnap);
                                CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.c, targetQuad.d), snappedYaw, allowSnap);
                                CheckSnapLineCenter(placedBounds, targetTransform, controlPoint, ref bestSnapPosition, new Line2(targetQuad.d, targetQuad.a), snappedYaw, allowSnap);
                                break;
                            }

                            case ObjectSideSnapMode.SnapToCorner:
                            {
                                // Same idea as FreeMove's per-edge check, but for the 4 corners: offset
                                // outward from the corner (away from the target's center) by the placed
                                // object's own half-extent along that diagonal, so it tucks flush against
                                // both edges meeting there. Whichever corner ends up closest to the
                                // cursor wins, via the same AddSnapPosition priority comparison.
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
                                CheckSnapLineGeneric(
                                    placedBounds,
                                    targetTransform,
                                    controlPoint,
                                    ref bestSnapPosition,
                                    new Line2(targetQuad.a, targetQuad.b),
                                    snappedYaw,
                                    allowSnap
                                );

                                CheckSnapLineGeneric(
                                    placedBounds,
                                    targetTransform,
                                    controlPoint,
                                    ref bestSnapPosition,
                                    new Line2(targetQuad.b, targetQuad.c),
                                    snappedYaw,
                                    allowSnap
                                );

                                CheckSnapLineGeneric(
                                    placedBounds,
                                    targetTransform,
                                    controlPoint,
                                    ref bestSnapPosition,
                                    new Line2(targetQuad.c, targetQuad.d),
                                    snappedYaw,
                                    allowSnap
                                );

                                CheckSnapLineGeneric(
                                    placedBounds,
                                    targetTransform,
                                    controlPoint,
                                    ref bestSnapPosition,
                                    new Line2(targetQuad.d, targetQuad.a),
                                    snappedYaw,
                                    allowSnap
                                );
                                break;
                            }
                        }
                    }
                }

                //if (!m_EditorMode && (m_Snap & Snap.AutoParent) != Snap.None && controlPoint.m_OriginalEntity != Entity.Null)
                //{
                //    Entity entity2 = controlPoint.m_OriginalEntity;
                //    bestSnapPosition.m_OriginalEntity = entity2;
                //}

                CalculateHeight(ref bestSnapPosition);

                // ObjectSurface's tilt-to-surface-normal (below, via AlignObject) is a continuous,
                // automatic cosmetic adjustment recomputed fresh from the raycast every frame - not a
                // hard constraint the player is fighting. It's never bit-identical frame to frame on a
                // non-flat surface (wall, sloped roof, ...), so any change-detection here would read as
                // "still snapping" forever and permanently lock manual rotation input the moment the
                // surface isn't flat (GetAllowRotation() reads m_IsSnapped). Our extra snaps should never
                // block manual rotation, so this is intentionally always false.
                RotationRef.m_IsSnapped = false;
                RotationRef.m_IsAligned &= RotationRef.m_Rotation.Equals(bestSnapPosition.m_Rotation);
                AlignObject(ref bestSnapPosition, ref RotationRef.m_ParentRotation, RotationRef.m_IsAligned);
                RotationRef.m_Rotation = bestSnapPosition.m_Rotation;

                if ((bestSnapPosition.m_OriginalEntity == Entity.Null || bestSnapPosition.m_ElementIndex.x == -1 || bestSnapPosition.m_HitDirection.y > 0.99f)
                    && m_ObjectGeometryData.TryGetComponent(m_Prefab, out var boundsGeometryData)
                    && boundsGeometryData.m_Bounds.min.y <= -0.01f
                    && ((m_PlaceableObjectData.TryGetComponent(m_Prefab, out var wallPlaceableData) && (wallPlaceableData.m_Flags & (Game.Objects.PlacementFlags.Wall | Game.Objects.PlacementFlags.Hanging)) != Game.Objects.PlacementFlags.None && (m_Snap & ObjectToolExtraSnap.Upright) != ObjectToolExtraSnap.None)
                        || (m_EditorMode && m_MovingObjectData.HasComponent(m_Prefab))))
                {
                    bestSnapPosition.m_Elevation -= boundsGeometryData.m_Bounds.min.y;
                    bestSnapPosition.m_Position.y -= boundsGeometryData.m_Bounds.min.y;
                }

                if (m_StackData.TryGetComponent(m_Prefab, out var stackData) && stackData.m_Direction == StackDirection.Up)
                {
                    float stackOffset = stackData.m_FirstBounds.max + MathUtils.Size(stackData.m_MiddleBounds) * 2f - stackData.m_LastBounds.min;
                    bestSnapPosition.m_Elevation += stackOffset;
                    bestSnapPosition.m_Position.y += stackOffset;
                }

                m_ControlPoints[m_ControlPoints.Length - 1] = bestSnapPosition;

            }

            private Entity FindNearbySideTarget(ControlPoint controlPoint)
            {
                ObjectGeometryData placedGeometryData = m_ObjectGeometryData[m_Prefab];
                BoundsObjectIterator iterator = new BoundsObjectIterator
                {
                    m_ControlPoint = controlPoint,
                    m_Bounds = ObjectUtils.CalculateBounds(controlPoint.m_Position, controlPoint.m_Rotation, placedGeometryData),
                    m_PlacedObjectGeometryData = placedGeometryData,
                    m_BestEntity = Entity.Null,
                    m_BestOverlap = float.MaxValue,
                    m_OwnerData = m_OwnerData,
                    m_TransformData = m_TransformData,
                    m_PrefabRefData = m_PrefabRefData,
                    m_ObjectGeometryData = m_ObjectGeometryData,
                };
                m_ObjectSearchTree.Iterate(ref iterator);
                return iterator.m_BestEntity;
            }

            // Adapted from Game.Tools.ObjectToolSystem.ParentObjectIterator's non-building/asset-stamp
            // branch - the generic "does this candidate's volume overlap the placed object's volume" test,
            // stripped of the building/asset-stamp special-casing that's specific to vanilla's AutoParent
            // feature. Finds the closest nearby object by volume proximity instead of requiring the
            // raycast to hit its geometry.
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
                    return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
                {
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

            private void CalculateHeight(ref ControlPoint controlPoint)
            {
                if (!m_PlaceableObjectData.HasComponent(m_Prefab))
                {
                    return;
                }
                PlaceableObjectData placeableObjectData = m_PlaceableObjectData[m_Prefab];

                if (m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    controlPoint.m_Position.y += placeableObjectData.m_PlacementOffset.y;
                    return;
                }
                float height;
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None && m_BuildingData.HasComponent(m_Prefab))
                {
                    BuildingData buildingData = m_BuildingData[m_Prefab];
                    float3 worldPosition = Game.Buildings.BuildingUtils.CalculateFrontPosition(new Transform(controlPoint.m_Position, controlPoint.m_Rotation), buildingData.m_LotSize.y);
                    height = TerrainUtils.SampleHeight(ref m_TerrainHeightData, worldPosition);
                }
                else
                {
                    height = TerrainUtils.SampleHeight(ref m_TerrainHeightData, controlPoint.m_Position);
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
                {
                    float waterHeight = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, controlPoint.m_Position);
                    waterHeight += placeableObjectData.m_PlacementOffset.y;
                    controlPoint.m_Elevation = math.max(0f, waterHeight - height);
                    height = math.max(height, waterHeight);
                }
                else if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
                {
                    height += placeableObjectData.m_PlacementOffset.y;
                }
                else
                {
                    float waterHeight = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, controlPoint.m_Position, out float waterDepth);
                    if (waterDepth >= 0.2f)
                    {
                        waterHeight += placeableObjectData.m_PlacementOffset.y;
                        if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Floating) != Game.Objects.PlacementFlags.None)
                        {
                            controlPoint.m_Elevation = math.max(0f, waterHeight - height);
                        }
                        height = math.max(height, waterHeight);
                    }
                }
                controlPoint.m_Position.y = height;
            }

            private void SnapSurface(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, int parentMesh)
            {
                Transform transform = m_TransformData[entity];
                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = entity;
                snapPosition.m_ElementIndex.x = parentMesh;
                snapPosition.m_Position = controlPoint.m_HitPosition;
                snapPosition.m_Direction = math.forward(transform.m_Rotation).xz;
                snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
                AddSnapPosition(ref bestPosition, snapPosition);
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
                float2 axisX =
                    math.normalize(
                        math.mul(rotation, new float3(1f, 0f, 0f)).xz
                    );

                float2 axisZ =
                    math.normalize(
                        math.mul(rotation, new float3(0f, 0f, 1f)).xz
                    );

                // Project OBB onto edge normal
                float offset =
                    math.abs(math.dot(axisX, normal)) * halfSize.x +
                    math.abs(math.dot(axisZ, normal)) * halfSize.y;



                // This shit doesn't work, can't slide the prefab on the line.
                // Project OBB onto edge direction (PARALLEL side length)
                float halfLength =
                     math.abs(math.dot(axisX, edgeDir)) * halfSize.x +
                     math.abs(math.dot(axisZ, edgeDir)) * halfSize.y;

                // Project mouse position onto edge.
                // controlPoint.m_Position is NOT the cursor's world position when the raycast hit an
                // object directly - RaycastSystem sets it to the HIT ENTITY's own transform position in
                // that case (m_HitPosition is the real ray/surface intersection point; the two only
                // coincide for a terrain hit, m_HitPosition = m_Position there). Using m_Position here
                // meant sliding along the edge was computed from the target's own center instead of the
                // cursor, which reads as "snaps to center" whenever the raycast actually hits the target's
                // geometry - exactly the case that doesn't go through the bounds-search fallback.
                MathUtils.Distance(line, controlPoint.m_HitPosition.xz, out float t);

                float lineLength = math.distance(line.a, line.b);
                t *= lineLength;
                //t = MathUtils.Snap(t, 0.5f);
                t = math.clamp(t, 0f - halfLength, lineLength + halfLength);

                // Position along the edge (THIS is what allows sliding)
                float2 pointOnLine = math.lerp(line.a, line.b, t / lineLength);

                // Final snapped position
                float centerProjection = math.dot(centerOffset, normal);
                float finalOffset = offset - centerProjection;
                float2 snappedXZ = pointOnLine + normal * finalOffset;

                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = Entity.Null;
                snapPosition.m_Position.xz = snappedXZ;
                snapPosition.m_Position.y = targetTransform.m_Position.y;

                // Rotation aligned to target edge
                snapPosition.m_Direction =
                    math.mul(
                        rotation,
                        new float3(0f, 0f, 1f)
                    ).xz;

                snapPosition.m_Rotation =
                    ToolUtils.CalculateRotation(snapPosition.m_Direction);

                float level = forceSnap ? 1f : 0f;
                snapPosition.m_SnapPriority =
                    ToolUtils.CalculateSnapPriority(
                        level,
                        1f,
                        0f,
                        controlPoint.m_HitPosition * 0.5f,
                        snapPosition.m_Position * 0.5f,
                        snapPosition.m_Direction
                    );

                AddSnapPosition(ref bestPosition, snapPosition);
            }

            // Same OBB-onto-edge-normal projection as CheckSnapLineGeneric, but the position along the
            // edge is fixed at the midpoint instead of following the cursor - still an edge snap, just
            // without the sliding. Whichever of the target's 4 edges ends up closest to the cursor wins
            // via the same AddSnapPosition priority comparison FreeMove uses.
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

                float centerProjection = math.dot(centerOffset, normal);
                float finalOffset = offset - centerProjection;
                float2 snappedXZ = pointOnLine + normal * finalOffset;

                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = Entity.Null;
                snapPosition.m_Position.xz = snappedXZ;
                snapPosition.m_Position.y = targetTransform.m_Position.y;

                snapPosition.m_Direction = math.mul(rotation, new float3(0f, 0f, 1f)).xz;
                snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);

                float level = forceSnap ? 1f : 0f;
                snapPosition.m_SnapPriority =
                    ToolUtils.CalculateSnapPriority(
                        level,
                        1f,
                        0f,
                        controlPoint.m_HitPosition * 0.5f,
                        snapPosition.m_Position * 0.5f,
                        snapPosition.m_Direction
                    );

                AddSnapPosition(ref bestPosition, snapPosition);
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

                float3 size = bounds.max - bounds.min;
                float2 halfSize = size.xz * 0.5f;

                float2 axisX = math.normalize(math.mul(rotation, new float3(1f, 0f, 0f)).xz);
                float2 axisZ = math.normalize(math.mul(rotation, new float3(0f, 0f, 1f)).xz);

                // Direction pointing away from the target's center, through this corner - the placed
                // object gets tucked into the corner along this diagonal.
                float2 outward = math.normalizesafe(corner - targetCenterXZ, new float2(1f, 0f));

                // Project the placed object's OBB half-extent onto that outward diagonal, same technique
                // CheckSnapLineGeneric uses to project onto an edge normal.
                float offset =
                    math.abs(math.dot(axisX, outward)) * halfSize.x +
                    math.abs(math.dot(axisZ, outward)) * halfSize.y;

                float2 snappedXZ = corner + outward * offset;

                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = Entity.Null;
                snapPosition.m_Position.xz = snappedXZ;
                snapPosition.m_Position.y = targetTransform.m_Position.y;

                snapPosition.m_Direction = math.mul(rotation, new float3(0f, 0f, 1f)).xz;
                snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);

                snapPosition.m_SnapPriority =
                    ToolUtils.CalculateSnapPriority(
                        1f,
                        1f,
                        0f,
                        controlPoint.m_HitPosition * 0.5f,
                        snapPosition.m_Position * 0.5f,
                        snapPosition.m_Direction
                    );

                AddSnapPosition(ref bestPosition, snapPosition);
            }


            private static void AddSnapPosition(ref ControlPoint bestSnapPosition, ControlPoint snapPosition)
            {
                if (ToolUtils.CompareSnapPriority(snapPosition.m_SnapPriority, bestSnapPosition.m_SnapPriority))
                {
                    bestSnapPosition = snapPosition;
                }
            }

            public static quaternion ClampTo90(quaternion q)
            {
                float3 forward = math.mul(q, new float3(0, 0, 1));
                float yaw = math.atan2(forward.x, forward.z);
                float snappedYaw = math.round(yaw / (math.PI / 2f)) * (math.PI / 2f);
                return quaternion.RotateY(snappedYaw);
            }

            public static void AlignRotation(ref quaternion rotation, quaternion parentRotation, bool zAxis)
            {
                if (zAxis)
                {
                    float3 forward = math.rotate(rotation, new float3(0f, 0f, 1f));
                    float3 up = math.rotate(parentRotation, new float3(0f, 1f, 0f));
                    quaternion a = quaternion.LookRotationSafe(forward, up);
                    quaternion q = rotation;
                    float num = float.MaxValue;
                    for (int i = 0; i < 8; i++)
                    {
                        quaternion quaternion = math.mul(a, quaternion.RotateZ((float)i * (MathF.PI / 4f)));
                        float num2 = MathUtils.RotationAngle(rotation, quaternion);
                        if (num2 < num)
                        {
                            q = quaternion;
                            num = num2;
                        }
                    }
                    rotation = math.normalizesafe(q, quaternion.identity);
                    return;
                }
                float3 forward2 = math.rotate(rotation, new float3(0f, 1f, 0f));
                float3 up2 = math.rotate(parentRotation, new float3(1f, 0f, 0f));
                quaternion a2 = math.mul(quaternion.LookRotationSafe(forward2, up2), quaternion.RotateX(MathF.PI / 2f));
                quaternion q2 = rotation;
                float num3 = float.MaxValue;
                for (int j = 0; j < 8; j++)
                {
                    quaternion quaternion2 = math.mul(a2, quaternion.RotateY((float)j * (MathF.PI / 4f)));
                    float num4 = MathUtils.RotationAngle(rotation, quaternion2);
                    if (num4 < num3)
                    {
                        q2 = quaternion2;
                        num3 = num4;
                    }
                }
                rotation = math.normalizesafe(q2, quaternion.identity);
            }

            private void AlignObject(ref ControlPoint controlPoint, ref quaternion parentRotation, bool alignRotation)
            {
                PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
                if (m_PlaceableObjectData.HasComponent(m_Prefab))
                {
                    placeableObjectData = m_PlaceableObjectData[m_Prefab];
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
                {
                    ObjectGeometryData objectGeometryData = m_ObjectGeometryData[m_Prefab];
                    controlPoint.m_Position.y -= objectGeometryData.m_Bounds.max.y;
                }
                parentRotation = quaternion.identity;
                if (m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    Entity entity = controlPoint.m_OriginalEntity;
                    PrefabRef prefabRef = m_PrefabRefData[entity];
                    parentRotation = m_TransformData[entity].m_Rotation;
                    while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(prefabRef.m_Prefab))
                    {
                        entity = m_OwnerData[entity].m_Owner;
                        prefabRef = m_PrefabRefData[entity];
                        if (m_TransformData.HasComponent(entity))
                        {
                            parentRotation = m_TransformData[entity].m_Rotation;
                        }
                    }
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
                {
                    float3 @float = math.forward(controlPoint.m_Rotation);
                    float3 value = controlPoint.m_HitDirection;
                    value.y = math.select(value.y, 0f, (m_Snap & ObjectToolExtraSnap.Upright) != 0);
                    if (!MathUtils.TryNormalize(ref value))
                    {
                        value = @float;
                        value.y = math.select(value.y, 0f, (m_Snap & ObjectToolExtraSnap.Upright) != 0);
                        if (!MathUtils.TryNormalize(ref value))
                        {
                            value = new float3(0f, 0f, 1f);
                        }
                    }
                    float3 value2 = math.cross(@float, value);
                    if (MathUtils.TryNormalize(ref value2))
                    {
                        float angle = math.acos(math.clamp(math.dot(@float, value), -1f, 1f));
                        controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value2, angle), controlPoint.m_Rotation), quaternion.identity);
                        if (alignRotation)
                        {
                            AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: true);
                        }
                    }
                    controlPoint.m_Position += math.forward(controlPoint.m_Rotation) * placeableObjectData.m_PlacementOffset.z;
                    return;
                }
                float3 float2 = math.rotate(controlPoint.m_Rotation, new float3(0f, 1f, 0f));
                float3 hitDirection = controlPoint.m_HitDirection;
                hitDirection = math.select(hitDirection, new float3(0f, 1f, 0f), (m_Snap & ObjectToolExtraSnap.Upright) != 0);
                if (!MathUtils.TryNormalize(ref hitDirection))
                {
                    hitDirection = float2;
                }
                float3 value3 = math.cross(float2, hitDirection);
                if (MathUtils.TryNormalize(ref value3))
                {
                    float angle2 = math.acos(math.clamp(math.dot(float2, hitDirection), -1f, 1f));
                    controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value3, angle2), controlPoint.m_Rotation), quaternion.identity);
                    if (alignRotation)
                    {
                        AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: false);
                    }
                }
            }

        }

        private static Entity GetUpgradable(Entity entity)
        {
            if (EL.m_EntityManager.TryGetComponent<Attached>(entity, out var component))
            {
                return component.m_Parent;
            }
            return entity;
        }

        // Must match the private ObjectToolSystem.Rotation struct's field layout exactly (order + types) -
        // GetRotationPtr() reinterprets the tool's real NativeReference<PrivateRotation> memory as this type.
        [StructLayout(LayoutKind.Sequential)]
        private struct Rotation
        {
            public quaternion m_Rotation;
            public quaternion m_ParentRotation;
            public bool m_IsAligned;
            public bool m_IsSnapped;

            // Old per-call approach: read a snapshot copy via reflection every frame. Kept for reference -
            // this only ever read the tool's rotation state, it never wrote back to it (see GetRotationPtr).
            //public Rotation(Traverse privateRotation)
            //{
            //    m_Rotation = privateRotation.Field("m_Rotation").GetValue<quaternion>();
            //    m_ParentRotation = privateRotation.Field("m_ParentRotation").GetValue<quaternion>();
            //    m_IsAligned = privateRotation.Field("m_IsAligned").GetValue<bool>();
            //    m_IsSnapped = privateRotation.Field("m_IsSnapped").GetValue<bool>();
            //}
        }

        // Old approach: snapshot-copy the tool's Rotation into our own struct via reflection, once per
        // frame. Read-only - any changes the job made to the copy were discarded, never written back to
        // the tool's real NativeReference<Rotation>. Superseded by GetRotationPtr(), which instead gives
        // the job direct read/write access to the tool's actual persistent-allocated memory.
        //private Rotation GetRotation()
        //{
        //    // m_Rotation is a NativeReference<T> where T is a private nested struct on ObjectToolSystem.
        //    return new Rotation(traverse.Field("m_Rotation").Property("Value"));
        //}

        // NativeReference<T>'s own layout never depends on T (just a raw pointer + allocator label), so
        // pinning the boxed NativeReference<PrivateRotation> and reading its m_Data pointer field via
        // Marshal (not FieldInfo - pointer-typed fields can't be reflected directly, GetValue throws)
        // gives us the address of the tool's actual persistent Rotation allocation. Cached once in the
        // ctor - Allocator.Persistent memory doesn't move.
        private IntPtr GetRotationPtr()
        {
            object boxedNativeRef = traverse.Field("m_Rotation").GetValue();
            Type nativeRefType = boxedNativeRef.GetType();

            GCHandle handle = GCHandle.Alloc(boxedNativeRef, GCHandleType.Pinned);
            try
            {
                IntPtr boxedDataAddr = handle.AddrOfPinnedObject();
                int dataOffset = (int)Marshal.OffsetOf(nativeRefType, "m_Data");
                return Marshal.ReadIntPtr(boxedDataAddr + dataOffset);
            }
            finally
            {
                handle.Free();
            }
        }

        private ObjectPrefab GetObjectPrefab()
        {
            return traverse.Method("GetObjectPrefab").GetValue<ObjectPrefab>();
        }

    }
}
