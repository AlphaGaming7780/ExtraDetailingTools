using Colossal.Collections;
using Colossal.Entities;
using ExtraDetailingTools.Snapping;
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
            m_SelectedSnap &= ~(ObjectToolExtraSnap.ObjectSide | ObjectToolExtraSnap.Upright);

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

            // Same modes as the vanilla snaps: placing a new object, or moving an existing one.
            if (mode != ObjectToolSystem.Mode.Create && mode != ObjectToolSystem.Mode.Move) return;

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

            SnapContext context = SnapContext.Create(m_Tool, m_TerrainSystem, m_WaterSystem, m_ObjectSearchSystem, out JobHandle contextDeps);

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

                m_StackData = m_Tool.GetComponentLookup<StackData>(true),
                m_Context = context,
            }, JobHandle.CombineDependencies(inputDeps, contextDeps));

            SnapContext.AddReaders(m_WaterSystem, m_ObjectSearchSystem, jobHandle);

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

            [ReadOnly] public ComponentLookup<StackData> m_StackData;

            public SnapContext m_Context;

            public IntPtr m_Rotation;

            // The only unsafe code needed for the rotation write-back - everywhere else uses this via
            // plain dot syntax, no unsafe context required at those call sites.
            private unsafe ref Rotation RotationRef => ref UnsafeUtility.AsRef<Rotation>((void*)m_Rotation);

            public void Execute()
            {
                ControlPoint controlPoint = m_LastRaycastPoint;
                ControlPoint bestSnapPosition = m_ControlPoints[m_ControlPoints.Length - 1];
                bool objectSurface = (m_Snap & ObjectToolExtraSnap.ObjectSurface) != ObjectToolExtraSnap.None;
                bool upright = (m_Snap & ObjectToolExtraSnap.Upright) != ObjectToolExtraSnap.None;

                if (objectSurface)
                {
                    SnapObjectSurface.SnapSurface(ref m_Context, controlPoint, ref bestSnapPosition);
                }

                if ((m_Snap & ObjectToolExtraSnap.ObjectSide) != ObjectToolExtraSnap.None && m_Context.m_ObjectGeometryData.TryGetComponent(m_Prefab, out var placedGeometryData))
                {
                    SnapObjectSide.Apply(ref m_Context, m_ObjectSideSnapMode, controlPoint, ref bestSnapPosition, RotationRef.m_Rotation, placedGeometryData);
                }

                //if (!m_EditorMode && (m_Snap & Snap.AutoParent) != Snap.None && controlPoint.m_OriginalEntity != Entity.Null)
                //{
                //    Entity entity2 = controlPoint.m_OriginalEntity;
                //    bestSnapPosition.m_OriginalEntity = entity2;
                //}

                SnapObjectSurface.CalculateHeight(ref m_Context, m_Prefab, ref bestSnapPosition);

                // ObjectSurface's tilt-to-surface-normal (below, via AlignToSurface) is a continuous,
                // automatic cosmetic adjustment recomputed fresh from the raycast every frame - not a
                // hard constraint the player is fighting. It's never bit-identical frame to frame on a
                // non-flat surface (wall, sloped roof, ...), so any change-detection here would read as
                // "still snapping" forever and permanently lock manual rotation input the moment the
                // surface isn't flat (GetAllowRotation() reads m_IsSnapped). Our extra snaps should never
                // block manual rotation, so this is intentionally always false.
                RotationRef.m_IsSnapped = false;
                RotationRef.m_IsAligned &= RotationRef.m_Rotation.Equals(bestSnapPosition.m_Rotation);

                SnapObjectSurface.AlignToSurface(ref m_Context, m_Prefab, objectSurface, upright, m_EditorMode, controlPoint, ref bestSnapPosition, ref RotationRef.m_ParentRotation, RotationRef.m_IsAligned);
                RotationRef.m_Rotation = bestSnapPosition.m_Rotation;

                if (m_StackData.TryGetComponent(m_Prefab, out var stackData) && stackData.m_Direction == StackDirection.Up)
                {
                    float stackOffset = stackData.m_FirstBounds.max + MathUtils.Size(stackData.m_MiddleBounds) * 2f - stackData.m_LastBounds.min;
                    bestSnapPosition.m_Elevation += stackOffset;
                    bestSnapPosition.m_Position.y += stackOffset;
                }

                m_ControlPoints[m_ControlPoints.Length - 1] = bestSnapPosition;
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
