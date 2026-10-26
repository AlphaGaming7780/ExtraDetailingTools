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

        // Address of the tool persistent NativeReference<Rotation> data, so the job writes the real rotation.
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

                // The surface tilt changes every frame, flagging it as snapped would lock manual rotation.
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

        // Same layout as the private ObjectToolSystem.Rotation struct.
        [StructLayout(LayoutKind.Sequential)]
        private struct Rotation
        {
            public quaternion m_Rotation;
            public quaternion m_ParentRotation;
            public bool m_IsAligned;
            public bool m_IsSnapped;
        }

        // NativeReference layout doesn't depend on T: read its m_Data pointer via Marshal (persistent, never moves).
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
