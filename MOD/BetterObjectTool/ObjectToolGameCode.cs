using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.PSI;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools
{
    // Token: 0x02000311 RID: 785
    [CompilerGenerated]
    public class ObjectToolSystem : ObjectToolBaseSystem
    {
        // Token: 0x17000243 RID: 579
        // (get) Token: 0x06000E3A RID: 3642 RVA: 0x0008C2CE File Offset: 0x0008A4CE
        public override string toolID
        {
            get
            {
                return "Object Tool";
            }
        }

        // Token: 0x17000244 RID: 580
        // (get) Token: 0x06000E3B RID: 3643 RVA: 0x0008C2D5 File Offset: 0x0008A4D5
        public override int uiModeIndex
        {
            get
            {
                return (int)this.actualMode;
            }
        }

        // Token: 0x06000E3C RID: 3644 RVA: 0x0008C2E0 File Offset: 0x0008A4E0
        public override void GetUIModes(List<ToolMode> modes)
        {
            ObjectToolSystem.Mode mode = this.mode;
            if (mode == ObjectToolSystem.Mode.Create || mode - ObjectToolSystem.Mode.Brush <= 1)
            {
                if (this.allowCreate)
                {
                    modes.Add(new ToolMode(ObjectToolSystem.Mode.Create.ToString(), 0));
                }
                if (this.allowBrush)
                {
                    modes.Add(new ToolMode(ObjectToolSystem.Mode.Brush.ToString(), 3));
                }
                if (this.allowStamp)
                {
                    modes.Add(new ToolMode(ObjectToolSystem.Mode.Stamp.ToString(), 4));
                }
            }
        }

        // Token: 0x17000245 RID: 581
        // (get) Token: 0x06000E3D RID: 3645 RVA: 0x0008C366 File Offset: 0x0008A566
        // (set) Token: 0x06000E3E RID: 3646 RVA: 0x0008C36E File Offset: 0x0008A56E
        public ObjectToolSystem.Mode mode { get; set; }

        // Token: 0x17000246 RID: 582
        // (get) Token: 0x06000E3F RID: 3647 RVA: 0x0008C378 File Offset: 0x0008A578
        public ObjectToolSystem.Mode actualMode
        {
            get
            {
                ObjectToolSystem.Mode mode = this.mode;
                if (!this.allowBrush && mode == ObjectToolSystem.Mode.Brush)
                {
                    mode = ObjectToolSystem.Mode.Create;
                }
                if (!this.allowStamp && mode == ObjectToolSystem.Mode.Stamp)
                {
                    mode = ObjectToolSystem.Mode.Create;
                }
                if (!this.allowCreate && this.allowBrush && mode == ObjectToolSystem.Mode.Create)
                {
                    mode = ObjectToolSystem.Mode.Brush;
                }
                if (!this.allowCreate && this.allowStamp && mode == ObjectToolSystem.Mode.Create)
                {
                    mode = ObjectToolSystem.Mode.Stamp;
                }
                return mode;
            }
        }

        // Token: 0x17000247 RID: 583
        // (get) Token: 0x06000E40 RID: 3648 RVA: 0x0008C3D3 File Offset: 0x0008A5D3
        // (set) Token: 0x06000E41 RID: 3649 RVA: 0x0008C3DB File Offset: 0x0008A5DB
        public AgeMask ageMask { get; set; }

        // Token: 0x17000248 RID: 584
        // (get) Token: 0x06000E42 RID: 3650 RVA: 0x0008C3E4 File Offset: 0x0008A5E4
        public AgeMask actualAgeMask
        {
            get
            {
                if (!this.allowAge)
                {
                    return AgeMask.Sapling;
                }
                if ((this.ageMask & (AgeMask.Sapling | AgeMask.Young | AgeMask.Mature | AgeMask.Elderly)) == (AgeMask)0)
                {
                    return AgeMask.Sapling;
                }
                return this.ageMask;
            }
        }

        // Token: 0x17000249 RID: 585
        // (get) Token: 0x06000E43 RID: 3651 RVA: 0x0008C403 File Offset: 0x0008A603
        // (set) Token: 0x06000E44 RID: 3652 RVA: 0x0008C40C File Offset: 0x0008A60C
        [CanBeNull]
        public ObjectPrefab prefab
        {
            get
            {
                return this.m_SelectedPrefab;
            }
            set
            {
                if (value != this.m_SelectedPrefab)
                {
                    this.m_SelectedPrefab = value;
                    this.m_ForceUpdate = true;
                    this.allowCreate = true;
                    this.allowBrush = false;
                    this.allowStamp = false;
                    this.allowAge = false;
                    if (value != null)
                    {
                        this.m_TransformPrefab = null;
                        ObjectGeometryData objectGeometryData;
                        if (this.m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(this.m_SelectedPrefab, out objectGeometryData))
                        {
                            this.allowBrush = ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Brushable) > Game.Objects.GeometryFlags.None);
                            this.allowStamp = ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Stampable) > Game.Objects.GeometryFlags.None);
                            this.allowCreate = (!this.allowStamp || this.m_ToolSystem.actionMode.IsEditor());
                        }
                        this.allowAge = (this.m_ToolSystem.actionMode.IsGame() && this.m_PrefabSystem.HasComponent<TreeData>(this.m_SelectedPrefab));
                    }
                    Action<PrefabBase> eventPrefabChanged = this.m_ToolSystem.EventPrefabChanged;
                    if (eventPrefabChanged == null)
                    {
                        return;
                    }
                    eventPrefabChanged(value);
                }
            }
        }

        // Token: 0x1700024A RID: 586
        // (get) Token: 0x06000E45 RID: 3653 RVA: 0x0008C509 File Offset: 0x0008A709
        // (set) Token: 0x06000E46 RID: 3654 RVA: 0x0008C514 File Offset: 0x0008A714
        public TransformPrefab transform
        {
            get
            {
                return this.m_TransformPrefab;
            }
            set
            {
                if (value != this.m_TransformPrefab)
                {
                    this.m_TransformPrefab = value;
                    this.m_ForceUpdate = true;
                    if (value != null)
                    {
                        this.m_SelectedPrefab = null;
                        this.allowCreate = true;
                        this.allowBrush = false;
                        this.allowStamp = false;
                        this.allowAge = false;
                    }
                    Action<PrefabBase> eventPrefabChanged = this.m_ToolSystem.EventPrefabChanged;
                    if (eventPrefabChanged == null)
                    {
                        return;
                    }
                    eventPrefabChanged(value);
                }
            }
        }

        // Token: 0x1700024B RID: 587
        // (get) Token: 0x06000E47 RID: 3655 RVA: 0x0008C57F File Offset: 0x0008A77F
        // (set) Token: 0x06000E48 RID: 3656 RVA: 0x0008C587 File Offset: 0x0008A787
        public bool underground { get; set; }

        // Token: 0x1700024C RID: 588
        // (get) Token: 0x06000E49 RID: 3657 RVA: 0x0008C590 File Offset: 0x0008A790
        // (set) Token: 0x06000E4A RID: 3658 RVA: 0x0008C598 File Offset: 0x0008A798
        public bool allowCreate { get; private set; }

        // Token: 0x1700024D RID: 589
        // (get) Token: 0x06000E4B RID: 3659 RVA: 0x0008C5A1 File Offset: 0x0008A7A1
        // (set) Token: 0x06000E4C RID: 3660 RVA: 0x0008C5A9 File Offset: 0x0008A7A9
        public bool allowBrush { get; private set; }

        // Token: 0x1700024E RID: 590
        // (get) Token: 0x06000E4D RID: 3661 RVA: 0x0008C5B2 File Offset: 0x0008A7B2
        // (set) Token: 0x06000E4E RID: 3662 RVA: 0x0008C5BA File Offset: 0x0008A7BA
        public bool allowStamp { get; private set; }

        // Token: 0x1700024F RID: 591
        // (get) Token: 0x06000E4F RID: 3663 RVA: 0x0008C5C3 File Offset: 0x0008A7C3
        // (set) Token: 0x06000E50 RID: 3664 RVA: 0x0008C5CB File Offset: 0x0008A7CB
        public bool allowAge { get; private set; }

        // Token: 0x17000250 RID: 592
        // (get) Token: 0x06000E51 RID: 3665 RVA: 0x0008C5D4 File Offset: 0x0008A7D4
        public override bool brushing
        {
            get
            {
                return this.actualMode == ObjectToolSystem.Mode.Brush;
            }
        }

        // Token: 0x06000E52 RID: 3666 RVA: 0x0008C5E0 File Offset: 0x0008A7E0
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            this.m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
            this.m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
            this.m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
            this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            this.m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
            this.m_DefinitionQuery = base.GetDefinitionQuery();
            this.m_ContainerQuery = base.GetContainerQuery();
            this.m_BrushQuery = base.GetBrushQuery();
            this.m_ControlPoints = new NativeList<ControlPoint>(1, Allocator.Persistent);
            this.m_Rotation = new NativeValue<ObjectToolSystem.Rotation>(Allocator.Persistent);
            ObjectToolSystem.Rotation value = default(ObjectToolSystem.Rotation);
            value.m_Rotation = quaternion.identity;
            value.m_ParentRotation = quaternion.identity;
            value.m_IsAligned = true;
            this.m_Rotation.value = value;
            this.m_SoundQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<ToolUXSoundSettingsData>()
            });
            this.m_LotQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Game.Areas.Lot>(),
                ComponentType.ReadOnly<Temp>()
            });
            this.m_BuildingQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<BuildingSpawnGroupData>(),
                ComponentType.ReadOnly<PrefabData>()
            });
            this.m_TempQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Temp>()
            });
            this.m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            this.m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");
            this.m_CancelAction = InputManager.instance.FindAction("Tool", "Mouse Cancel");
            this.m_ApplyDisplayOverride = new DisplayNameOverride("RouteToolSystem", this.m_ApplyAction, null, 20);
            this.m_SecondaryApplyDisplayOverride = new DisplayNameOverride("RouteToolSystem", this.m_SecondaryApplyAction, null, 25);
            base.brushSize = 200f;
            base.brushAngle = 0f;
            base.brushStrength = 0.5f;
            this.selectedSnap &= ~(Snap.AutoParent | Snap.ContourLines);
            this.ageMask = AgeMask.Sapling;
        }

        // Token: 0x06000E53 RID: 3667 RVA: 0x0008C864 File Offset: 0x0008AA64
        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            base.brushType = base.FindDefaultBrush(this.m_BrushQuery);
            base.brushSize = 200f;
            base.brushAngle = 0f;
            base.brushStrength = 0.5f;
        }

        // Token: 0x06000E54 RID: 3668 RVA: 0x0008C8A0 File Offset: 0x0008AAA0
        [Preserve]
        protected override void OnDestroy()
        {
            this.m_ControlPoints.Dispose();
            this.m_Rotation.Dispose();
            base.OnDestroy();
        }

        // Token: 0x06000E55 RID: 3669 RVA: 0x0008C8C0 File Offset: 0x0008AAC0
        [Preserve]
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.m_ControlPoints.Clear();
            this.m_LastRaycastPoint = default(ControlPoint);
            this.m_StartPoint = default(ControlPoint);
            this.m_State = ObjectToolSystem.State.Default;
            this.m_MovingInitialized = Entity.Null;
            this.m_ForceCancel = false;
            this.Randomize();
            base.requireZones = false;
            base.requireUnderground = false;
            base.requireNetArrows = false;
            base.requireAreas = AreaTypeMask.Lots;
            base.requireNet = Layer.None;
            if (this.m_ToolSystem.actionMode.IsEditor())
            {
                base.requireAreas |= AreaTypeMask.Spaces;
            }
            this.UpdateActions(true);
        }

        // Token: 0x06000E56 RID: 3670 RVA: 0x0008C960 File Offset: 0x0008AB60
        [Preserve]
        protected override void OnStopRunning()
        {
            this.m_ApplyAction.enabled = false;
            this.m_SecondaryApplyAction.enabled = false;
            this.m_CancelAction.enabled = false;
            this.m_ApplyDisplayOverride.state = DisplayNameOverride.State.Off;
            this.m_SecondaryApplyDisplayOverride.state = DisplayNameOverride.State.Off;
            base.OnStopRunning();
        }

        // Token: 0x06000E57 RID: 3671 RVA: 0x0008C9B0 File Offset: 0x0008ABB0
        public override PrefabBase GetPrefab()
        {
            ObjectToolSystem.Mode actualMode = this.actualMode;
            if (actualMode != ObjectToolSystem.Mode.Create && actualMode - ObjectToolSystem.Mode.Brush > 1)
            {
                return null;
            }
            if (!(this.prefab != null))
            {
                return this.transform;
            }
            return this.prefab;
        }

        // Token: 0x06000E58 RID: 3672 RVA: 0x0008C9EA File Offset: 0x0008ABEA
        protected override bool GetAllowApply()
        {
            return base.GetAllowApply() && !this.m_TempQuery.IsEmptyIgnoreFilter;
        }

        // Token: 0x06000E59 RID: 3673 RVA: 0x0008CA04 File Offset: 0x0008AC04
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            ObjectPrefab objectPrefab = prefab as ObjectPrefab;
            if (objectPrefab != null)
            {
                ObjectToolSystem.Mode mode = this.mode;
                if (!this.m_ToolSystem.actionMode.IsEditor() && prefab.Has<Game.Prefabs.ServiceUpgrade>())
                {
                    Entity entity = this.m_PrefabSystem.GetEntity(prefab);
                    base.CheckedStateRef.EntityManager.CompleteDependencyBeforeRO<PlaceableObjectData>();
                    this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup.Update(base.CheckedStateRef);
                    if (!this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup.HasComponent(entity))
                    {
                        return false;
                    }
                    mode = ObjectToolSystem.Mode.Upgrade;
                }
                else if (mode == ObjectToolSystem.Mode.Upgrade || mode == ObjectToolSystem.Mode.Move)
                {
                    mode = ObjectToolSystem.Mode.Create;
                }
                this.prefab = objectPrefab;
                this.mode = mode;
                return true;
            }
            TransformPrefab transformPrefab = prefab as TransformPrefab;
            if (transformPrefab != null)
            {
                this.transform = transformPrefab;
                this.mode = ObjectToolSystem.Mode.Create;
                return true;
            }
            return false;
        }

        // Token: 0x06000E5A RID: 3674 RVA: 0x0008CAC8 File Offset: 0x0008ACC8
        public void StartMoving(Entity movingObject)
        {
            this.m_MovingObject = movingObject;
            Owner owner;
            if (this.m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(this.m_MovingObject) && base.EntityManager.TryGetComponent(this.m_MovingObject, out owner))
            {
                this.m_MovingObject = owner.m_Owner;
            }
            this.mode = ObjectToolSystem.Mode.Move;
            this.prefab = this.m_PrefabSystem.GetPrefab<ObjectPrefab>(base.EntityManager.GetComponentData<PrefabRef>(this.m_MovingObject));
        }

        // Token: 0x06000E5B RID: 3675 RVA: 0x0008CB54 File Offset: 0x0008AD54
        private void Randomize()
        {
            this.m_RandomSeed = RandomSeed.Next();
            PlaceableObjectData placeableObjectData;
            if (this.m_SelectedPrefab != null && this.m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(this.m_SelectedPrefab, out placeableObjectData) && placeableObjectData.m_RotationSymmetry != RotationSymmetry.None)
            {
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(567890109);
                ObjectToolSystem.Rotation value = this.m_Rotation.value;
                float num = 6.2831855f;
                if (placeableObjectData.m_RotationSymmetry == RotationSymmetry.Any)
                {
                    num = random.NextFloat(num);
                    value.m_IsAligned = false;
                }
                else
                {
                    num *= (float)random.NextInt((int)placeableObjectData.m_RotationSymmetry) / (float)placeableObjectData.m_RotationSymmetry;
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
                {
                    value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateZ(num)), quaternion.identity);
                    if (value.m_IsAligned)
                    {
                        ObjectToolSystem.SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, true);
                    }
                }
                else
                {
                    value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateY(num)), quaternion.identity);
                    if (value.m_IsAligned)
                    {
                        ObjectToolSystem.SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, false);
                    }
                }
                this.m_Rotation.value = value;
            }
        }

        // Token: 0x06000E5C RID: 3676 RVA: 0x0008CC90 File Offset: 0x0008AE90
        private ObjectPrefab GetObjectPrefab()
        {
            Entity entity;
            Entity entity2;
            if (this.m_ToolSystem.actionMode.IsEditor() && this.m_TransformPrefab != null && base.GetContainers(this.m_ContainerQuery, out entity, out entity2))
            {
                return this.m_PrefabSystem.GetPrefab<ObjectPrefab>(entity2);
            }
            if (this.actualMode == ObjectToolSystem.Mode.Move)
            {
                Entity entity3 = this.m_MovingObject;
                Owner owner;
                if (this.m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(entity3) && base.EntityManager.TryGetComponent(entity3, out owner))
                {
                    entity3 = owner.m_Owner;
                }
                PrefabRef refData;
                if (base.EntityManager.TryGetComponent(entity3, out refData))
                {
                    return this.m_PrefabSystem.GetPrefab<ObjectPrefab>(refData);
                }
            }
            return this.m_SelectedPrefab;
        }

        // Token: 0x06000E5D RID: 3677 RVA: 0x0008CD4B File Offset: 0x0008AF4B
        public override void SetUnderground(bool underground)
        {
            this.underground = underground;
        }

        // Token: 0x06000E5E RID: 3678 RVA: 0x0008CD54 File Offset: 0x0008AF54
        public override void ElevationUp()
        {
            this.underground = false;
        }

        // Token: 0x06000E5F RID: 3679 RVA: 0x0008CD5D File Offset: 0x0008AF5D
        public override void ElevationDown()
        {
            this.underground = true;
        }

        // Token: 0x06000E60 RID: 3680 RVA: 0x0008CD66 File Offset: 0x0008AF66
        public override void ElevationScroll()
        {
            this.underground = !this.underground;
        }

        // Token: 0x06000E61 RID: 3681 RVA: 0x0008CD78 File Offset: 0x0008AF78
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            this.m_Prefab = this.GetObjectPrefab();
            if (this.m_Prefab != null)
            {
                float3 rayOffset = default(float3);
                Bounds3 bounds = default(Bounds3);
                ObjectGeometryData objectGeometryData;
                if (this.m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(this.m_Prefab, out objectGeometryData))
                {
                    rayOffset.y -= objectGeometryData.m_Pivot.y;
                    bounds = objectGeometryData.m_Bounds;
                }
                PlaceableObjectData placeableObjectData;
                if (this.m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(this.m_Prefab, out placeableObjectData))
                {
                    rayOffset.y -= placeableObjectData.m_PlacementOffset.y;
                    if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
                    {
                        rayOffset.y += bounds.max.y;
                    }
                }
                this.m_ToolRaycastSystem.rayOffset = rayOffset;
                Snap onMask;
                Snap offMask;
                this.GetAvailableSnapMask(out onMask, out offMask);
                Snap actualSnap = ToolBaseSystem.GetActualSnap(this.selectedSnap, onMask, offMask);
                if ((actualSnap & (Snap.NetArea | Snap.NetNode)) != Snap.None)
                {
                    this.m_ToolRaycastSystem.typeMask |= TypeMask.Net;
                    this.m_ToolRaycastSystem.netLayerMask |= (Layer.Road | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad);
                }
                if ((actualSnap & Snap.ObjectSurface) != Snap.None)
                {
                    this.m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    if (this.m_ToolSystem.actionMode.IsEditor())
                    {
                        this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }
                }
                if ((actualSnap & (Snap.NetArea | Snap.NetNode | Snap.ObjectSurface)) != Snap.None && !this.m_PrefabSystem.HasComponent<BuildingData>(this.m_Prefab))
                {
                    if (this.underground)
                    {
                        this.m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
                        this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.PartialSurface;
                    }
                    else
                    {
                        this.m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
                        if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
                        {
                            this.m_ToolRaycastSystem.typeMask |= TypeMask.Water;
                        }
                        this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
                        this.m_ToolRaycastSystem.collisionMask = (CollisionMask.OnGround | CollisionMask.Overground);
                    }
                }
                else
                {
                    this.m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
                    if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
                    {
                        this.m_ToolRaycastSystem.typeMask |= TypeMask.Water;
                    }
                    this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
                    this.m_ToolRaycastSystem.netLayerMask |= Layer.None;
                }
            }
            else
            {
                this.m_ToolRaycastSystem.typeMask = (TypeMask.Terrain | TypeMask.Water);
                this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
                this.m_ToolRaycastSystem.netLayerMask = Layer.None;
                this.m_ToolRaycastSystem.rayOffset = default(float3);
            }
            if (this.m_ToolSystem.actionMode.IsEditor())
            {
                this.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
            }
        }

        // Token: 0x06000E62 RID: 3682 RVA: 0x0008D050 File Offset: 0x0008B250
        private void InitializeRotation(Entity entity, PlaceableObjectData placeableObjectData)
        {
            ObjectToolSystem.Rotation rotation = default(ObjectToolSystem.Rotation);
            rotation.m_Rotation = quaternion.identity;
            rotation.m_ParentRotation = quaternion.identity;
            rotation.m_IsAligned = true;
            ObjectToolSystem.Rotation rotation2 = rotation;
            Game.Objects.Transform transform;
            if (base.EntityManager.TryGetComponent(entity, out transform))
            {
                rotation2.m_Rotation = transform.m_Rotation;
            }
            Owner owner;
            if (base.EntityManager.TryGetComponent(entity, out owner))
            {
                Entity owner2 = owner.m_Owner;
                Game.Objects.Transform transform2;
                if (base.EntityManager.TryGetComponent(owner2, out transform2))
                {
                    rotation2.m_ParentRotation = transform2.m_Rotation;
                }
                while (base.EntityManager.TryGetComponent(owner2, out owner) && !base.EntityManager.HasComponent<Building>(owner2))
                {
                    owner2 = owner.m_Owner;
                    if (base.EntityManager.TryGetComponent(owner2, out transform2))
                    {
                        rotation2.m_ParentRotation = transform2.m_Rotation;
                    }
                }
            }
            quaternion rotation3 = rotation2.m_Rotation;
            if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
            {
                ObjectToolSystem.SnapJob.AlignRotation(ref rotation3, rotation2.m_ParentRotation, true);
            }
            else
            {
                ObjectToolSystem.SnapJob.AlignRotation(ref rotation3, rotation2.m_ParentRotation, false);
            }
            if (MathUtils.RotationAngle(rotation2.m_Rotation, rotation3) > 0.01f)
            {
                rotation2.m_IsAligned = false;
            }
            this.m_Rotation.value = rotation2;
        }

        // Token: 0x06000E63 RID: 3683 RVA: 0x0008D184 File Offset: 0x0008B384
        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            this.m_UpgradingObject = Entity.Null;
            ObjectToolSystem.Mode actualMode = this.actualMode;
            if (actualMode == ObjectToolSystem.Mode.Brush && base.brushType == null)
            {
                base.brushType = base.FindDefaultBrush(this.m_BrushQuery);
            }
            if (actualMode != ObjectToolSystem.Mode.Move)
            {
                this.m_MovingObject = Entity.Null;
            }
            bool forceCancel = this.m_ForceCancel;
            this.m_ForceCancel = false;
            CameraController cameraController;
            if (this.m_CameraController == null && CameraController.TryGet(out cameraController))
            {
                this.m_CameraController = cameraController;
            }
            this.UpdateActions(false);
            if (this.m_Prefab != null)
            {
                this.allowUnderground = false;
                base.requireUnderground = false;
                base.requireNet = Layer.None;
                base.requireNetArrows = false;
                base.requireStops = TransportType.None;
                base.UpdateInfoview(this.m_ToolSystem.actionMode.IsEditor() ? Entity.Null : this.m_PrefabSystem.GetEntity(this.m_Prefab));
                this.GetAvailableSnapMask(out this.m_SnapOnMask, out this.m_SnapOffMask);
                ObjectGeometryData objectGeometryData;
                this.m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(this.m_Prefab, out objectGeometryData);
                PlaceableObjectData placeableObjectData;
                if (this.m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(this.m_Prefab, out placeableObjectData))
                {
                    if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.HasUndergroundElements) != Game.Objects.PlacementFlags.None)
                    {
                        base.requireNet |= Layer.Road;
                    }
                    if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) != Game.Objects.PlacementFlags.None)
                    {
                        base.requireNet |= Layer.Waterway;
                    }
                }
                if (actualMode != ObjectToolSystem.Mode.Upgrade)
                {
                    if (actualMode == ObjectToolSystem.Mode.Move)
                    {
                        if (!base.EntityManager.Exists(this.m_MovingObject))
                        {
                            this.m_MovingObject = Entity.Null;
                        }
                        if (this.m_MovingInitialized != this.m_MovingObject)
                        {
                            this.m_MovingInitialized = this.m_MovingObject;
                            this.InitializeRotation(this.m_MovingObject, placeableObjectData);
                        }
                    }
                }
                else if (this.m_PrefabSystem.HasComponent<ServiceUpgradeData>(this.m_Prefab))
                {
                    this.m_UpgradingObject = this.m_ToolSystem.selected;
                }
                if ((ToolBaseSystem.GetActualSnap(this.selectedSnap, this.m_SnapOnMask, this.m_SnapOffMask) & (Snap.NetArea | Snap.NetNode | Snap.ObjectSurface)) != Snap.None && !this.m_PrefabSystem.HasComponent<BuildingData>(this.m_Prefab))
                {
                    this.allowUnderground = true;
                }
                TransportStopData transportStopData;
                if (this.m_PrefabSystem.TryGetComponentData<TransportStopData>(this.m_Prefab, out transportStopData))
                {
                    base.requireNetArrows = (transportStopData.m_TransportType != TransportType.Post);
                    base.requireStops = transportStopData.m_TransportType;
                }
                base.requireUnderground = (this.allowUnderground && this.underground);
                base.requireZones = (!base.requireUnderground && ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None || ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.OccupyZone) != Game.Objects.GeometryFlags.None && base.requireStops == TransportType.None)));
                if (this.m_State != ObjectToolSystem.State.Default && !this.m_ApplyAction.enabled)
                {
                    this.m_State = ObjectToolSystem.State.Default;
                }
                if ((this.m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == (RaycastFlags)0U)
                {
                    if (this.m_CancelAction.WasPressedThisFrame())
                    {
                        if (actualMode == ObjectToolSystem.Mode.Upgrade && (this.m_SnapOffMask & Snap.OwnerSide) == Snap.None)
                        {
                            this.m_ToolSystem.activeTool = this.m_DefaultToolSystem;
                        }
                        return this.Cancel(inputDeps, this.m_CancelAction.WasReleasedThisFrame());
                    }
                    if (this.m_State == ObjectToolSystem.State.Adding || this.m_State == ObjectToolSystem.State.Removing)
                    {
                        if (this.m_ApplyAction.WasPressedThisFrame() || this.m_ApplyAction.WasReleasedThisFrame())
                        {
                            return this.Apply(inputDeps, false);
                        }
                        if (forceCancel || this.m_SecondaryApplyAction.WasPressedThisFrame() || this.m_SecondaryApplyAction.WasReleasedThisFrame())
                        {
                            return this.Cancel(inputDeps, false);
                        }
                        return this.Update(inputDeps);
                    }
                    else
                    {
                        if ((actualMode != ObjectToolSystem.Mode.Upgrade || (this.m_SnapOffMask & Snap.OwnerSide) != Snap.None) && this.m_SecondaryApplyAction.WasPressedThisFrame())
                        {
                            return this.Cancel(inputDeps, this.m_SecondaryApplyAction.WasReleasedThisFrame());
                        }
                        if (this.m_State == ObjectToolSystem.State.Rotating && this.m_SecondaryApplyAction.WasReleasedThisFrame())
                        {
                            this.StopRotating();
                            return this.Update(inputDeps);
                        }
                        if (this.m_ApplyAction.WasPressedThisFrame())
                        {
                            if (actualMode == ObjectToolSystem.Mode.Move)
                            {
                                this.m_ToolSystem.activeTool = this.m_DefaultToolSystem;
                                this.m_TerrainSystem.OnBuildingMoved(this.m_MovingObject);
                            }
                            return this.Apply(inputDeps, this.m_ApplyAction.WasReleasedThisFrame());
                        }
                        if (this.m_State == ObjectToolSystem.State.Rotating)
                        {
                            InputManager.ControlScheme activeControlScheme = InputManager.instance.activeControlScheme;
                            if (activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse)
                            {
                                float3 @float = InputManager.instance.mousePosition;
                                if (@float.x != this.m_RotationStartPosition.x)
                                {
                                    ObjectToolSystem.Rotation value = this.m_Rotation.value;
                                    float angle = (@float.x - this.m_RotationStartPosition.x) * 6.2831855f * 0.002f;
                                    if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
                                    {
                                        value.m_Rotation = math.normalizesafe(math.mul(this.m_StartRotation, quaternion.RotateZ(angle)), quaternion.identity);
                                    }
                                    else
                                    {
                                        value.m_Rotation = math.normalizesafe(math.mul(this.m_StartRotation, quaternion.RotateY(angle)), quaternion.identity);
                                    }
                                    this.m_RotationModified = true;
                                    value.m_IsAligned = false;
                                    this.m_Rotation.value = value;
                                }
                            }
                            else if (activeControlScheme == InputManager.ControlScheme.Gamepad && (placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) == Game.Objects.PlacementFlags.None)
                            {
                                float num = math.radians(this.cameraAngle - this.m_StartCameraAngle);
                                if (this.m_RotationModified || math.abs(num) > 0.01f)
                                {
                                    ObjectToolSystem.Rotation value2 = this.m_Rotation.value;
                                    value2.m_Rotation = math.normalizesafe(math.mul(this.m_StartRotation, quaternion.RotateY(num)), quaternion.identity);
                                    this.m_RotationModified = true;
                                    value2.m_IsAligned = false;
                                    this.m_Rotation.value = value2;
                                }
                            }
                        }
                        return this.Update(inputDeps);
                    }
                }
            }
            else
            {
                base.requireUnderground = false;
                base.requireZones = false;
                base.requireNetArrows = false;
                base.requireNet = Layer.None;
                base.UpdateInfoview(Entity.Null);
            }
            if (this.m_State != ObjectToolSystem.State.Default && (this.m_ApplyAction.WasReleasedThisFrame() || this.m_SecondaryApplyAction.WasReleasedThisFrame()))
            {
                this.m_State = ObjectToolSystem.State.Default;
            }
            return this.Clear(inputDeps);
        }

        // Token: 0x06000E64 RID: 3684 RVA: 0x0008D764 File Offset: 0x0008B964
        public override void ToggleToolOptions(bool enabled)
        {
            this.m_ApplyAction.enabled = !enabled;
            this.m_SecondaryApplyAction.enabled = !enabled;
            this.m_ApplyDisplayOverride.state = (this.m_ApplyAction.enabled ? DisplayNameOverride.State.GlobalHint : DisplayNameOverride.State.Off);
            this.m_SecondaryApplyDisplayOverride.state = (this.m_SecondaryApplyAction.enabled ? DisplayNameOverride.State.GlobalHint : DisplayNameOverride.State.Off);
        }

        // Token: 0x06000E65 RID: 3685 RVA: 0x0008D7C8 File Offset: 0x0008B9C8
        private void UpdateActions(bool forceEnabled)
        {
            if (forceEnabled)
            {
                this.m_ApplyAction.enabled = true;
                this.m_SecondaryApplyAction.enabled = true;
                this.m_CancelAction.enabled = (this.actualMode == ObjectToolSystem.Mode.Upgrade);
                this.m_ApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
                this.m_SecondaryApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
            }
            else
            {
                this.m_CancelAction.enabled = (this.m_ApplyAction.enabled && this.actualMode == ObjectToolSystem.Mode.Upgrade);
            }
            if (this.actualMode == ObjectToolSystem.Mode.Upgrade)
            {
                this.m_ApplyDisplayOverride.displayName = "Place Upgrade";
                this.m_SecondaryApplyDisplayOverride.displayName = "Rotate Object";
                return;
            }
            if (this.actualMode == ObjectToolSystem.Mode.Move)
            {
                this.m_ApplyDisplayOverride.displayName = "Move Object";
                this.m_SecondaryApplyDisplayOverride.displayName = "Rotate Object";
                return;
            }
            if (this.actualMode == ObjectToolSystem.Mode.Brush)
            {
                this.m_ApplyDisplayOverride.displayName = "Paint Object";
                this.m_SecondaryApplyDisplayOverride.displayName = "Erase Object";
                return;
            }
            this.m_ApplyDisplayOverride.displayName = "Place Object";
            this.m_SecondaryApplyDisplayOverride.displayName = "Rotate Object";
        }

        // Token: 0x06000E66 RID: 3686 RVA: 0x0008D8E0 File Offset: 0x0008BAE0
        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            if (!(this.m_Prefab != null))
            {
                base.GetAvailableSnapMask(out onMask, out offMask);
                return;
            }
            ObjectToolSystem.Mode actualMode = this.actualMode;
            bool flag = this.m_PrefabSystem.HasComponent<BuildingData>(this.m_Prefab);
            bool isAssetStamp = !flag && this.m_PrefabSystem.HasComponent<AssetStampData>(this.m_Prefab);
            bool brushing = actualMode == ObjectToolSystem.Mode.Brush;
            bool stamping = actualMode == ObjectToolSystem.Mode.Stamp;
            if (this.m_PrefabSystem.HasComponent<PlaceableObjectData>(this.m_Prefab))
            {
                ObjectToolSystem.GetAvailableSnapMask(this.m_PrefabSystem.GetComponentData<PlaceableObjectData>(this.m_Prefab), this.m_ToolSystem.actionMode.IsEditor(), flag, isAssetStamp, brushing, stamping, out onMask, out offMask);
                return;
            }
            ObjectToolSystem.GetAvailableSnapMask(default(PlaceableObjectData), this.m_ToolSystem.actionMode.IsEditor(), flag, isAssetStamp, brushing, stamping, out onMask, out offMask);
        }

        // Token: 0x06000E67 RID: 3687 RVA: 0x0008D9A8 File Offset: 0x0008BBA8
        private static void GetAvailableSnapMask(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, bool brushing, bool stamping, out Snap onMask, out Snap offMask)
        {
            onMask = Snap.Upright;
            offMask = Snap.None;
            if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.OwnerSide)) == Game.Objects.PlacementFlags.OwnerSide)
            {
                onMask |= Snap.OwnerSide;
            }
            else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
            {
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.OwnerSide) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.OwnerSide;
                    offMask |= Snap.OwnerSide;
                }
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.NetSide;
                    offMask |= Snap.NetSide;
                }
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadEdge) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.NetArea;
                    offMask |= Snap.NetArea;
                }
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.Shoreline;
                    offMask |= Snap.Shoreline;
                }
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.ObjectSurface;
                    offMask |= Snap.ObjectSurface;
                }
            }
            else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadNode | Game.Objects.PlacementFlags.RoadEdge)) != Game.Objects.PlacementFlags.None)
            {
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.NetNode;
                }
                if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadEdge) != Game.Objects.PlacementFlags.None)
                {
                    onMask |= Snap.NetArea;
                }
            }
            else if (editorMode && !isBuilding)
            {
                onMask |= Snap.ObjectSurface;
                offMask |= Snap.ObjectSurface;
                offMask |= Snap.Upright;
            }
            if (editorMode && (!isAssetStamp || stamping))
            {
                onMask |= Snap.AutoParent;
                offMask |= Snap.AutoParent;
            }
            if (brushing)
            {
                onMask &= Snap.Upright;
                offMask &= Snap.Upright;
                onMask |= Snap.PrefabType;
                offMask |= Snap.PrefabType;
            }
            if (isBuilding || isAssetStamp)
            {
                onMask |= Snap.ContourLines;
                offMask |= Snap.ContourLines;
            }
        }

        // Token: 0x06000E68 RID: 3688 RVA: 0x00070652 File Offset: 0x0006E852
        private JobHandle Clear(JobHandle inputDeps)
        {
            base.applyMode = ApplyMode.Clear;
            return inputDeps;
        }

        // Token: 0x06000E69 RID: 3689 RVA: 0x0008DB6C File Offset: 0x0008BD6C
        private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
        {
            if (this.actualMode != ObjectToolSystem.Mode.Brush)
            {
                if (this.actualMode != ObjectToolSystem.Mode.Upgrade || (this.m_SnapOffMask & Snap.OwnerSide) != Snap.None)
                {
                    this.m_State = ObjectToolSystem.State.Rotating;
                    this.m_RotationModified = false;
                    this.m_RotationStartPosition = InputManager.instance.mousePosition;
                    this.m_StartRotation = this.m_Rotation.value.m_Rotation;
                    this.m_StartCameraAngle = this.cameraAngle;
                    if (singleFrameOnly)
                    {
                        this.StopRotating();
                    }
                }
                base.applyMode = ApplyMode.Clear;
                this.m_ControlPoints.Clear();
                ControlPoint controlPoint;
                if (this.GetRaycastResult(out controlPoint))
                {
                    controlPoint.m_Rotation = this.m_Rotation.value.m_Rotation;
                    this.m_ControlPoints.Add(controlPoint);
                    inputDeps = this.SnapControlPoint(inputDeps);
                    inputDeps = this.UpdateDefinitions(inputDeps);
                }
                return inputDeps;
            }
            if (this.m_State == ObjectToolSystem.State.Default)
            {
                base.applyMode = ApplyMode.Clear;
                this.Randomize();
                this.m_StartPoint = this.m_LastRaycastPoint;
                this.m_State = ObjectToolSystem.State.Removing;
                this.m_ForceCancel = singleFrameOnly;
                this.GetRaycastResult(out this.m_LastRaycastPoint);
                return this.UpdateDefinitions(inputDeps);
            }
            if (this.m_State == ObjectToolSystem.State.Removing && this.GetAllowApply())
            {
                base.applyMode = ApplyMode.Apply;
                this.Randomize();
                this.m_StartPoint = default(ControlPoint);
                this.m_State = ObjectToolSystem.State.Default;
                this.GetRaycastResult(out this.m_LastRaycastPoint);
                return this.UpdateDefinitions(inputDeps);
            }
            base.applyMode = ApplyMode.Clear;
            this.m_StartPoint = default(ControlPoint);
            this.m_State = ObjectToolSystem.State.Default;
            this.GetRaycastResult(out this.m_LastRaycastPoint);
            return this.UpdateDefinitions(inputDeps);
        }

        // Token: 0x06000E6A RID: 3690 RVA: 0x0008DCF4 File Offset: 0x0008BEF4
        private void StopRotating()
        {
            if (!this.m_RotationModified)
            {
                PlaceableObjectData placeableObjectData;
                this.m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(this.m_Prefab, out placeableObjectData);
                ObjectToolSystem.Rotation value = this.m_Rotation.value;
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
                {
                    value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateZ(0.7853982f)), quaternion.identity);
                    ObjectToolSystem.SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, true);
                }
                else
                {
                    value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateY(0.7853982f)), quaternion.identity);
                    ObjectToolSystem.SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, false);
                }
                value.m_IsAligned = true;
                this.m_Rotation.value = value;
            }
            this.m_State = ObjectToolSystem.State.Default;
        }

        // Token: 0x17000251 RID: 593
        // (get) Token: 0x06000E6B RID: 3691 RVA: 0x0008DDC9 File Offset: 0x0008BFC9
        private float cameraAngle
        {
            get
            {
                if (!(this.m_CameraController != null))
                {
                    return 0f;
                }
                return this.m_CameraController.angle.x;
            }
        }

        // Token: 0x06000E6C RID: 3692 RVA: 0x0008DDF0 File Offset: 0x0008BFF0
        private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
        {
            if (this.actualMode != ObjectToolSystem.Mode.Brush)
            {
                if (this.GetAllowApply())
                {
                    base.applyMode = ApplyMode.Apply;
                    this.Randomize();
                    if (this.m_Prefab is BuildingPrefab)
                    {
                        Game.Prefabs.ServiceUpgrade serviceUpgrade;
                        if (this.m_Prefab.TryGet<Game.Prefabs.ServiceUpgrade>(out serviceUpgrade))
                        {
                            this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceUpgradeSound, 1f);
                        }
                        else
                        {
                            this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingSound, 1f);
                        }
                    }
                    else if (this.m_Prefab is StaticObjectPrefab || this.m_ToolSystem.actionMode.IsEditor())
                    {
                        this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlacePropSound, 1f);
                    }
                    this.m_ControlPoints.Clear();
                    if (this.m_ToolSystem.actionMode.IsGame() && !this.m_LotQuery.IsEmptyIgnoreFilter)
                    {
                        using (NativeArray<Entity> nativeArray = this.m_LotQuery.ToEntityArray(Allocator.TempJob))
                        {
                            for (int i = 0; i < nativeArray.Length; i++)
                            {
                                Entity entity = nativeArray[i];
                                ref Area componentData = base.EntityManager.GetComponentData<Area>(entity);
                                Temp componentData2 = base.EntityManager.GetComponentData<Temp>(entity);
                                if ((componentData.m_Flags & AreaFlags.Slave) == (AreaFlags)0 && (componentData2.m_Flags & TempFlags.Create) != (TempFlags)0U)
                                {
                                    this.m_AreaToolSystem.recreate = entity;
                                    this.m_AreaToolSystem.prefab = this.m_PrefabSystem.GetPrefab<AreaPrefab>(base.EntityManager.GetComponentData<PrefabRef>(entity));
                                    this.m_AreaToolSystem.mode = AreaToolSystem.Mode.Edit;
                                    this.m_ToolSystem.activeTool = this.m_AreaToolSystem;
                                    return inputDeps;
                                }
                            }
                        }
                    }
                    ControlPoint controlPoint;
                    if (this.GetRaycastResult(out controlPoint))
                    {
                        if (this.m_ToolSystem.actionMode.IsGame())
                        {
                            Telemetry.PlaceBuilding(this.m_UpgradingObject, this.m_Prefab, controlPoint.m_Position);
                        }
                        controlPoint.m_Rotation = this.m_Rotation.value.m_Rotation;
                        this.m_ControlPoints.Add(controlPoint);
                        inputDeps = this.SnapControlPoint(inputDeps);
                        inputDeps = this.UpdateDefinitions(inputDeps);
                    }
                }
                else
                {
                    this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound, 1f);
                    inputDeps = this.Update(inputDeps);
                }
                return inputDeps;
            }
            bool allowApply = this.GetAllowApply();
            if (this.m_State == ObjectToolSystem.State.Default)
            {
                base.applyMode = (allowApply ? ApplyMode.Apply : ApplyMode.Clear);
                this.Randomize();
                if (!singleFrameOnly)
                {
                    this.m_StartPoint = this.m_LastRaycastPoint;
                    this.m_State = ObjectToolSystem.State.Adding;
                }
                this.GetRaycastResult(out this.m_LastRaycastPoint);
                return this.UpdateDefinitions(inputDeps);
            }
            if (this.m_State == ObjectToolSystem.State.Adding && allowApply)
            {
                base.applyMode = ApplyMode.Apply;
                this.Randomize();
                this.m_StartPoint = default(ControlPoint);
                this.m_State = ObjectToolSystem.State.Default;
                this.GetRaycastResult(out this.m_LastRaycastPoint);
                return this.UpdateDefinitions(inputDeps);
            }
            base.applyMode = ApplyMode.Clear;
            this.m_StartPoint = default(ControlPoint);
            this.m_State = ObjectToolSystem.State.Default;
            this.GetRaycastResult(out this.m_LastRaycastPoint);
            return this.UpdateDefinitions(inputDeps);
        }

        // Token: 0x06000E6D RID: 3693 RVA: 0x0008E130 File Offset: 0x0008C330
        private JobHandle Update(JobHandle inputDeps)
        {
            if (this.actualMode != ObjectToolSystem.Mode.Brush)
            {
                ControlPoint controlPoint;
                bool flag;
                if (this.GetRaycastResult(out controlPoint, out flag))
                {
                    controlPoint.m_Rotation = this.m_Rotation.value.m_Rotation;
                    base.applyMode = ApplyMode.None;
                    if (!this.m_LastRaycastPoint.Equals(controlPoint) || flag)
                    {
                        this.m_LastRaycastPoint = controlPoint;
                        ControlPoint controlPoint2 = default(ControlPoint);
                        if (this.m_ControlPoints.Length != 0)
                        {
                            controlPoint2 = this.m_ControlPoints[0];
                            this.m_ControlPoints.Clear();
                        }
                        this.m_ControlPoints.Add(controlPoint);
                        inputDeps = this.SnapControlPoint(inputDeps);
                        JobHandle.ScheduleBatchedJobs();
                        if (!flag)
                        {
                            inputDeps.Complete();
                            ControlPoint other = this.m_ControlPoints[0];
                            flag = !controlPoint2.EqualsIgnoreHit(other);
                        }
                        if (flag)
                        {
                            base.applyMode = ApplyMode.Clear;
                            inputDeps = this.UpdateDefinitions(inputDeps);
                        }
                    }
                }
                else
                {
                    base.applyMode = ApplyMode.Clear;
                    this.m_LastRaycastPoint = default(ControlPoint);
                }
                return inputDeps;
            }
            ControlPoint controlPoint3;
            bool flag2;
            if (this.GetRaycastResult(out controlPoint3, out flag2))
            {
                if (this.m_State != ObjectToolSystem.State.Default)
                {
                    base.applyMode = (this.GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
                    this.Randomize();
                    this.m_StartPoint = this.m_LastRaycastPoint;
                    this.m_LastRaycastPoint = controlPoint3;
                    return this.UpdateDefinitions(inputDeps);
                }
                if (this.m_LastRaycastPoint.Equals(controlPoint3) && !flag2)
                {
                    base.applyMode = ApplyMode.None;
                    return inputDeps;
                }
                base.applyMode = ApplyMode.Clear;
                this.m_StartPoint = controlPoint3;
                this.m_LastRaycastPoint = controlPoint3;
                return this.UpdateDefinitions(inputDeps);
            }
            else
            {
                if (this.m_LastRaycastPoint.Equals(default(ControlPoint)) && !flag2)
                {
                    base.applyMode = ApplyMode.None;
                    return inputDeps;
                }
                if (this.m_State != ObjectToolSystem.State.Default)
                {
                    base.applyMode = (this.GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
                    this.Randomize();
                    this.m_StartPoint = this.m_LastRaycastPoint;
                    this.m_LastRaycastPoint = default(ControlPoint);
                }
                else
                {
                    base.applyMode = ApplyMode.Clear;
                    this.m_StartPoint = default(ControlPoint);
                    this.m_LastRaycastPoint = default(ControlPoint);
                }
                return this.UpdateDefinitions(inputDeps);
            }
        }

        // Token: 0x06000E6E RID: 3694 RVA: 0x0008E330 File Offset: 0x0008C530
        private JobHandle SnapControlPoint(JobHandle inputDeps)
        {
            Entity selected = (this.actualMode == ObjectToolSystem.Mode.Move) ? this.m_MovingObject : this.m_ToolSystem.selected;
            this.__TypeHandle.__Game_Prefabs_NetCompositionArea_RO_BufferLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Terrain_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(base.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(base.CheckedStateRef);
            ObjectToolSystem.SnapJob jobData = default(ObjectToolSystem.SnapJob);
            jobData.m_EditorMode = this.m_ToolSystem.actionMode.IsEditor();
            jobData.m_Snap = base.GetActualSnap();
            jobData.m_Mode = this.actualMode;
            jobData.m_Prefab = this.m_PrefabSystem.GetEntity(this.m_Prefab);
            jobData.m_Selected = selected;
            jobData.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
            jobData.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            jobData.m_AttachedData = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
            jobData.m_TerrainData = this.__TypeHandle.__Game_Common_Terrain_RO_ComponentLookup;
            jobData.m_LocalTransformCacheData = this.__TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup;
            jobData.m_EdgeData = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
            jobData.m_NodeData = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
            jobData.m_OrphanData = this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup;
            jobData.m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
            jobData.m_CompositionData = this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup;
            jobData.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
            jobData.m_StartNodeGeometryData = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup;
            jobData.m_EndNodeGeometryData = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup;
            jobData.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            jobData.m_ObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
            jobData.m_BuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            jobData.m_BuildingExtensionData = this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;
            jobData.m_PrefabCompositionData = this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup;
            jobData.m_PlaceableObjectData = this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;
            jobData.m_AssetStampData = this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup;
            jobData.m_OutsideConnectionData = this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
            jobData.m_NetObjectData = this.__TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentLookup;
            jobData.m_TransportStopData = this.__TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup;
            jobData.m_StackData = this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup;
            jobData.m_ServiceUpgradeData = this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;
            jobData.m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup;
            jobData.m_SubObjects = this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup;
            jobData.m_ConnectedEdges = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup;
            jobData.m_PrefabCompositionAreas = this.__TypeHandle.__Game_Prefabs_NetCompositionArea_RO_BufferLookup;
            JobHandle job;
            jobData.m_ObjectSearchTree = this.m_ObjectSearchSystem.GetStaticSearchTree(true, out job);
            JobHandle job2;
            jobData.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(true, out job2);
            JobHandle job3;
            jobData.m_ZoneSearchTree = this.m_ZoneSearchSystem.GetSearchTree(true, out job3);
            JobHandle job4;
            jobData.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out job4);
            jobData.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData(false);
            jobData.m_ControlPoints = this.m_ControlPoints;
            jobData.m_Rotation = this.m_Rotation;
            JobHandle jobHandle = jobData.Schedule(JobUtils.CombineDependencies(inputDeps, job, job2, job3, job4));
            this.m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
            this.m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
            this.m_ZoneSearchSystem.AddSearchTreeReader(jobHandle);
            this.m_WaterSystem.AddSurfaceReader(jobHandle);
            return jobHandle;
        }

        // Token: 0x06000E6F RID: 3695 RVA: 0x0008E908 File Offset: 0x0008CB08
        private JobHandle UpdateDefinitions(JobHandle inputDeps)
        {
            JobHandle jobHandle = base.DestroyDefinitions(this.m_DefinitionQuery, this.m_ToolOutputBarrier, inputDeps);
            if (this.m_Prefab != null)
            {
                Entity entity = this.m_PrefabSystem.GetEntity(this.m_Prefab);
                Entity @null = Entity.Null;
                Entity transformPrefab = Entity.Null;
                Entity brushPrefab = Entity.Null;
                float deltaTime = UnityEngine.Time.deltaTime;
                if (this.m_ToolSystem.actionMode.IsEditor())
                {
                    Entity entity2;
                    base.GetContainers(this.m_ContainerQuery, out @null, out entity2);
                }
                if (this.m_TransformPrefab != null)
                {
                    transformPrefab = this.m_PrefabSystem.GetEntity(this.m_TransformPrefab);
                }
                if (this.actualMode == ObjectToolSystem.Mode.Brush && base.brushType != null)
                {
                    brushPrefab = this.m_PrefabSystem.GetEntity(base.brushType);
                    base.EnsureCachedBrushData();
                    ControlPoint startPoint = this.m_StartPoint;
                    ControlPoint lastRaycastPoint = this.m_LastRaycastPoint;
                    startPoint.m_OriginalEntity = Entity.Null;
                    lastRaycastPoint.m_OriginalEntity = Entity.Null;
                    this.m_ControlPoints.Clear();
                    this.m_ControlPoints.Add(startPoint);
                    this.m_ControlPoints.Add(lastRaycastPoint);
                    if (this.m_State == ObjectToolSystem.State.Default)
                    {
                        deltaTime = 0.1f;
                    }
                }
                NativeReference<ObjectToolBaseSystem.AttachmentData> attachmentPrefab = default(NativeReference<ObjectToolBaseSystem.AttachmentData>);
                PlaceholderBuildingData placeholderBuildingData;
                if (!this.m_ToolSystem.actionMode.IsEditor() && base.EntityManager.TryGetComponent(entity, out placeholderBuildingData))
                {
                    ZoneData componentData = base.EntityManager.GetComponentData<ZoneData>(placeholderBuildingData.m_ZonePrefab);
                    BuildingData componentData2 = base.EntityManager.GetComponentData<BuildingData>(entity);
                    this.m_BuildingQuery.ResetFilter();
                    this.m_BuildingQuery.SetSharedComponentFilter<BuildingSpawnGroupData>(new BuildingSpawnGroupData(componentData.m_ZoneType));
                    attachmentPrefab = new NativeReference<ObjectToolBaseSystem.AttachmentData>(Allocator.TempJob, NativeArrayOptions.ClearMemory);
                    JobHandle job;
                    NativeList<ArchetypeChunk> chunks = this.m_BuildingQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out job);
                    this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(base.CheckedStateRef);
                    this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle.Update(base.CheckedStateRef);
                    this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(base.CheckedStateRef);
                    ObjectToolSystem.FindAttachmentBuildingJob jobData = default(ObjectToolSystem.FindAttachmentBuildingJob);
                    jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                    jobData.m_BuildingDataType = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
                    jobData.m_SpawnableBuildingType = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
                    jobData.m_BuildingData = componentData2;
                    jobData.m_RandomSeed = this.m_RandomSeed;
                    jobData.m_Chunks = chunks;
                    jobData.m_AttachmentPrefab = attachmentPrefab;
                    inputDeps = jobData.Schedule(JobHandle.CombineDependencies(inputDeps, job));
                    chunks.Dispose(inputDeps);
                }
                jobHandle = JobHandle.CombineDependencies(jobHandle, base.CreateDefinitions(entity, transformPrefab, brushPrefab, this.m_UpgradingObject, this.m_MovingObject, @null, this.m_CityConfigurationSystem.defaultTheme, this.m_ControlPoints, attachmentPrefab, this.m_ToolSystem.actionMode.IsEditor(), this.m_CityConfigurationSystem.leftHandTraffic, this.m_State == ObjectToolSystem.State.Removing, this.actualMode == ObjectToolSystem.Mode.Stamp, base.brushSize, math.radians(base.brushAngle), base.brushStrength, deltaTime, this.m_RandomSeed, base.GetActualSnap(), this.actualAgeMask, inputDeps));
                if (attachmentPrefab.IsCreated)
                {
                    attachmentPrefab.Dispose(jobHandle);
                }
            }
            return jobHandle;
        }

        // Token: 0x06000E70 RID: 3696 RVA: 0x000030F5 File Offset: 0x000012F5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06000E71 RID: 3697 RVA: 0x0008EC36 File Offset: 0x0008CE36
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(base.CheckedStateRef);
        }

        // Token: 0x06000E72 RID: 3698 RVA: 0x0008EC5B File Offset: 0x0008CE5B
        [Preserve]
        public ObjectToolSystem()
        {
        }

        // Token: 0x0400181F RID: 6175
        public const string kToolID = "Object Tool";

        // Token: 0x04001827 RID: 6183
        private AreaToolSystem m_AreaToolSystem;

        // Token: 0x04001828 RID: 6184
        private Game.Net.SearchSystem m_NetSearchSystem;

        // Token: 0x04001829 RID: 6185
        private Game.Zones.SearchSystem m_ZoneSearchSystem;

        // Token: 0x0400182A RID: 6186
        private CityConfigurationSystem m_CityConfigurationSystem;

        // Token: 0x0400182B RID: 6187
        private AudioManager m_AudioManager;

        // Token: 0x0400182C RID: 6188
        private EntityQuery m_DefinitionQuery;

        // Token: 0x0400182D RID: 6189
        private EntityQuery m_TempQuery;

        // Token: 0x0400182E RID: 6190
        private EntityQuery m_ContainerQuery;

        // Token: 0x0400182F RID: 6191
        private EntityQuery m_BrushQuery;

        // Token: 0x04001830 RID: 6192
        private EntityQuery m_LotQuery;

        // Token: 0x04001831 RID: 6193
        private EntityQuery m_BuildingQuery;

        // Token: 0x04001832 RID: 6194
        private ProxyAction m_ApplyAction;

        // Token: 0x04001833 RID: 6195
        private ProxyAction m_SecondaryApplyAction;

        // Token: 0x04001834 RID: 6196
        private ProxyAction m_CancelAction;

        // Token: 0x04001835 RID: 6197
        private DisplayNameOverride m_ApplyDisplayOverride;

        // Token: 0x04001836 RID: 6198
        private DisplayNameOverride m_SecondaryApplyDisplayOverride;

        // Token: 0x04001837 RID: 6199
        private NativeList<ControlPoint> m_ControlPoints;

        // Token: 0x04001838 RID: 6200
        private NativeValue<ObjectToolSystem.Rotation> m_Rotation;

        // Token: 0x04001839 RID: 6201
        private ControlPoint m_LastRaycastPoint;

        // Token: 0x0400183A RID: 6202
        private ControlPoint m_StartPoint;

        // Token: 0x0400183B RID: 6203
        private Entity m_UpgradingObject;

        // Token: 0x0400183C RID: 6204
        private Entity m_MovingObject;

        // Token: 0x0400183D RID: 6205
        private Entity m_MovingInitialized;

        // Token: 0x0400183E RID: 6206
        private ObjectToolSystem.State m_State;

        // Token: 0x0400183F RID: 6207
        private bool m_RotationModified;

        // Token: 0x04001840 RID: 6208
        private bool m_ForceCancel;

        // Token: 0x04001841 RID: 6209
        private float3 m_RotationStartPosition;

        // Token: 0x04001842 RID: 6210
        private quaternion m_StartRotation;

        // Token: 0x04001843 RID: 6211
        private float m_StartCameraAngle;

        // Token: 0x04001844 RID: 6212
        private EntityQuery m_SoundQuery;

        // Token: 0x04001845 RID: 6213
        private RandomSeed m_RandomSeed;

        // Token: 0x04001846 RID: 6214
        private ObjectPrefab m_Prefab;

        // Token: 0x04001847 RID: 6215
        private ObjectPrefab m_SelectedPrefab;

        // Token: 0x04001848 RID: 6216
        private TransformPrefab m_TransformPrefab;

        // Token: 0x04001849 RID: 6217
        private CameraController m_CameraController;

        // Token: 0x0400184A RID: 6218
        private ObjectToolSystem.TypeHandle __TypeHandle;

        // Token: 0x02000312 RID: 786
        public enum Mode
        {
            // Token: 0x0400184C RID: 6220
            Create,
            // Token: 0x0400184D RID: 6221
            Upgrade,
            // Token: 0x0400184E RID: 6222
            Move,
            // Token: 0x0400184F RID: 6223
            Brush,
            // Token: 0x04001850 RID: 6224
            Stamp
        }

        // Token: 0x02000313 RID: 787
        private enum State
        {
            // Token: 0x04001852 RID: 6226
            Default,
            // Token: 0x04001853 RID: 6227
            Rotating,
            // Token: 0x04001854 RID: 6228
            Adding,
            // Token: 0x04001855 RID: 6229
            Removing
        }

        // Token: 0x02000314 RID: 788
        private struct Rotation
        {
            // Token: 0x04001856 RID: 6230
            public quaternion m_Rotation;

            // Token: 0x04001857 RID: 6231
            public quaternion m_ParentRotation;

            // Token: 0x04001858 RID: 6232
            public bool m_IsAligned;
        }

        // Token: 0x02000315 RID: 789
        [BurstCompile]
        private struct SnapJob : IJob
        {
            // Token: 0x06000E73 RID: 3699 RVA: 0x0008EC64 File Offset: 0x0008CE64
            public void Execute()
            {
                ControlPoint controlPoint = this.m_ControlPoints[0];
                if ((this.m_Snap & (Snap.NetArea | Snap.NetNode)) != Snap.None && this.m_TerrainData.HasComponent(controlPoint.m_OriginalEntity) && !this.m_BuildingData.HasComponent(this.m_Prefab))
                {
                    this.FindLoweredParent(ref controlPoint);
                }
                ControlPoint controlPoint2 = controlPoint;
                controlPoint2.m_OriginalEntity = Entity.Null;
                if (this.m_OutsideConnectionData.HasComponent(this.m_Prefab))
                {
                    this.HandleWorldSize(ref controlPoint2, controlPoint);
                }
                float minValue = float.MinValue;
                if ((this.m_Snap & Snap.Shoreline) != Snap.None)
                {
                    float radius = 1f;
                    float3 offset = 0f;
                    BuildingData buildingData;
                    BuildingExtensionData buildingExtensionData;
                    if (this.m_BuildingData.TryGetComponent(this.m_Prefab, out buildingData))
                    {
                        radius = math.length(buildingData.m_LotSize) * 4f;
                    }
                    else if (this.m_BuildingExtensionData.TryGetComponent(this.m_Prefab, out buildingExtensionData))
                    {
                        radius = math.length(buildingExtensionData.m_LotSize) * 4f;
                    }
                    PlaceableObjectData placeableObjectData;
                    if (this.m_PlaceableObjectData.TryGetComponent(this.m_Prefab, out placeableObjectData))
                    {
                        offset = placeableObjectData.m_PlacementOffset;
                    }
                    this.SnapShoreline(controlPoint, ref controlPoint2, ref minValue, radius, offset);
                }
                if ((this.m_Snap & Snap.NetSide) != Snap.None)
                {
                    BuildingData buildingData2 = this.m_BuildingData[this.m_Prefab];
                    float rhs = (float)buildingData2.m_LotSize.y * 4f + 16f;
                    float bestDistance = (float)math.cmin(buildingData2.m_LotSize) * 4f + 16f;
                    ObjectToolSystem.SnapJob.ZoneBlockIterator zoneBlockIterator = default(ObjectToolSystem.SnapJob.ZoneBlockIterator);
                    zoneBlockIterator.m_ControlPoint = controlPoint;
                    zoneBlockIterator.m_BestSnapPosition = controlPoint2;
                    zoneBlockIterator.m_BestDistance = bestDistance;
                    zoneBlockIterator.m_LotSize = buildingData2.m_LotSize;
                    zoneBlockIterator.m_Bounds = new Bounds2(controlPoint.m_Position.xz - rhs, controlPoint.m_Position.xz + rhs);
                    zoneBlockIterator.m_Direction = math.forward(this.m_Rotation.value.m_Rotation).xz;
                    zoneBlockIterator.m_IgnoreOwner = ((this.m_Mode == ObjectToolSystem.Mode.Move) ? this.m_Selected : Entity.Null);
                    zoneBlockIterator.m_OwnerData = this.m_OwnerData;
                    zoneBlockIterator.m_BlockData = this.m_BlockData;
                    ObjectToolSystem.SnapJob.ZoneBlockIterator zoneBlockIterator2 = zoneBlockIterator;
                    this.m_ZoneSearchTree.Iterate<ObjectToolSystem.SnapJob.ZoneBlockIterator>(ref zoneBlockIterator2, 0);
                    controlPoint2 = zoneBlockIterator2.m_BestSnapPosition;
                }
                if ((this.m_Snap & Snap.OwnerSide) != Snap.None)
                {
                    Entity entity = Entity.Null;
                    Owner owner;
                    if (this.m_Mode == ObjectToolSystem.Mode.Upgrade)
                    {
                        entity = this.m_Selected;
                    }
                    else if (this.m_Mode == ObjectToolSystem.Mode.Move && this.m_OwnerData.TryGetComponent(this.m_Selected, out owner))
                    {
                        entity = owner.m_Owner;
                    }
                    if (entity != Entity.Null)
                    {
                        BuildingData buildingData3 = this.m_BuildingData[this.m_Prefab];
                        PrefabRef prefabRef = this.m_PrefabRefData[entity];
                        Game.Objects.Transform transform = this.m_TransformData[entity];
                        BuildingData buildingData4 = this.m_BuildingData[prefabRef.m_Prefab];
                        int2 lotSize = buildingData4.m_LotSize + buildingData3.m_LotSize.y;
                        Quad2 xz = BuildingUtils.CalculateCorners(transform, lotSize).xz;
                        int num = buildingData3.m_LotSize.x - 1;
                        bool flag = false;
                        ServiceUpgradeData serviceUpgradeData;
                        if (this.m_ServiceUpgradeData.TryGetComponent(this.m_Prefab, out serviceUpgradeData))
                        {
                            num = math.select(num, serviceUpgradeData.m_MaxPlacementOffset, serviceUpgradeData.m_MaxPlacementOffset >= 0);
                            flag |= (serviceUpgradeData.m_MaxPlacementDistance == 0f);
                        }
                        if (!flag)
                        {
                            float2 halfLotSize = buildingData3.m_LotSize * 4f - 0.4f;
                            Quad2 xz2 = BuildingUtils.CalculateCorners(transform, buildingData4.m_LotSize).xz;
                            Quad2 xz3 = BuildingUtils.CalculateCorners(controlPoint.m_HitPosition, this.m_Rotation.value.m_Rotation, halfLotSize).xz;
                            flag = (MathUtils.Intersect(xz2, xz3) && MathUtils.Intersect(xz, controlPoint.m_HitPosition.xz));
                        }
                        ObjectToolSystem.SnapJob.CheckSnapLine(buildingData3, transform, controlPoint, ref controlPoint2, new Line2(xz.a, xz.b), num, 0f, flag);
                        ObjectToolSystem.SnapJob.CheckSnapLine(buildingData3, transform, controlPoint, ref controlPoint2, new Line2(xz.b, xz.c), num, 1.5707964f, flag);
                        ObjectToolSystem.SnapJob.CheckSnapLine(buildingData3, transform, controlPoint, ref controlPoint2, new Line2(xz.c, xz.d), num, 3.1415927f, flag);
                        ObjectToolSystem.SnapJob.CheckSnapLine(buildingData3, transform, controlPoint, ref controlPoint2, new Line2(xz.d, xz.a), num, 4.712389f, flag);
                    }
                }
                if ((this.m_Snap & Snap.NetArea) != Snap.None)
                {
                    if (this.m_BuildingData.HasComponent(this.m_Prefab))
                    {
                        Curve curve;
                        if (this.m_CurveData.TryGetComponent(controlPoint.m_OriginalEntity, out curve))
                        {
                            ControlPoint controlPoint3 = controlPoint;
                            controlPoint3.m_OriginalEntity = Entity.Null;
                            controlPoint3.m_Position = MathUtils.Position(curve.m_Bezier, controlPoint.m_CurvePosition);
                            controlPoint3.m_Direction = math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, controlPoint.m_CurvePosition).xz, default(float2));
                            controlPoint3.m_Direction = MathUtils.Left(controlPoint3.m_Direction);
                            if (math.dot(math.forward(this.m_Rotation.value.m_Rotation).xz, controlPoint3.m_Direction) < 0f)
                            {
                                controlPoint3.m_Direction = -controlPoint3.m_Direction;
                            }
                            controlPoint3.m_Rotation = ToolUtils.CalculateRotation(controlPoint3.m_Direction);
                            controlPoint3.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, controlPoint.m_HitPosition.xz, controlPoint3.m_Position.xz, controlPoint3.m_Direction);
                            ObjectToolSystem.SnapJob.AddSnapPosition(ref controlPoint2, controlPoint3);
                        }
                    }
                    else if (this.m_EdgeGeometryData.HasComponent(controlPoint.m_OriginalEntity))
                    {
                        EdgeGeometry edgeGeometry = this.m_EdgeGeometryData[controlPoint.m_OriginalEntity];
                        Composition composition = this.m_CompositionData[controlPoint.m_OriginalEntity];
                        NetCompositionData netCompositionData = this.m_PrefabCompositionData[composition.m_Edge];
                        DynamicBuffer<NetCompositionArea> areas = this.m_PrefabCompositionAreas[composition.m_Edge];
                        float num2 = 0f;
                        if (this.m_ObjectGeometryData.HasComponent(this.m_Prefab))
                        {
                            ObjectGeometryData objectGeometryData = this.m_ObjectGeometryData[this.m_Prefab];
                            if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
                            {
                                num2 = objectGeometryData.m_LegSize.z * 0.5f;
                                if (objectGeometryData.m_LegSize.y <= netCompositionData.m_HeightRange.max)
                                {
                                    num2 = math.max(num2, objectGeometryData.m_Size.z * 0.5f);
                                }
                            }
                            else
                            {
                                num2 = objectGeometryData.m_Size.z * 0.5f;
                            }
                        }
                        this.SnapSegmentAreas(controlPoint, ref controlPoint2, num2, controlPoint.m_OriginalEntity, edgeGeometry.m_Start, netCompositionData, areas);
                        this.SnapSegmentAreas(controlPoint, ref controlPoint2, num2, controlPoint.m_OriginalEntity, edgeGeometry.m_End, netCompositionData, areas);
                    }
                    else if (this.m_ConnectedEdges.HasBuffer(controlPoint.m_OriginalEntity))
                    {
                        DynamicBuffer<ConnectedEdge> dynamicBuffer = this.m_ConnectedEdges[controlPoint.m_OriginalEntity];
                        for (int i = 0; i < dynamicBuffer.Length; i++)
                        {
                            Entity edge = dynamicBuffer[i].m_Edge;
                            Edge edge2 = this.m_EdgeData[edge];
                            if ((!(edge2.m_Start != controlPoint.m_OriginalEntity) || !(edge2.m_End != controlPoint.m_OriginalEntity)) && this.m_EdgeGeometryData.HasComponent(edge))
                            {
                                EdgeGeometry edgeGeometry2 = this.m_EdgeGeometryData[edge];
                                Composition composition2 = this.m_CompositionData[edge];
                                NetCompositionData netCompositionData2 = this.m_PrefabCompositionData[composition2.m_Edge];
                                DynamicBuffer<NetCompositionArea> areas2 = this.m_PrefabCompositionAreas[composition2.m_Edge];
                                float num3 = 0f;
                                if (this.m_ObjectGeometryData.HasComponent(this.m_Prefab))
                                {
                                    ObjectGeometryData objectGeometryData2 = this.m_ObjectGeometryData[this.m_Prefab];
                                    if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
                                    {
                                        num3 = objectGeometryData2.m_LegSize.z * 0.5f;
                                        if (objectGeometryData2.m_LegSize.y <= netCompositionData2.m_HeightRange.max)
                                        {
                                            num3 = math.max(num3, objectGeometryData2.m_Size.z * 0.5f);
                                        }
                                    }
                                    else
                                    {
                                        num3 = objectGeometryData2.m_Size.z * 0.5f;
                                    }
                                }
                                this.SnapSegmentAreas(controlPoint, ref controlPoint2, num3, edge, edgeGeometry2.m_Start, netCompositionData2, areas2);
                                this.SnapSegmentAreas(controlPoint, ref controlPoint2, num3, edge, edgeGeometry2.m_End, netCompositionData2, areas2);
                            }
                        }
                    }
                }
                if ((this.m_Snap & Snap.NetNode) != Snap.None)
                {
                    if (this.m_NodeData.HasComponent(controlPoint.m_OriginalEntity))
                    {
                        Game.Net.Node node = this.m_NodeData[controlPoint.m_OriginalEntity];
                        this.SnapNode(controlPoint, ref controlPoint2, controlPoint.m_OriginalEntity, node);
                    }
                    else if (this.m_EdgeData.HasComponent(controlPoint.m_OriginalEntity))
                    {
                        Edge edge3 = this.m_EdgeData[controlPoint.m_OriginalEntity];
                        this.SnapNode(controlPoint, ref controlPoint2, edge3.m_Start, this.m_NodeData[edge3.m_Start]);
                        this.SnapNode(controlPoint, ref controlPoint2, edge3.m_End, this.m_NodeData[edge3.m_End]);
                    }
                }
                if ((this.m_Snap & Snap.ObjectSurface) != Snap.None && this.m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    int num4 = controlPoint.m_ElementIndex.x;
                    Entity entity2 = controlPoint.m_OriginalEntity;
                    while (this.m_OwnerData.HasComponent(entity2))
                    {
                        if (this.m_LocalTransformCacheData.HasComponent(entity2))
                        {
                            num4 = this.m_LocalTransformCacheData[entity2].m_ParentMesh;
                            num4 += math.select(1000, -1000, num4 < 0);
                        }
                        entity2 = this.m_OwnerData[entity2].m_Owner;
                    }
                    if (this.m_TransformData.HasComponent(entity2) && this.m_SubObjects.HasBuffer(entity2))
                    {
                        this.SnapSurface(controlPoint, ref controlPoint2, entity2, num4);
                    }
                }
                this.CalculateHeight(ref controlPoint2, minValue);
                if (this.m_EditorMode)
                {
                    if ((this.m_Snap & Snap.AutoParent) == Snap.None)
                    {
                        if ((this.m_Snap & (Snap.NetArea | Snap.NetNode)) == Snap.None || this.m_TransformData.HasComponent(controlPoint2.m_OriginalEntity) || this.m_BuildingData.HasComponent(this.m_Prefab))
                        {
                            controlPoint2.m_OriginalEntity = Entity.Null;
                        }
                    }
                    else if (controlPoint2.m_OriginalEntity == Entity.Null)
                    {
                        ObjectGeometryData objectGeometryData3 = default(ObjectGeometryData);
                        if (this.m_ObjectGeometryData.HasComponent(this.m_Prefab))
                        {
                            objectGeometryData3 = this.m_ObjectGeometryData[this.m_Prefab];
                        }
                        ObjectToolSystem.SnapJob.ParentObjectIterator parentObjectIterator = default(ObjectToolSystem.SnapJob.ParentObjectIterator);
                        parentObjectIterator.m_ControlPoint = controlPoint2;
                        parentObjectIterator.m_BestSnapPosition = controlPoint2;
                        parentObjectIterator.m_Bounds = ObjectUtils.CalculateBounds(controlPoint2.m_Position, controlPoint2.m_Rotation, objectGeometryData3);
                        parentObjectIterator.m_BestOverlap = float.MaxValue;
                        parentObjectIterator.m_IsBuilding = this.m_BuildingData.HasComponent(this.m_Prefab);
                        parentObjectIterator.m_PrefabObjectGeometryData1 = objectGeometryData3;
                        parentObjectIterator.m_TransformData = this.m_TransformData;
                        parentObjectIterator.m_BuildingData = this.m_BuildingData;
                        parentObjectIterator.m_AssetStampData = this.m_AssetStampData;
                        parentObjectIterator.m_PrefabRefData = this.m_PrefabRefData;
                        parentObjectIterator.m_PrefabObjectGeometryData = this.m_ObjectGeometryData;
                        ObjectToolSystem.SnapJob.ParentObjectIterator parentObjectIterator2 = parentObjectIterator;
                        this.m_ObjectSearchTree.Iterate<ObjectToolSystem.SnapJob.ParentObjectIterator>(ref parentObjectIterator2, 0);
                        controlPoint2 = parentObjectIterator2.m_BestSnapPosition;
                    }
                }
                if (this.m_Mode == ObjectToolSystem.Mode.Create && this.m_NetObjectData.HasComponent(this.m_Prefab) && (this.m_NodeData.HasComponent(controlPoint2.m_OriginalEntity) || this.m_EdgeData.HasComponent(controlPoint2.m_OriginalEntity)))
                {
                    this.FindOriginalObject(ref controlPoint2, controlPoint);
                }
                ObjectToolSystem.Rotation value = this.m_Rotation.value;
                value.m_IsAligned &= value.m_Rotation.Equals(controlPoint2.m_Rotation);
                this.AlignObject(ref controlPoint2, ref value.m_ParentRotation, value.m_IsAligned);
                value.m_Rotation = controlPoint2.m_Rotation;
                this.m_Rotation.value = value;
                StackData stackData;
                if (this.m_StackData.TryGetComponent(this.m_Prefab, out stackData) && stackData.m_Direction == StackDirection.Up)
                {
                    float num5 = stackData.m_FirstBounds.max + MathUtils.Size(stackData.m_MiddleBounds) * 2f - stackData.m_LastBounds.min;
                    controlPoint2.m_Elevation += num5;
                    controlPoint2.m_Position.y = controlPoint2.m_Position.y + num5;
                }
                this.m_ControlPoints[0] = controlPoint2;
            }

            // Token: 0x06000E74 RID: 3700 RVA: 0x0008F950 File Offset: 0x0008DB50
            private void FindLoweredParent(ref ControlPoint controlPoint)
            {
                ObjectToolSystem.SnapJob.LoweredParentIterator loweredParentIterator = default(ObjectToolSystem.SnapJob.LoweredParentIterator);
                loweredParentIterator.m_Result = controlPoint;
                loweredParentIterator.m_Position = controlPoint.m_HitPosition;
                loweredParentIterator.m_EdgeData = this.m_EdgeData;
                loweredParentIterator.m_NodeData = this.m_NodeData;
                loweredParentIterator.m_OrphanData = this.m_OrphanData;
                loweredParentIterator.m_CurveData = this.m_CurveData;
                loweredParentIterator.m_CompositionData = this.m_CompositionData;
                loweredParentIterator.m_EdgeGeometryData = this.m_EdgeGeometryData;
                loweredParentIterator.m_StartNodeGeometryData = this.m_StartNodeGeometryData;
                loweredParentIterator.m_EndNodeGeometryData = this.m_EndNodeGeometryData;
                loweredParentIterator.m_PrefabCompositionData = this.m_PrefabCompositionData;
                ObjectToolSystem.SnapJob.LoweredParentIterator loweredParentIterator2 = loweredParentIterator;
                this.m_NetSearchTree.Iterate<ObjectToolSystem.SnapJob.LoweredParentIterator>(ref loweredParentIterator2, 0);
                controlPoint = loweredParentIterator2.m_Result;
            }

            // Token: 0x06000E75 RID: 3701 RVA: 0x0008FA10 File Offset: 0x0008DC10
            private void FindOriginalObject(ref ControlPoint bestSnapPosition, ControlPoint controlPoint)
            {
                ObjectToolSystem.SnapJob.OriginalObjectIterator originalObjectIterator = default(ObjectToolSystem.SnapJob.OriginalObjectIterator);
                originalObjectIterator.m_Parent = bestSnapPosition.m_OriginalEntity;
                originalObjectIterator.m_BestDistance = float.MaxValue;
                originalObjectIterator.m_EditorMode = this.m_EditorMode;
                originalObjectIterator.m_OwnerData = this.m_OwnerData;
                originalObjectIterator.m_AttachedData = this.m_AttachedData;
                originalObjectIterator.m_PrefabRefData = this.m_PrefabRefData;
                originalObjectIterator.m_NetObjectData = this.m_NetObjectData;
                originalObjectIterator.m_TransportStopData = this.m_TransportStopData;
                ObjectToolSystem.SnapJob.OriginalObjectIterator originalObjectIterator2 = originalObjectIterator;
                ObjectGeometryData geometryData;
                if (this.m_ObjectGeometryData.TryGetComponent(this.m_Prefab, out geometryData))
                {
                    originalObjectIterator2.m_Bounds = ObjectUtils.CalculateBounds(bestSnapPosition.m_Position, bestSnapPosition.m_Rotation, geometryData);
                }
                else
                {
                    originalObjectIterator2.m_Bounds = new Bounds3(bestSnapPosition.m_Position - 1f, bestSnapPosition.m_Position + 1f);
                }
                TransportStopData transportStopData;
                if (this.m_TransportStopData.TryGetComponent(this.m_Prefab, out transportStopData))
                {
                    originalObjectIterator2.m_TransportStopData1 = transportStopData;
                }
                this.m_ObjectSearchTree.Iterate<ObjectToolSystem.SnapJob.OriginalObjectIterator>(ref originalObjectIterator2, 0);
                if (originalObjectIterator2.m_Result != Entity.Null)
                {
                    bestSnapPosition.m_OriginalEntity = originalObjectIterator2.m_Result;
                }
            }

            // Token: 0x06000E76 RID: 3702 RVA: 0x0008FB34 File Offset: 0x0008DD34
            private void HandleWorldSize(ref ControlPoint bestSnapPosition, ControlPoint controlPoint)
            {
                Bounds3 bounds = TerrainUtils.GetBounds(ref this.m_TerrainHeightData);
                bool2 @bool = false;
                float2 @float = 0f;
                Bounds3 bounds2 = new Bounds3(controlPoint.m_HitPosition, controlPoint.m_HitPosition);
                ObjectGeometryData geometryData;
                if (this.m_ObjectGeometryData.TryGetComponent(this.m_Prefab, out geometryData))
                {
                    bounds2 = ObjectUtils.CalculateBounds(controlPoint.m_HitPosition, controlPoint.m_Rotation, geometryData);
                }
                if (bounds2.min.x < bounds.min.x)
                {
                    @bool.x = true;
                    @float.x = bounds.min.x;
                }
                else if (bounds2.max.x > bounds.max.x)
                {
                    @bool.x = true;
                    @float.x = bounds.max.x;
                }
                if (bounds2.min.z < bounds.min.z)
                {
                    @bool.y = true;
                    @float.y = bounds.min.z;
                }
                else if (bounds2.max.z > bounds.max.z)
                {
                    @bool.y = true;
                    @float.y = bounds.max.z;
                }
                if (!math.any(@bool))
                {
                    return;
                }
                ControlPoint controlPoint2 = controlPoint;
                controlPoint2.m_OriginalEntity = Entity.Null;
                controlPoint2.m_Direction = new float2(0f, 1f);
                controlPoint2.m_Position.xz = math.select(controlPoint.m_HitPosition.xz, @float, @bool);
                controlPoint2.m_Position.y = controlPoint.m_HitPosition.y;
                controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(2f, 1f, controlPoint.m_HitPosition.xz, controlPoint2.m_Position.xz, controlPoint2.m_Direction);
                controlPoint2.m_Rotation = quaternion.LookRotationSafe(new float3
                {
                    xz = math.sign(@float)
                }, math.up());
                ObjectToolSystem.SnapJob.AddSnapPosition(ref bestSnapPosition, controlPoint2);
            }

            // Token: 0x06000E77 RID: 3703 RVA: 0x0008FD30 File Offset: 0x0008DF30
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
                        quaternion quaternion = math.mul(a, quaternion.RotateZ((float)i * 0.7853982f));
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
                quaternion a2 = math.mul(quaternion.LookRotationSafe(forward2, up2), quaternion.RotateX(1.5707964f));
                quaternion q2 = rotation;
                float num3 = float.MaxValue;
                for (int j = 0; j < 8; j++)
                {
                    quaternion quaternion2 = math.mul(a2, quaternion.RotateY((float)j * 0.7853982f));
                    float num4 = MathUtils.RotationAngle(rotation, quaternion2);
                    if (num4 < num3)
                    {
                        q2 = quaternion2;
                        num3 = num4;
                    }
                }
                rotation = math.normalizesafe(q2, quaternion.identity);
            }

            // Token: 0x06000E78 RID: 3704 RVA: 0x0008FE9C File Offset: 0x0008E09C
            private void AlignObject(ref ControlPoint controlPoint, ref quaternion parentRotation, bool alignRotation)
            {
                PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
                if (this.m_PlaceableObjectData.HasComponent(this.m_Prefab))
                {
                    placeableObjectData = this.m_PlaceableObjectData[this.m_Prefab];
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
                {
                    ObjectGeometryData objectGeometryData = this.m_ObjectGeometryData[this.m_Prefab];
                    controlPoint.m_Position.y = controlPoint.m_Position.y - objectGeometryData.m_Bounds.max.y;
                }
                parentRotation = quaternion.identity;
                if (this.m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    Entity entity = controlPoint.m_OriginalEntity;
                    PrefabRef prefabRef = this.m_PrefabRefData[entity];
                    parentRotation = this.m_TransformData[entity].m_Rotation;
                    while (this.m_OwnerData.HasComponent(entity) && !this.m_BuildingData.HasComponent(prefabRef.m_Prefab))
                    {
                        entity = this.m_OwnerData[entity].m_Owner;
                        prefabRef = this.m_PrefabRefData[entity];
                        if (this.m_TransformData.HasComponent(entity))
                        {
                            parentRotation = this.m_TransformData[entity].m_Rotation;
                        }
                    }
                }
                if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
                {
                    float3 @float = math.forward(controlPoint.m_Rotation);
                    float3 float2 = controlPoint.m_HitDirection;
                    float2.y = math.select(float2.y, 0f, (this.m_Snap & Snap.Upright) > Snap.None);
                    if (!MathUtils.TryNormalize(ref float2))
                    {
                        float2 = @float;
                        float2.y = math.select(float2.y, 0f, (this.m_Snap & Snap.Upright) > Snap.None);
                        if (!MathUtils.TryNormalize(ref float2))
                        {
                            float2 = new float3(0f, 0f, 1f);
                        }
                    }
                    float3 axis = math.cross(@float, float2);
                    if (MathUtils.TryNormalize(ref axis))
                    {
                        float angle = math.acos(math.clamp(math.dot(@float, float2), -1f, 1f));
                        controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(axis, angle), controlPoint.m_Rotation), quaternion.identity);
                        if (alignRotation)
                        {
                            ObjectToolSystem.SnapJob.AlignRotation(ref controlPoint.m_Rotation, parentRotation, true);
                        }
                    }
                    controlPoint.m_Position += math.forward(controlPoint.m_Rotation) * placeableObjectData.m_PlacementOffset.z;
                    return;
                }
                float3 float3 = math.rotate(controlPoint.m_Rotation, new float3(0f, 1f, 0f));
                float3 float4 = controlPoint.m_HitDirection;
                float4 = math.select(float4, new float3(0f, 1f, 0f), (this.m_Snap & Snap.Upright) > Snap.None);
                if (!MathUtils.TryNormalize(ref float4))
                {
                    float4 = float3;
                }
                float3 axis2 = math.cross(float3, float4);
                if (MathUtils.TryNormalize(ref axis2))
                {
                    float angle2 = math.acos(math.clamp(math.dot(float3, float4), -1f, 1f));
                    controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(axis2, angle2), controlPoint.m_Rotation), quaternion.identity);
                    if (alignRotation)
                    {
                        ObjectToolSystem.SnapJob.AlignRotation(ref controlPoint.m_Rotation, parentRotation, false);
                    }
                }
            }

            // Token: 0x06000E79 RID: 3705 RVA: 0x000901D4 File Offset: 0x0008E3D4
            private void CalculateHeight(ref ControlPoint controlPoint, float waterSurfaceHeight)
            {
                if (this.m_PlaceableObjectData.HasComponent(this.m_Prefab))
                {
                    PlaceableObjectData placeableObjectData = this.m_PlaceableObjectData[this.m_Prefab];
                    if (this.m_SubObjects.HasBuffer(controlPoint.m_OriginalEntity))
                    {
                        controlPoint.m_Position.y = controlPoint.m_Position.y + placeableObjectData.m_PlacementOffset.y;
                        return;
                    }
                    float num;
                    if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None && this.m_BuildingData.HasComponent(this.m_Prefab))
                    {
                        BuildingData buildingData = this.m_BuildingData[this.m_Prefab];
                        float3 worldPosition = BuildingUtils.CalculateFrontPosition(new Game.Objects.Transform(controlPoint.m_Position, controlPoint.m_Rotation), buildingData.m_LotSize.y);
                        num = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, worldPosition);
                    }
                    else
                    {
                        num = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, controlPoint.m_Position);
                    }
                    if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
                    {
                        float num2 = WaterUtils.SampleHeight(ref this.m_WaterSurfaceData, ref this.m_TerrainHeightData, controlPoint.m_Position);
                        num2 += placeableObjectData.m_PlacementOffset.y;
                        controlPoint.m_Elevation = math.max(0f, num2 - num);
                        num = math.max(num, num2);
                    }
                    else if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == Game.Objects.PlacementFlags.None)
                    {
                        num += placeableObjectData.m_PlacementOffset.y;
                    }
                    else
                    {
                        float num4;
                        float num3 = WaterUtils.SampleHeight(ref this.m_WaterSurfaceData, ref this.m_TerrainHeightData, controlPoint.m_Position, out num4);
                        if (num4 >= 0.2f)
                        {
                            num3 += placeableObjectData.m_PlacementOffset.y;
                            if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Floating) != Game.Objects.PlacementFlags.None)
                            {
                                controlPoint.m_Elevation = math.max(0f, num3 - num);
                            }
                            num = math.max(num, num3);
                        }
                    }
                    if ((this.m_Snap & Snap.Shoreline) != Snap.None)
                    {
                        num = math.max(num, waterSurfaceHeight + placeableObjectData.m_PlacementOffset.y);
                    }
                    controlPoint.m_Position.y = num;
                }
            }

            // Token: 0x06000E7A RID: 3706 RVA: 0x000903A4 File Offset: 0x0008E5A4
            private void SnapSurface(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, int parentMesh)
            {
                Game.Objects.Transform transform = this.m_TransformData[entity];
                ControlPoint controlPoint2 = controlPoint;
                controlPoint2.m_OriginalEntity = entity;
                controlPoint2.m_ElementIndex.x = parentMesh;
                controlPoint2.m_Position = controlPoint.m_HitPosition;
                controlPoint2.m_Direction = math.forward(transform.m_Rotation).xz;
                controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, controlPoint.m_HitPosition.xz, controlPoint2.m_Position.xz, controlPoint2.m_Direction);
                ObjectToolSystem.SnapJob.AddSnapPosition(ref bestPosition, controlPoint2);
            }

            // Token: 0x06000E7B RID: 3707 RVA: 0x00090438 File Offset: 0x0008E638
            private void SnapNode(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, Game.Net.Node node)
            {
                Bounds1 bounds = new Bounds1(float.MaxValue, float.MinValue);
                DynamicBuffer<ConnectedEdge> dynamicBuffer = this.m_ConnectedEdges[entity];
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity edge = dynamicBuffer[i].m_Edge;
                    Edge edge2 = this.m_EdgeData[edge];
                    if (edge2.m_Start == entity)
                    {
                        Composition composition = this.m_CompositionData[edge];
                        NetCompositionData netCompositionData = this.m_PrefabCompositionData[composition.m_StartNode];
                        bounds |= netCompositionData.m_SurfaceHeight;
                    }
                    else if (edge2.m_End == entity)
                    {
                        Composition composition2 = this.m_CompositionData[edge];
                        NetCompositionData netCompositionData2 = this.m_PrefabCompositionData[composition2.m_EndNode];
                        bounds |= netCompositionData2.m_SurfaceHeight;
                    }
                }
                ControlPoint controlPoint2 = controlPoint;
                controlPoint2.m_OriginalEntity = entity;
                controlPoint2.m_Position = node.m_Position;
                if (bounds.min < 3.4028235E+38f)
                {
                    controlPoint2.m_Position.y = controlPoint2.m_Position.y + bounds.min;
                }
                controlPoint2.m_Direction = math.normalizesafe(math.forward(node.m_Rotation), default(float3)).xz;
                controlPoint2.m_Rotation = node.m_Rotation;
                controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, controlPoint.m_HitPosition.xz, controlPoint2.m_Position.xz, controlPoint2.m_Direction);
                ObjectToolSystem.SnapJob.AddSnapPosition(ref bestPosition, controlPoint2);
            }

            // Token: 0x06000E7C RID: 3708 RVA: 0x000905CC File Offset: 0x0008E7CC
            private void SnapShoreline(ControlPoint controlPoint, ref ControlPoint bestPosition, ref float waterSurfaceHeight, float radius, float3 offset)
            {
                int2 @int = (int2)math.floor(WaterUtils.ToSurfaceSpace(ref this.m_WaterSurfaceData, controlPoint.m_HitPosition - radius).xz);
                int2 int2 = (int2)math.ceil(WaterUtils.ToSurfaceSpace(ref this.m_WaterSurfaceData, controlPoint.m_HitPosition + radius).xz);
                @int = math.max(@int, default(int2));
                int2 = math.min(int2, this.m_WaterSurfaceData.resolution.xz - 1);
                float3 @float = default(float3);
                float3 float2 = default(float3);
                float2 float3 = default(float2);
                for (int i = @int.y; i <= int2.y; i++)
                {
                    for (int j = @int.x; j <= int2.x; j++)
                    {
                        float3 worldPosition = WaterUtils.GetWorldPosition(ref this.m_WaterSurfaceData, new int2(j, i));
                        if (worldPosition.y > 0.2f)
                        {
                            float num = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, worldPosition) + worldPosition.y;
                            float num2 = math.max(0f, radius * radius - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
                            worldPosition.y = (worldPosition.y - 0.2f) * num2;
                            worldPosition.xz *= worldPosition.y;
                            float2 += worldPosition;
                            num *= num2;
                            float3 += new float2(num, num2);
                        }
                        else if (worldPosition.y < 0.2f)
                        {
                            float num3 = math.max(0f, radius * radius - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
                            worldPosition.y = (0.2f - worldPosition.y) * num3;
                            worldPosition.xz *= worldPosition.y;
                            @float += worldPosition;
                        }
                    }
                }
                if (@float.y != 0f && float2.y != 0f && float3.y != 0f)
                {
                    @float /= @float.y;
                    float2 /= float2.y;
                    float3 lhs = default(float3);
                    lhs.xz = @float.xz - float2.xz;
                    if (MathUtils.TryNormalize(ref lhs))
                    {
                        waterSurfaceHeight = float3.x / float3.y;
                        bestPosition = controlPoint;
                        bestPosition.m_Position.xz = math.lerp(float2.xz, @float.xz, 0.5f);
                        bestPosition.m_Position.y = waterSurfaceHeight + offset.y;
                        bestPosition.m_Position += lhs * offset.z;
                        bestPosition.m_Direction = lhs.xz;
                        bestPosition.m_Rotation = ToolUtils.CalculateRotation(bestPosition.m_Direction);
                        bestPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, controlPoint.m_HitPosition.xz, bestPosition.m_Position.xz, bestPosition.m_Direction);
                        bestPosition.m_OriginalEntity = Entity.Null;
                    }
                }
            }

            // Token: 0x06000E7D RID: 3709 RVA: 0x00090930 File Offset: 0x0008EB30
            private void SnapSegmentAreas(ControlPoint controlPoint, ref ControlPoint bestPosition, float radius, Entity entity, Segment segment1, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1)
            {
                for (int i = 0; i < areas1.Length; i++)
                {
                    NetCompositionArea netCompositionArea = areas1[i];
                    if ((netCompositionArea.m_Flags & NetAreaFlags.Buildable) != (NetAreaFlags)0)
                    {
                        float num = netCompositionArea.m_Width * 0.51f;
                        if (radius < num)
                        {
                            Bezier4x3 curve = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f);
                            float t;
                            MathUtils.Distance(curve.xz, controlPoint.m_HitPosition.xz, out t);
                            ControlPoint controlPoint2 = controlPoint;
                            controlPoint2.m_OriginalEntity = entity;
                            controlPoint2.m_Position = MathUtils.Position(curve, t);
                            controlPoint2.m_Direction = math.normalizesafe(MathUtils.Tangent(curve, t).xz, default(float2));
                            if ((netCompositionArea.m_Flags & NetAreaFlags.Invert) != (NetAreaFlags)0)
                            {
                                controlPoint2.m_Direction = MathUtils.Right(controlPoint2.m_Direction);
                            }
                            else
                            {
                                controlPoint2.m_Direction = MathUtils.Left(controlPoint2.m_Direction);
                            }
                            float3 @float = MathUtils.Position(MathUtils.Lerp(segment1.m_Left, segment1.m_Right, netCompositionArea.m_SnapPosition.x / prefabCompositionData1.m_Width + 0.5f), t);
                            float maxLength = math.max(0f, math.min(netCompositionArea.m_Width * 0.5f, math.abs(netCompositionArea.m_SnapPosition.x - netCompositionArea.m_Position.x) + netCompositionArea.m_SnapWidth * 0.5f) - radius);
                            float maxLength2 = math.max(0f, netCompositionArea.m_SnapWidth * 0.5f - radius);
                            controlPoint2.m_Position.xz = controlPoint2.m_Position.xz + MathUtils.ClampLength(@float.xz - controlPoint2.m_Position.xz, maxLength);
                            controlPoint2.m_Position.xz = controlPoint2.m_Position.xz + MathUtils.ClampLength(controlPoint.m_HitPosition.xz - controlPoint2.m_Position.xz, maxLength2);
                            controlPoint2.m_Position.y = controlPoint2.m_Position.y + netCompositionArea.m_Position.y;
                            controlPoint2.m_Rotation = ToolUtils.CalculateRotation(controlPoint2.m_Direction);
                            controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, controlPoint.m_HitPosition.xz, controlPoint2.m_Position.xz, controlPoint2.m_Direction);
                            ObjectToolSystem.SnapJob.AddSnapPosition(ref bestPosition, controlPoint2);
                        }
                    }
                }
            }

            // Token: 0x06000E7E RID: 3710 RVA: 0x0000FB07 File Offset: 0x0000DD07
            private static Bounds3 SetHeightRange(Bounds3 bounds, Bounds1 heightRange)
            {
                bounds.min.y = bounds.min.y + heightRange.min;
                bounds.max.y = bounds.max.y + heightRange.max;
                return bounds;
            }

            // Token: 0x06000E7F RID: 3711 RVA: 0x00090BB0 File Offset: 0x0008EDB0
            private static void CheckSnapLine(BuildingData buildingData, Game.Objects.Transform ownerTransformData, ControlPoint controlPoint, ref ControlPoint bestPosition, Line2 line, int maxOffset, float angle, bool forceSnap)
            {
                float num;
                MathUtils.Distance(line, controlPoint.m_Position.xz, out num);
                float num2 = math.select(0f, 4f, (buildingData.m_LotSize.x - buildingData.m_LotSize.y & 1) != 0);
                float num3 = (float)math.min(2 * maxOffset - buildingData.m_LotSize.y - buildingData.m_LotSize.x, buildingData.m_LotSize.y - buildingData.m_LotSize.x) * 4f;
                float num4 = math.distance(line.a, line.b);
                num *= num4;
                num = MathUtils.Snap(num + num2, 8f) - num2;
                num = math.clamp(num, -num3, num4 + num3);
                ControlPoint controlPoint2 = controlPoint;
                controlPoint2.m_OriginalEntity = Entity.Null;
                controlPoint2.m_Position.y = ownerTransformData.m_Position.y;
                controlPoint2.m_Position.xz = MathUtils.Position(line, num / num4);
                controlPoint2.m_Direction = math.mul(math.mul(ownerTransformData.m_Rotation, quaternion.RotateY(angle)), new float3(0f, 0f, 1f)).xz;
                controlPoint2.m_Rotation = ToolUtils.CalculateRotation(controlPoint2.m_Direction);
                float level = math.select(0f, 1f, forceSnap);
                controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, controlPoint.m_HitPosition.xz * 0.5f, controlPoint2.m_Position.xz * 0.5f, controlPoint2.m_Direction);
                ObjectToolSystem.SnapJob.AddSnapPosition(ref bestPosition, controlPoint2);
            }

            // Token: 0x06000E80 RID: 3712 RVA: 0x00090D5E File Offset: 0x0008EF5E
            private static void AddSnapPosition(ref ControlPoint bestSnapPosition, ControlPoint snapPosition)
            {
                if (ToolUtils.CompareSnapPriority(snapPosition.m_SnapPriority, bestSnapPosition.m_SnapPriority))
                {
                    bestSnapPosition = snapPosition;
                }
            }

            // Token: 0x04001859 RID: 6233
            [ReadOnly]
            public bool m_EditorMode;

            // Token: 0x0400185A RID: 6234
            [ReadOnly]
            public Snap m_Snap;

            // Token: 0x0400185B RID: 6235
            [ReadOnly]
            public ObjectToolSystem.Mode m_Mode;

            // Token: 0x0400185C RID: 6236
            [ReadOnly]
            public Entity m_Prefab;

            // Token: 0x0400185D RID: 6237
            [ReadOnly]
            public Entity m_Selected;

            // Token: 0x0400185E RID: 6238
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            // Token: 0x0400185F RID: 6239
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;

            // Token: 0x04001860 RID: 6240
            [ReadOnly]
            public ComponentLookup<Attached> m_AttachedData;

            // Token: 0x04001861 RID: 6241
            [ReadOnly]
            public ComponentLookup<Game.Common.Terrain> m_TerrainData;

            // Token: 0x04001862 RID: 6242
            [ReadOnly]
            public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

            // Token: 0x04001863 RID: 6243
            [ReadOnly]
            public ComponentLookup<Edge> m_EdgeData;

            // Token: 0x04001864 RID: 6244
            [ReadOnly]
            public ComponentLookup<Game.Net.Node> m_NodeData;

            // Token: 0x04001865 RID: 6245
            [ReadOnly]
            public ComponentLookup<Orphan> m_OrphanData;

            // Token: 0x04001866 RID: 6246
            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;

            // Token: 0x04001867 RID: 6247
            [ReadOnly]
            public ComponentLookup<Composition> m_CompositionData;

            // Token: 0x04001868 RID: 6248
            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            // Token: 0x04001869 RID: 6249
            [ReadOnly]
            public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

            // Token: 0x0400186A RID: 6250
            [ReadOnly]
            public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

            // Token: 0x0400186B RID: 6251
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            // Token: 0x0400186C RID: 6252
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

            // Token: 0x0400186D RID: 6253
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingData;

            // Token: 0x0400186E RID: 6254
            [ReadOnly]
            public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

            // Token: 0x0400186F RID: 6255
            [ReadOnly]
            public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

            // Token: 0x04001870 RID: 6256
            [ReadOnly]
            public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

            // Token: 0x04001871 RID: 6257
            [ReadOnly]
            public ComponentLookup<AssetStampData> m_AssetStampData;

            // Token: 0x04001872 RID: 6258
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> m_OutsideConnectionData;

            // Token: 0x04001873 RID: 6259
            [ReadOnly]
            public ComponentLookup<NetObjectData> m_NetObjectData;

            // Token: 0x04001874 RID: 6260
            [ReadOnly]
            public ComponentLookup<TransportStopData> m_TransportStopData;

            // Token: 0x04001875 RID: 6261
            [ReadOnly]
            public ComponentLookup<StackData> m_StackData;

            // Token: 0x04001876 RID: 6262
            [ReadOnly]
            public ComponentLookup<ServiceUpgradeData> m_ServiceUpgradeData;

            // Token: 0x04001877 RID: 6263
            [ReadOnly]
            public ComponentLookup<Block> m_BlockData;

            // Token: 0x04001878 RID: 6264
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjects;

            // Token: 0x04001879 RID: 6265
            [ReadOnly]
            public BufferLookup<ConnectedEdge> m_ConnectedEdges;

            // Token: 0x0400187A RID: 6266
            [ReadOnly]
            public BufferLookup<NetCompositionArea> m_PrefabCompositionAreas;

            // Token: 0x0400187B RID: 6267
            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

            // Token: 0x0400187C RID: 6268
            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            // Token: 0x0400187D RID: 6269
            [ReadOnly]
            public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

            // Token: 0x0400187E RID: 6270
            [ReadOnly]
            public WaterSurfaceData m_WaterSurfaceData;

            // Token: 0x0400187F RID: 6271
            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            // Token: 0x04001880 RID: 6272
            public NativeList<ControlPoint> m_ControlPoints;

            // Token: 0x04001881 RID: 6273
            public NativeValue<ObjectToolSystem.Rotation> m_Rotation;

            // Token: 0x02000316 RID: 790
            private struct LoweredParentIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                // Token: 0x06000E81 RID: 3713 RVA: 0x00090D7A File Offset: 0x0008EF7A
                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds.xz, this.m_Position.xz);
                }

                // Token: 0x06000E82 RID: 3714 RVA: 0x00090D98 File Offset: 0x0008EF98
                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds.xz, this.m_Position.xz))
                    {
                        return;
                    }
                    if (this.m_EdgeGeometryData.HasComponent(entity))
                    {
                        this.CheckEdge(entity);
                        return;
                    }
                    if (this.m_OrphanData.HasComponent(entity))
                    {
                        this.CheckNode(entity);
                    }
                }

                // Token: 0x06000E83 RID: 3715 RVA: 0x00090DF0 File Offset: 0x0008EFF0
                private void CheckNode(Entity entity)
                {
                    Game.Net.Node node = this.m_NodeData[entity];
                    Orphan orphan = this.m_OrphanData[entity];
                    NetCompositionData netCompositionData = this.m_PrefabCompositionData[orphan.m_Composition];
                    if ((netCompositionData.m_State & CompositionState.Marker) == (CompositionState)0 && ((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != (CompositionFlags.Side)0U)
                    {
                        float3 position = node.m_Position;
                        position.y += netCompositionData.m_SurfaceHeight.max;
                        if (math.distance(this.m_Position.xz, position.xz) <= netCompositionData.m_Width * 0.5f)
                        {
                            this.m_Result.m_OriginalEntity = entity;
                            this.m_Result.m_Position = node.m_Position;
                            this.m_Result.m_HitPosition = this.m_Position;
                            this.m_Result.m_HitPosition.y = position.y;
                            this.m_Result.m_HitDirection = default(float3);
                        }
                    }
                }

                // Token: 0x06000E84 RID: 3716 RVA: 0x00090EF0 File Offset: 0x0008F0F0
                private void CheckEdge(Entity entity)
                {
                    EdgeGeometry edgeGeometry = this.m_EdgeGeometryData[entity];
                    EdgeNodeGeometry geometry = this.m_StartNodeGeometryData[entity].m_Geometry;
                    EdgeNodeGeometry geometry2 = this.m_EndNodeGeometryData[entity].m_Geometry;
                    bool3 @bool;
                    @bool.x = MathUtils.Intersect(edgeGeometry.m_Bounds.xz, this.m_Position.xz);
                    @bool.y = MathUtils.Intersect(geometry.m_Bounds.xz, this.m_Position.xz);
                    @bool.z = MathUtils.Intersect(geometry2.m_Bounds.xz, this.m_Position.xz);
                    if (!math.any(@bool))
                    {
                        return;
                    }
                    Composition composition = this.m_CompositionData[entity];
                    Edge edge = this.m_EdgeData[entity];
                    Curve curve = this.m_CurveData[entity];
                    if (@bool.x)
                    {
                        NetCompositionData netCompositionData = this.m_PrefabCompositionData[composition.m_Edge];
                        if ((netCompositionData.m_State & CompositionState.Marker) == (CompositionState)0 && ((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != (CompositionFlags.Side)0U)
                        {
                            this.CheckSegment(entity, edgeGeometry.m_Start, curve.m_Bezier, netCompositionData);
                            this.CheckSegment(entity, edgeGeometry.m_End, curve.m_Bezier, netCompositionData);
                        }
                    }
                    if (@bool.y)
                    {
                        NetCompositionData netCompositionData2 = this.m_PrefabCompositionData[composition.m_StartNode];
                        if ((netCompositionData2.m_State & CompositionState.Marker) == (CompositionState)0 && ((netCompositionData2.m_Flags.m_Left | netCompositionData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != (CompositionFlags.Side)0U)
                        {
                            if (geometry.m_MiddleRadius > 0f)
                            {
                                this.CheckSegment(edge.m_Start, geometry.m_Left, curve.m_Bezier, netCompositionData2);
                                Segment right = geometry.m_Right;
                                Segment right2 = geometry.m_Right;
                                right.m_Right = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
                                right2.m_Left = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
                                right.m_Right.d = geometry.m_Middle.d;
                                right2.m_Left.d = geometry.m_Middle.d;
                                this.CheckSegment(edge.m_Start, right, curve.m_Bezier, netCompositionData2);
                                this.CheckSegment(edge.m_Start, right2, curve.m_Bezier, netCompositionData2);
                            }
                            else
                            {
                                Segment left = geometry.m_Left;
                                Segment right3 = geometry.m_Right;
                                this.CheckSegment(edge.m_Start, left, curve.m_Bezier, netCompositionData2);
                                this.CheckSegment(edge.m_Start, right3, curve.m_Bezier, netCompositionData2);
                                left.m_Right = geometry.m_Middle;
                                right3.m_Left = geometry.m_Middle;
                                this.CheckSegment(edge.m_Start, left, curve.m_Bezier, netCompositionData2);
                                this.CheckSegment(edge.m_Start, right3, curve.m_Bezier, netCompositionData2);
                            }
                        }
                    }
                    if (@bool.z)
                    {
                        NetCompositionData netCompositionData3 = this.m_PrefabCompositionData[composition.m_EndNode];
                        if ((netCompositionData3.m_State & CompositionState.Marker) == (CompositionState)0 && ((netCompositionData3.m_Flags.m_Left | netCompositionData3.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != (CompositionFlags.Side)0U)
                        {
                            if (geometry2.m_MiddleRadius > 0f)
                            {
                                this.CheckSegment(edge.m_End, geometry2.m_Left, curve.m_Bezier, netCompositionData3);
                                Segment right4 = geometry2.m_Right;
                                Segment right5 = geometry2.m_Right;
                                right4.m_Right = MathUtils.Lerp(geometry2.m_Right.m_Left, geometry2.m_Right.m_Right, 0.5f);
                                right4.m_Right.d = geometry2.m_Middle.d;
                                right5.m_Left = right4.m_Right;
                                this.CheckSegment(edge.m_End, right4, curve.m_Bezier, netCompositionData3);
                                this.CheckSegment(edge.m_End, right5, curve.m_Bezier, netCompositionData3);
                                return;
                            }
                            Segment left2 = geometry2.m_Left;
                            Segment right6 = geometry2.m_Right;
                            this.CheckSegment(edge.m_End, left2, curve.m_Bezier, netCompositionData3);
                            this.CheckSegment(edge.m_End, right6, curve.m_Bezier, netCompositionData3);
                            left2.m_Right = geometry2.m_Middle;
                            right6.m_Left = geometry2.m_Middle;
                            this.CheckSegment(edge.m_End, left2, curve.m_Bezier, netCompositionData3);
                            this.CheckSegment(edge.m_End, right6, curve.m_Bezier, netCompositionData3);
                        }
                    }
                }

                // Token: 0x06000E85 RID: 3717 RVA: 0x0009139C File Offset: 0x0008F59C
                private void CheckSegment(Entity entity, Segment segment, Bezier4x3 curve, NetCompositionData prefabCompositionData)
                {
                    float3 a = segment.m_Left.a;
                    float3 @float = segment.m_Right.a;
                    for (int i = 1; i <= 8; i++)
                    {
                        float t = (float)i / 8f;
                        float3 float2 = MathUtils.Position(segment.m_Left, t);
                        float3 float3 = MathUtils.Position(segment.m_Right, t);
                        Triangle3 triangle = new Triangle3(a, @float, float2);
                        Triangle3 triangle2 = new Triangle3(float3, float2, @float);
                        float2 t2;
                        if (MathUtils.Intersect(triangle.xz, this.m_Position.xz, out t2))
                        {
                            float3 position = this.m_Position;
                            position.y = MathUtils.Position(triangle.y, t2) + prefabCompositionData.m_SurfaceHeight.max;
                            float num;
                            MathUtils.Distance(curve.xz, position.xz, out num);
                            this.m_Result.m_OriginalEntity = entity;
                            this.m_Result.m_Position = MathUtils.Position(curve, num);
                            this.m_Result.m_HitPosition = position;
                            this.m_Result.m_HitDirection = default(float3);
                            this.m_Result.m_CurvePosition = num;
                        }
                        else if (MathUtils.Intersect(triangle2.xz, this.m_Position.xz, out t2))
                        {
                            float3 position2 = this.m_Position;
                            position2.y = MathUtils.Position(triangle2.y, t2) + prefabCompositionData.m_SurfaceHeight.max;
                            float num2;
                            MathUtils.Distance(curve.xz, position2.xz, out num2);
                            this.m_Result.m_OriginalEntity = entity;
                            this.m_Result.m_Position = MathUtils.Position(curve, num2);
                            this.m_Result.m_HitPosition = position2;
                            this.m_Result.m_HitDirection = default(float3);
                            this.m_Result.m_CurvePosition = num2;
                        }
                        a = float2;
                        @float = float3;
                    }
                }

                // Token: 0x04001882 RID: 6274
                public ControlPoint m_Result;

                // Token: 0x04001883 RID: 6275
                public float3 m_Position;

                // Token: 0x04001884 RID: 6276
                public ComponentLookup<Edge> m_EdgeData;

                // Token: 0x04001885 RID: 6277
                public ComponentLookup<Game.Net.Node> m_NodeData;

                // Token: 0x04001886 RID: 6278
                public ComponentLookup<Orphan> m_OrphanData;

                // Token: 0x04001887 RID: 6279
                public ComponentLookup<Curve> m_CurveData;

                // Token: 0x04001888 RID: 6280
                public ComponentLookup<Composition> m_CompositionData;

                // Token: 0x04001889 RID: 6281
                public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

                // Token: 0x0400188A RID: 6282
                public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

                // Token: 0x0400188B RID: 6283
                public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

                // Token: 0x0400188C RID: 6284
                public ComponentLookup<NetCompositionData> m_PrefabCompositionData;
            }

            // Token: 0x02000317 RID: 791
            private struct OriginalObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                // Token: 0x06000E86 RID: 3718 RVA: 0x0009156A File Offset: 0x0008F76A
                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
                }

                // Token: 0x06000E87 RID: 3719 RVA: 0x00091580 File Offset: 0x0008F780
                public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds))
                    {
                        return;
                    }
                    if (!this.m_AttachedData.HasComponent(item) || (!this.m_EditorMode && this.m_OwnerData.HasComponent(item)))
                    {
                        return;
                    }
                    if (this.m_AttachedData[item].m_Parent != this.m_Parent)
                    {
                        return;
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[item];
                    if (!this.m_NetObjectData.HasComponent(prefabRef.m_Prefab))
                    {
                        return;
                    }
                    TransportStopData transportStopData = default(TransportStopData);
                    if (this.m_TransportStopData.HasComponent(prefabRef.m_Prefab))
                    {
                        transportStopData = this.m_TransportStopData[prefabRef.m_Prefab];
                    }
                    if (this.m_TransportStopData1.m_TransportType != transportStopData.m_TransportType)
                    {
                        return;
                    }
                    float num = math.distance(MathUtils.Center(this.m_Bounds), MathUtils.Center(bounds.m_Bounds));
                    if (num < this.m_BestDistance)
                    {
                        this.m_Result = item;
                        this.m_BestDistance = num;
                    }
                }

                // Token: 0x0400188D RID: 6285
                public Entity m_Parent;

                // Token: 0x0400188E RID: 6286
                public Entity m_Result;

                // Token: 0x0400188F RID: 6287
                public Bounds3 m_Bounds;

                // Token: 0x04001890 RID: 6288
                public float m_BestDistance;

                // Token: 0x04001891 RID: 6289
                public bool m_EditorMode;

                // Token: 0x04001892 RID: 6290
                public TransportStopData m_TransportStopData1;

                // Token: 0x04001893 RID: 6291
                public ComponentLookup<Owner> m_OwnerData;

                // Token: 0x04001894 RID: 6292
                public ComponentLookup<Attached> m_AttachedData;

                // Token: 0x04001895 RID: 6293
                public ComponentLookup<PrefabRef> m_PrefabRefData;

                // Token: 0x04001896 RID: 6294
                public ComponentLookup<NetObjectData> m_NetObjectData;

                // Token: 0x04001897 RID: 6295
                public ComponentLookup<TransportStopData> m_TransportStopData;
            }

            // Token: 0x02000318 RID: 792
            private struct ParentObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                // Token: 0x06000E88 RID: 3720 RVA: 0x0009167A File Offset: 0x0008F87A
                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds.xz, this.m_Bounds.xz);
                }

                // Token: 0x06000E89 RID: 3721 RVA: 0x00091698 File Offset: 0x0008F898
                public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds.xz, this.m_Bounds.xz))
                    {
                        return;
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[item];
                    bool flag = this.m_BuildingData.HasComponent(prefabRef.m_Prefab);
                    bool flag2 = this.m_AssetStampData.HasComponent(prefabRef.m_Prefab);
                    if (this.m_IsBuilding && !flag2)
                    {
                        return;
                    }
                    float num = this.m_BestOverlap;
                    if (flag || flag2)
                    {
                        Game.Objects.Transform transform = this.m_TransformData[item];
                        ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                        float3 rhs = MathUtils.Center(bounds.m_Bounds);
                        if ((this.m_PrefabObjectGeometryData1.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                        {
                            Circle2 circle = new Circle2(this.m_PrefabObjectGeometryData1.m_Size.x * 0.5f - 0.01f, (this.m_ControlPoint.m_Position - rhs).xz);
                            Bounds2 bounds2;
                            if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                            {
                                Circle2 circle2 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - rhs).xz);
                                if (MathUtils.Intersect(circle, circle2))
                                {
                                    num = math.distance(new float3
                                    {
                                        xz = rhs.xz + MathUtils.Center(MathUtils.Bounds(circle) & MathUtils.Bounds(circle2)),
                                        y = MathUtils.Center(bounds.m_Bounds.y & this.m_Bounds.y)
                                    }, this.m_ControlPoint.m_Position);
                                }
                            }
                            else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position - rhs, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz, circle, out bounds2))
                            {
                                num = math.distance(new float3
                                {
                                    xz = rhs.xz + MathUtils.Center(bounds2),
                                    y = MathUtils.Center(bounds.m_Bounds.y & this.m_Bounds.y)
                                }, this.m_ControlPoint.m_Position);
                            }
                        }
                        else
                        {
                            Quad2 xz = ObjectUtils.CalculateBaseCorners(this.m_ControlPoint.m_Position - rhs, this.m_ControlPoint.m_Rotation, MathUtils.Expand(this.m_PrefabObjectGeometryData1.m_Bounds, -0.01f)).xz;
                            if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                            {
                                Circle2 circle3 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - rhs).xz);
                                Bounds2 bounds3;
                                if (MathUtils.Intersect(xz, circle3, out bounds3))
                                {
                                    num = math.distance(new float3
                                    {
                                        xz = rhs.xz + MathUtils.Center(bounds3),
                                        y = MathUtils.Center(bounds.m_Bounds.y & this.m_Bounds.y)
                                    }, this.m_ControlPoint.m_Position);
                                }
                            }
                            else
                            {
                                Quad2 xz2 = ObjectUtils.CalculateBaseCorners(transform.m_Position - rhs, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz;
                                Bounds2 bounds4;
                                if (MathUtils.Intersect(xz, xz2, out bounds4))
                                {
                                    num = math.distance(new float3
                                    {
                                        xz = rhs.xz + MathUtils.Center(bounds4),
                                        y = MathUtils.Center(bounds.m_Bounds.y & this.m_Bounds.y)
                                    }, this.m_ControlPoint.m_Position);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds))
                        {
                            return;
                        }
                        if (!this.m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
                        {
                            return;
                        }
                        Game.Objects.Transform transform2 = this.m_TransformData[item];
                        ObjectGeometryData objectGeometryData2 = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                        float3 @float = MathUtils.Center(bounds.m_Bounds);
                        quaternion q = math.inverse(this.m_ControlPoint.m_Rotation);
                        quaternion q2 = math.inverse(transform2.m_Rotation);
                        float3 float2 = math.mul(q, this.m_ControlPoint.m_Position - @float);
                        float3 float3 = math.mul(q2, transform2.m_Position - @float);
                        if ((this.m_PrefabObjectGeometryData1.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                        {
                            Cylinder3 cylinder = default(Cylinder3);
                            cylinder.circle = new Circle2(this.m_PrefabObjectGeometryData1.m_Size.x * 0.5f - 0.01f, float2.xz);
                            cylinder.height = new Bounds1(0.01f, this.m_PrefabObjectGeometryData1.m_Size.y - 0.01f) + float2.y;
                            cylinder.rotation = this.m_ControlPoint.m_Rotation;
                            if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                            {
                                Cylinder3 cylinder2 = default(Cylinder3);
                                cylinder2.circle = new Circle2(objectGeometryData2.m_Size.x * 0.5f - 0.01f, float3.xz);
                                cylinder2.height = new Bounds1(0.01f, objectGeometryData2.m_Size.y - 0.01f) + float3.y;
                                cylinder2.rotation = transform2.m_Rotation;
                                float3 x = default(float3);
                                if (Game.Objects.ValidationHelpers.Intersect(cylinder, cylinder2, ref x))
                                {
                                    num = math.distance(x, this.m_ControlPoint.m_Position);
                                }
                            }
                            else
                            {
                                Box3 box = default(Box3);
                                box.bounds = objectGeometryData2.m_Bounds + float3;
                                box.bounds = MathUtils.Expand(box.bounds, -0.01f);
                                box.rotation = transform2.m_Rotation;
                                Bounds3 bounds5;
                                Bounds3 bounds6;
                                if (MathUtils.Intersect(cylinder, box, out bounds5, out bounds6))
                                {
                                    float3 x2 = math.mul(cylinder.rotation, MathUtils.Center(bounds5));
                                    float3 y = math.mul(box.rotation, MathUtils.Center(bounds6));
                                    num = math.distance(@float + math.lerp(x2, y, 0.5f), this.m_ControlPoint.m_Position);
                                }
                            }
                        }
                        else
                        {
                            Box3 box2 = default(Box3);
                            box2.bounds = this.m_PrefabObjectGeometryData1.m_Bounds + float2;
                            box2.bounds = MathUtils.Expand(box2.bounds, -0.01f);
                            box2.rotation = this.m_ControlPoint.m_Rotation;
                            if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
                            {
                                Cylinder3 cylinder3 = new Cylinder3
                                {
                                    circle = new Circle2(objectGeometryData2.m_Size.x * 0.5f - 0.01f, float3.xz),
                                    height = new Bounds1(0.01f, objectGeometryData2.m_Size.y - 0.01f) + float3.y,
                                    rotation = transform2.m_Rotation
                                };
                                Bounds3 bounds7;
                                Bounds3 bounds8;
                                if (MathUtils.Intersect(cylinder3, box2, out bounds7, out bounds8))
                                {
                                    float3 x3 = math.mul(box2.rotation, MathUtils.Center(bounds8));
                                    float3 y2 = math.mul(cylinder3.rotation, MathUtils.Center(bounds7));
                                    num = math.distance(@float + math.lerp(x3, y2, 0.5f), this.m_ControlPoint.m_Position);
                                }
                            }
                            else
                            {
                                Box3 box3 = default(Box3);
                                box3.bounds = objectGeometryData2.m_Bounds + float3;
                                box3.bounds = MathUtils.Expand(box3.bounds, -0.01f);
                                box3.rotation = transform2.m_Rotation;
                                Bounds3 bounds9;
                                Bounds3 bounds10;
                                if (MathUtils.Intersect(box2, box3, out bounds9, out bounds10))
                                {
                                    float3 x4 = math.mul(box2.rotation, MathUtils.Center(bounds9));
                                    float3 y3 = math.mul(box3.rotation, MathUtils.Center(bounds10));
                                    num = math.distance(@float + math.lerp(x4, y3, 0.5f), this.m_ControlPoint.m_Position);
                                }
                            }
                        }
                    }
                    if (num < this.m_BestOverlap)
                    {
                        this.m_BestSnapPosition = this.m_ControlPoint;
                        this.m_BestSnapPosition.m_OriginalEntity = item;
                        this.m_BestSnapPosition.m_ElementIndex = new int2(-1, -1);
                        this.m_BestOverlap = num;
                    }
                }

                // Token: 0x04001898 RID: 6296
                public ControlPoint m_ControlPoint;

                // Token: 0x04001899 RID: 6297
                public ControlPoint m_BestSnapPosition;

                // Token: 0x0400189A RID: 6298
                public Bounds3 m_Bounds;

                // Token: 0x0400189B RID: 6299
                public float m_BestOverlap;

                // Token: 0x0400189C RID: 6300
                public bool m_IsBuilding;

                // Token: 0x0400189D RID: 6301
                public ObjectGeometryData m_PrefabObjectGeometryData1;

                // Token: 0x0400189E RID: 6302
                public ComponentLookup<Game.Objects.Transform> m_TransformData;

                // Token: 0x0400189F RID: 6303
                public ComponentLookup<PrefabRef> m_PrefabRefData;

                // Token: 0x040018A0 RID: 6304
                public ComponentLookup<BuildingData> m_BuildingData;

                // Token: 0x040018A1 RID: 6305
                public ComponentLookup<AssetStampData> m_AssetStampData;

                // Token: 0x040018A2 RID: 6306
                public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;
            }

            // Token: 0x02000319 RID: 793
            private struct ZoneBlockIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
            {
                // Token: 0x06000E8A RID: 3722 RVA: 0x00091F4B File Offset: 0x0009014B
                public bool Intersect(Bounds2 bounds)
                {
                    return MathUtils.Intersect(bounds, this.m_Bounds);
                }

                // Token: 0x06000E8B RID: 3723 RVA: 0x00091F5C File Offset: 0x0009015C
                public void Iterate(Bounds2 bounds, Entity blockEntity)
                {
                    if (!MathUtils.Intersect(bounds, this.m_Bounds))
                    {
                        return;
                    }
                    if (this.m_IgnoreOwner != Entity.Null)
                    {
                        Entity entity = blockEntity;
                        Owner owner;
                        while (this.m_OwnerData.TryGetComponent(entity, out owner))
                        {
                            if (owner.m_Owner == this.m_IgnoreOwner)
                            {
                                return;
                            }
                            entity = owner.m_Owner;
                        }
                    }
                    Block block = this.m_BlockData[blockEntity];
                    Quad2 quad = ZoneUtils.CalculateCorners(block);
                    Line2.Segment line = new Line2.Segment(quad.a, quad.b);
                    Line2.Segment line2 = new Line2.Segment(this.m_ControlPoint.m_HitPosition.xz, this.m_ControlPoint.m_HitPosition.xz);
                    float2 rhs = this.m_Direction * (math.max(0f, (float)(this.m_LotSize.y - this.m_LotSize.x)) * 4f);
                    line2.a -= rhs;
                    line2.b += rhs;
                    float2 @float;
                    float num = MathUtils.Distance(line, line2, out @float);
                    if (num == 0f)
                    {
                        num -= 0.5f - math.abs(@float.y - 0.5f);
                    }
                    if (num >= this.m_BestDistance)
                    {
                        return;
                    }
                    this.m_BestDistance = num;
                    float2 y = this.m_ControlPoint.m_HitPosition.xz - block.m_Position.xz;
                    float2 float2 = MathUtils.Left(block.m_Direction);
                    float num2 = (float)block.m_Size.y * 4f;
                    float num3 = (float)this.m_LotSize.y * 4f;
                    float num4 = math.dot(block.m_Direction, y);
                    float num5 = math.dot(float2, y);
                    float num6 = math.select(0f, 0.5f, ((block.m_Size.x ^ this.m_LotSize.x) & 1) != 0);
                    num5 -= (math.round(num5 / 8f - num6) + num6) * 8f;
                    this.m_BestSnapPosition = this.m_ControlPoint;
                    this.m_BestSnapPosition.m_Position = this.m_ControlPoint.m_HitPosition;
                    this.m_BestSnapPosition.m_Position.xz = this.m_BestSnapPosition.m_Position.xz + block.m_Direction * (num2 - num3 - num4);
                    this.m_BestSnapPosition.m_Position.xz = this.m_BestSnapPosition.m_Position.xz - float2 * num5;
                    this.m_BestSnapPosition.m_Direction = block.m_Direction;
                    this.m_BestSnapPosition.m_Rotation = ToolUtils.CalculateRotation(this.m_BestSnapPosition.m_Direction);
                    this.m_BestSnapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, this.m_ControlPoint.m_HitPosition.xz * 0.5f, this.m_BestSnapPosition.m_Position.xz * 0.5f, this.m_BestSnapPosition.m_Direction);
                    this.m_BestSnapPosition.m_OriginalEntity = blockEntity;
                }

                // Token: 0x040018A3 RID: 6307
                public ControlPoint m_ControlPoint;

                // Token: 0x040018A4 RID: 6308
                public ControlPoint m_BestSnapPosition;

                // Token: 0x040018A5 RID: 6309
                public float m_BestDistance;

                // Token: 0x040018A6 RID: 6310
                public int2 m_LotSize;

                // Token: 0x040018A7 RID: 6311
                public Bounds2 m_Bounds;

                // Token: 0x040018A8 RID: 6312
                public float2 m_Direction;

                // Token: 0x040018A9 RID: 6313
                public Entity m_IgnoreOwner;

                // Token: 0x040018AA RID: 6314
                public ComponentLookup<Owner> m_OwnerData;

                // Token: 0x040018AB RID: 6315
                public ComponentLookup<Block> m_BlockData;
            }
        }

        // Token: 0x0200031A RID: 794
        [BurstCompile]
        private struct FindAttachmentBuildingJob : IJob
        {
            // Token: 0x06000E8C RID: 3724 RVA: 0x00092274 File Offset: 0x00090474
            public void Execute()
            {
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(2000000);
                int2 lotSize = this.m_BuildingData.m_LotSize;
                bool2 lhs = new bool2((this.m_BuildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) > (Game.Prefabs.BuildingFlags)0U, (this.m_BuildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) > (Game.Prefabs.BuildingFlags)0U);
                ObjectToolBaseSystem.AttachmentData attachmentData = default(ObjectToolBaseSystem.AttachmentData);
                BuildingData buildingData = default(BuildingData);
                float num = 0f;
                for (int i = 0; i < this.m_Chunks.Length; i++)
                {
                    ArchetypeChunk archetypeChunk = this.m_Chunks[i];
                    NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(this.m_EntityType);
                    NativeArray<BuildingData> nativeArray2 = archetypeChunk.GetNativeArray<BuildingData>(ref this.m_BuildingDataType);
                    NativeArray<SpawnableBuildingData> nativeArray3 = archetypeChunk.GetNativeArray<SpawnableBuildingData>(ref this.m_SpawnableBuildingType);
                    for (int j = 0; j < nativeArray3.Length; j++)
                    {
                        if (nativeArray3[j].m_Level == 1)
                        {
                            BuildingData buildingData2 = nativeArray2[j];
                            int2 lotSize2 = buildingData2.m_LotSize;
                            bool2 rhs = new bool2((buildingData2.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) > (Game.Prefabs.BuildingFlags)0U, (buildingData2.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) > (Game.Prefabs.BuildingFlags)0U);
                            if (math.all(lotSize2 <= lotSize))
                            {
                                int2 @int = math.select(lotSize - lotSize2, 0, lotSize2 == lotSize - 1);
                                float num2 = (float)(lotSize2.x * lotSize2.y) * random.NextFloat(1f, 1.05f);
                                num2 += (float)(@int.x * lotSize2.y) * random.NextFloat(0.95f, 1f);
                                num2 += (float)(lotSize.x * @int.y) * random.NextFloat(0.55f, 0.6f);
                                num2 /= (float)(lotSize.x * lotSize.y);
                                num2 *= math.csum(math.select(0.01f, 0.5f, lhs == rhs));
                                if (num2 > num)
                                {
                                    attachmentData.m_Entity = nativeArray[j];
                                    buildingData = buildingData2;
                                    num = num2;
                                }
                            }
                        }
                    }
                }
                if (attachmentData.m_Entity != Entity.Null)
                {
                    float z = (float)(this.m_BuildingData.m_LotSize.y - buildingData.m_LotSize.y) * 4f;
                    attachmentData.m_Offset = new float3(0f, 0f, z);
                }
                this.m_AttachmentPrefab.Value = attachmentData;
            }

            // Token: 0x040018AC RID: 6316
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040018AD RID: 6317
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> m_BuildingDataType;

            // Token: 0x040018AE RID: 6318
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

            // Token: 0x040018AF RID: 6319
            [ReadOnly]
            public BuildingData m_BuildingData;

            // Token: 0x040018B0 RID: 6320
            [ReadOnly]
            public RandomSeed m_RandomSeed;

            // Token: 0x040018B1 RID: 6321
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_Chunks;

            // Token: 0x040018B2 RID: 6322
            public NativeReference<ObjectToolBaseSystem.AttachmentData> m_AttachmentPrefab;
        }

        // Token: 0x0200031B RID: 795
        private struct TypeHandle
        {
            // Token: 0x06000E8D RID: 3725 RVA: 0x000924FC File Offset: 0x000906FC
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(true);
                this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                this.__Game_Common_Terrain_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Terrain>(true);
                this.__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(true);
                this.__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(true);
                this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(true);
                this.__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(true);
                this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);
                this.__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(true);
                this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(true);
                this.__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(true);
                this.__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(true);
                this.__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(true);
                this.__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(true);
                this.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(true);
                this.__Game_Prefabs_NetObjectData_RO_ComponentLookup = state.GetComponentLookup<NetObjectData>(true);
                this.__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(true);
                this.__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(true);
                this.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(true);
                this.__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(true);
                this.__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(true);
                this.__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(true);
                this.__Game_Prefabs_NetCompositionArea_RO_BufferLookup = state.GetBufferLookup<NetCompositionArea>(true);
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
            }

            // Token: 0x040018B3 RID: 6323
            [ReadOnly]
            public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

            // Token: 0x040018B4 RID: 6324
            [ReadOnly]
            public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

            // Token: 0x040018B5 RID: 6325
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x040018B6 RID: 6326
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            // Token: 0x040018B7 RID: 6327
            [ReadOnly]
            public ComponentLookup<Game.Common.Terrain> __Game_Common_Terrain_RO_ComponentLookup;

            // Token: 0x040018B8 RID: 6328
            [ReadOnly]
            public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

            // Token: 0x040018B9 RID: 6329
            [ReadOnly]
            public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

            // Token: 0x040018BA RID: 6330
            [ReadOnly]
            public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

            // Token: 0x040018BB RID: 6331
            [ReadOnly]
            public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

            // Token: 0x040018BC RID: 6332
            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

            // Token: 0x040018BD RID: 6333
            [ReadOnly]
            public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

            // Token: 0x040018BE RID: 6334
            [ReadOnly]
            public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

            // Token: 0x040018BF RID: 6335
            [ReadOnly]
            public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

            // Token: 0x040018C0 RID: 6336
            [ReadOnly]
            public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

            // Token: 0x040018C1 RID: 6337
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x040018C2 RID: 6338
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

            // Token: 0x040018C3 RID: 6339
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x040018C4 RID: 6340
            [ReadOnly]
            public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

            // Token: 0x040018C5 RID: 6341
            [ReadOnly]
            public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

            // Token: 0x040018C6 RID: 6342
            [ReadOnly]
            public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

            // Token: 0x040018C7 RID: 6343
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

            // Token: 0x040018C8 RID: 6344
            [ReadOnly]
            public ComponentLookup<NetObjectData> __Game_Prefabs_NetObjectData_RO_ComponentLookup;

            // Token: 0x040018C9 RID: 6345
            [ReadOnly]
            public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

            // Token: 0x040018CA RID: 6346
            [ReadOnly]
            public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

            // Token: 0x040018CB RID: 6347
            [ReadOnly]
            public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

            // Token: 0x040018CC RID: 6348
            [ReadOnly]
            public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

            // Token: 0x040018CD RID: 6349
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

            // Token: 0x040018CE RID: 6350
            [ReadOnly]
            public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

            // Token: 0x040018CF RID: 6351
            [ReadOnly]
            public BufferLookup<NetCompositionArea> __Game_Prefabs_NetCompositionArea_RO_BufferLookup;

            // Token: 0x040018D0 RID: 6352
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x040018D1 RID: 6353
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

            // Token: 0x040018D2 RID: 6354
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
        }
    }
}
