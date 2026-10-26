using Colossal.Entities;
using Colossal.Mathematics;
using ExtraDetailingTools.Components;
using ExtraDetailingTools.Gizmos;
using Game;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;
using Game.Vehicles;
using ExtraDetailingTools.Systems;
using ExtraDetailingTools.Systems.Duplicate;
using ExtraDetailingTools.Snapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Game.Input.UIBaseInputAction;
using static Game.Tools.DefaultToolSystem;
using Color = UnityEngine.Color;
using Node = Game.Areas.Node;
using RaycastHit = Game.Common.RaycastHit;
using RaycastResult = Game.Common.RaycastResult;
using ServiceUpgrade = Game.Buildings.ServiceUpgrade;
using SubArea = Game.Areas.SubArea;
using SubNet = Game.Net.SubNet;
using SubObject = Game.Objects.SubObject;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool : ToolBaseSystem
    {

#if RELEASE
        [BurstCompile]
#endif
        private struct CreateDefinitionsJob : IJob
        {
            // Selection roots (no entity owned by another one of the list) + the hovered entity in Default mode.
            [ReadOnly]
            public NativeArray<Entity> m_Entities;

            [ReadOnly]
            public NativeArray<bool> m_AllowHighlights;

            // Highlighted in green. Entity.Null with a single selection.
            [ReadOnly]
            public Entity m_Primary;

            // Originals that already got a definition, so overlapping selections don't create the same temp twice.
            public NativeParallelHashSet<Entity> m_Added;

            private Entity m_Entity;

            private bool m_AllowHighlight;

            [ReadOnly]
            public float3 m_Position;

            [ReadOnly]
            public bool m_SetPosition;

            [ReadOnly]
            public BufferLookup<ConnectedEdge> m_Edges;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;

            [ReadOnly]
            public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

            [ReadOnly]
            public ComponentLookup<Edge> m_EdgeData;

            [ReadOnly]
            public ComponentLookup<Game.Net.Node> m_NodeData;

            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;

            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

            [ReadOnly]
            public ComponentLookup<Attached> m_AttachedData;

            [ReadOnly]
            public ComponentLookup<Attachment> m_AttachmentData;

            [ReadOnly]
            public ComponentLookup<ServiceUpgrade> m_ServiceUpgradeData;

            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_LotData;

            [ReadOnly]
            public ComponentLookup<Position> m_RoutePositionData;

            [ReadOnly]
            public ComponentLookup<Connected> m_RouteConnectedData;

            [ReadOnly]
            public ComponentLookup<Icon> m_IconData;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_AreaNodes;

            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;

            [ReadOnly]
            public BufferLookup<RouteWaypoint> m_RouteWaypoints;

            [ReadOnly]
            public BufferLookup<AggregateElement> m_AggregateElements;

            public EntityCommandBuffer m_CommandBuffer;

            [ReadOnly]
            public bool m_MoveSubBuildings;

            public void Execute()
            {
                for (int i = 0; i < m_Entities.Length; i++)
                {
                    m_Entity = m_Entities[i];
                    m_AllowHighlight = m_AllowHighlights[i];
                    AddSelectedEntity();
                }
            }

            private void AddSelectedEntity()
            {
                Entity entity = m_Entity;
                OwnerDefinition ownerDefinition = default(OwnerDefinition);
                bool isParent = false;
                bool attachParentCreated = false;
                if (m_ServiceUpgradeData.HasComponent(m_Entity) && m_OwnerData.TryGetComponent(m_Entity, out var owner) && m_TransformData.TryGetComponent(owner.m_Owner, out Transform transform))
                {
                    entity = owner.m_Owner;
                    isParent = true;
                    AddEntity(entity, Entity.Null, default(OwnerDefinition), isParent: true, attachParentCreated: false);
                    if (m_AttachmentData.TryGetComponent(entity, out var componentData3) && m_TransformData.HasComponent(componentData3.m_Attached))
                    {
                        AddEntity(componentData3.m_Attached, Entity.Null, default(OwnerDefinition), isParent: true, attachParentCreated: true);
                    }
                    ownerDefinition = new OwnerDefinition
                    {
                        m_Prefab = m_PrefabRefData[entity].m_Prefab,
                        m_Position = transform.m_Position,
                        m_Rotation = transform.m_Rotation
                    };
                }
                else if (m_AttachedData.TryGetComponent(m_Entity, out Attached componentData4) && m_AttachmentData.TryGetComponent(componentData4.m_Parent, out Attachment componentData5) && componentData5.m_Attached == m_Entity)
                {
                    entity = componentData4.m_Parent;
                    attachParentCreated = true;
                    AddEntity(entity, Entity.Null, default(OwnerDefinition), isParent: false, attachParentCreated: false);
                }
                AddEntity(m_Entity, Entity.Null, ownerDefinition, isParent: false, attachParentCreated);
                if (!m_InstalledUpgrades.TryGetBuffer(entity, out var installedUpgrades))
                {
                    return;
                }
                transform = m_TransformData[entity];
                ownerDefinition = new OwnerDefinition
                {
                    m_Prefab = m_PrefabRefData[entity].m_Prefab,
                    m_Position = transform.m_Position,
                    m_Rotation = transform.m_Rotation
                };
                for (int i = 0; i < installedUpgrades.Length; i++)
                {
                    Entity entity2 = installedUpgrades[i];
                    if (entity2 != m_Entity)
                    {
                        isParent = (isParent && m_Entity != entity) || (m_BuildingData.HasComponent(entity2) && !m_MoveSubBuildings);
                        AddEntity(entity2, Entity.Null, ownerDefinition, isParent, attachParentCreated: false);
                    }
                }
            }

            private void AddEntity(Entity entity, Entity owner, OwnerDefinition ownerDefinition, bool isParent, bool attachParentCreated)
            {
                // Another entity of the list gets its own pass (with its own flags), don't add it here as a parent / upgrade.
                if (entity != m_Entity && m_Entities.Contains(entity)) return;
                if (!m_Added.Add(entity)) return;

                Entity e = m_CommandBuffer.CreateEntity();
                CreationDefinition component = new CreationDefinition
                {
                    m_Original = entity
                };
                if (isParent)
                {
                    component.m_Flags |= CreationFlags.Parent | CreationFlags.Duplicate;
                }
                else if (m_AllowHighlight)
                {
                    // TempFlags.Parent (green highlight) marks the primary, replacing Select which GenerateObjectsSystem would ignore it with.
                    component.m_Flags |= entity == m_Primary ? CreationFlags.Parent : CreationFlags.Select;
                }
                m_CommandBuffer.AddComponent(e, default(Updated));
                if (ownerDefinition.m_Prefab != Entity.Null)
                {
                    m_CommandBuffer.AddComponent(e, ownerDefinition);
                }

                if (m_EdgeData.HasComponent(entity))
                {
                    if (m_EditorContainerData.HasComponent(entity))
                    {
                        component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
                    }
                    Edge edge = m_EdgeData[entity];
                    NetCourse component2 = default(NetCourse);
                    component2.m_Curve = m_CurveData[entity].m_Bezier;
                    component2.m_Length = MathUtils.Length(component2.m_Curve);
                    component2.m_FixedIndex = -1;
                    component2.m_StartPosition.m_Entity = edge.m_Start;
                    component2.m_StartPosition.m_Position = component2.m_Curve.a;
                    component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve));
                    component2.m_StartPosition.m_CourseDelta = 0f;
                    component2.m_EndPosition.m_Entity = edge.m_End;
                    component2.m_EndPosition.m_Position = component2.m_Curve.d;
                    component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve));
                    component2.m_EndPosition.m_CourseDelta = 1f;
                    m_CommandBuffer.AddComponent(e, component2);
                }
                else if (m_NodeData.HasComponent(entity))
                {
                    if (m_EditorContainerData.HasComponent(entity))
                    {
                        component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
                    }
                    Game.Net.Node node = m_NodeData[entity];
                    NetCourse component3 = new NetCourse
                    {
                        m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
                        m_Length = 0f,
                        m_FixedIndex = -1,
                        m_StartPosition =
                        {
                            m_Entity = entity,
                            m_Position = node.m_Position,
                            m_Rotation = node.m_Rotation,
                            m_CourseDelta = 0f
                        },
                        m_EndPosition =
                        {
                            m_Entity = entity,
                            m_Position = node.m_Position,
                            m_Rotation = node.m_Rotation,
                            m_CourseDelta = 1f
                        }
                    };
                    m_CommandBuffer.AddComponent(e, component3);
                }
                else if (m_TransformData.HasComponent(entity))
                {
                    Transform transform = m_TransformData[entity];
                    if (m_SetPosition)
                    {
                        transform.m_Position = m_Position;
                        component.m_Flags |= CreationFlags.Dragging;
                    }
                    ObjectDefinition component4 = new ObjectDefinition
                    {
                        m_Position = transform.m_Position,
                        m_Rotation = transform.m_Rotation
                    };
                    if (m_ElevationData.TryGetComponent(entity, out var componentData))
                    {
                        component4.m_Elevation = componentData.m_Elevation;
                        component4.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData.m_Flags);
                    }
                    else
                    {
                        component4.m_ParentMesh = -1;
                    }
                    Entity entity2 = entity;
                    if (m_AttachedData.HasComponent(entity))
                    {
                        component.m_Attached = m_AttachedData[entity].m_Parent;
                        component.m_Flags |= CreationFlags.Attach;
                        if (m_AttachmentData.TryGetComponent(component.m_Attached, out var componentData2) && componentData2.m_Attached == entity)
                        {
                            entity2 = component.m_Attached;
                        }
                        if (attachParentCreated && m_PrefabRefData.TryGetComponent(component.m_Attached, out var componentData3))
                        {
                            component.m_Attached = componentData3.m_Prefab;
                        }
                    }
                    component4.m_Probability = 100;
                    component4.m_PrefabSubIndex = -1;
                    if (m_LocalTransformCacheData.HasComponent(entity))
                    {
                        LocalTransformCache localTransformCache = m_LocalTransformCacheData[entity];
                        component4.m_LocalPosition = localTransformCache.m_Position;
                        component4.m_LocalRotation = localTransformCache.m_Rotation;
                        component4.m_ParentMesh = localTransformCache.m_ParentMesh;
                        component4.m_GroupIndex = localTransformCache.m_GroupIndex;
                        component4.m_Probability = localTransformCache.m_Probability;
                        component4.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
                    }
                    else if (ownerDefinition.m_Prefab != Entity.Null)
                    {
                        Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
                        component4.m_LocalPosition = transform2.m_Position;
                        component4.m_LocalRotation = transform2.m_Rotation;
                    }
                    else if (m_TransformData.HasComponent(owner))
                    {
                        Transform transform3 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
                        component4.m_LocalPosition = transform3.m_Position;
                        component4.m_LocalRotation = transform3.m_Rotation;
                    }
                    else
                    {
                        component4.m_LocalPosition = transform.m_Position;
                        component4.m_LocalRotation = transform.m_Rotation;
                    }
                    if (m_EditorContainerData.HasComponent(entity))
                    {
                        Game.Tools.EditorContainer editorContainer = m_EditorContainerData[entity];
                        component.m_SubPrefab = editorContainer.m_Prefab;
                        component4.m_Scale = editorContainer.m_Scale;
                        component4.m_Intensity = editorContainer.m_Intensity;
                        component4.m_GroupIndex = editorContainer.m_GroupIndex;
                    }
                    m_CommandBuffer.AddComponent(e, component4);
                    if (m_SubAreas.TryGetBuffer(entity2, out var bufferData))
                    {
                        OwnerDefinition ownerDefinition2 = new OwnerDefinition
                        {
                            m_Prefab = m_PrefabRefData[entity].m_Prefab,
                            m_Position = transform.m_Position,
                            m_Rotation = transform.m_Rotation
                        };
                        for (int i = 0; i < bufferData.Length; i++)
                        {
                            Entity area = bufferData[i].m_Area;
                            if(isParent && m_LotData.HasComponent(area))
                                AddEntity(area, Entity.Null, ownerDefinition2, isParent, attachParentCreated: false);
                            else if(!isParent)
                                AddEntity(area, Entity.Null, ownerDefinition2, isParent, attachParentCreated: false);
                        }
                    }
                }
                else if (m_AreaNodes.HasBuffer(entity))
                {
                    DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[entity];
                    DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
                    dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
                    dynamicBuffer2.CopyFrom(dynamicBuffer.AsNativeArray());
                }
                else if (m_RouteWaypoints.HasBuffer(entity))
                {
                    DynamicBuffer<RouteWaypoint> dynamicBuffer3 = m_RouteWaypoints[entity];
                    DynamicBuffer<WaypointDefinition> dynamicBuffer4 = m_CommandBuffer.AddBuffer<WaypointDefinition>(e);
                    dynamicBuffer4.ResizeUninitialized(dynamicBuffer3.Length);
                    for (int j = 0; j < dynamicBuffer3.Length; j++)
                    {
                        RouteWaypoint routeWaypoint = dynamicBuffer3[j];
                        WaypointDefinition value = new WaypointDefinition
                        {
                            m_Position = m_RoutePositionData[routeWaypoint.m_Waypoint].m_Position,
                            m_Original = routeWaypoint.m_Waypoint
                        };
                        if (m_RouteConnectedData.HasComponent(routeWaypoint.m_Waypoint))
                        {
                            value.m_Connection = m_RouteConnectedData[routeWaypoint.m_Waypoint].m_Connected;
                        }
                        dynamicBuffer4[j] = value;
                    }
                }
                else if (m_IconData.HasComponent(entity))
                {
                    Icon icon = m_IconData[entity];
                    m_CommandBuffer.AddComponent(e, new IconDefinition(icon));
                }
                else if (m_AggregateElements.HasBuffer(entity))
                {
                    DynamicBuffer<AggregateElement> dynamicBuffer5 = m_AggregateElements[entity];
                    DynamicBuffer<AggregateElement> dynamicBuffer6 = m_CommandBuffer.AddBuffer<AggregateElement>(e);
                    dynamicBuffer6.ResizeUninitialized(dynamicBuffer5.Length);
                    dynamicBuffer6.CopyFrom(dynamicBuffer5.AsNativeArray());
                }
                m_CommandBuffer.AddComponent(e, component);
            }
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct UpdateGizmosJob : IJob
        {
            [ReadOnly] public Mode m_Mode;
            [ReadOnly] public State m_State;
            [ReadOnly] public bool m_HasGizmo;
            [ReadOnly] public Handle m_SelectedHandle;
            [ReadOnly] public ComponentLookup<GizmosData> m_GizmosDataLookup;
            [ReadOnly] public ComponentLookup<Highlighted> m_HighlightedLookup;

            [ReadOnly] public bool m_UseLocalAxis;
            [ReadOnly] public XZHandleMode m_XZHandleMode;
            [ReadOnly] public float3 m_Position;
            [ReadOnly] public quaternion m_Rotation;
            // Dragging: the gizmo written by TransformToolSnapJob is used instead of m_Position / m_Rotation.
            [ReadOnly] public bool m_UseDragGizmo;
            [ReadOnly] public NativeReference<DragGizmoResult> m_DragGizmo;

            [ReadOnly] public EntityCommandBuffer m_CommandBuffer;

            [ReadOnly] public TerrainHeightData m_TerrainHeightData;

            [ReadOnly] public NativeList<Entity> m_Handles;

            [ReadOnly] public float3 m_CameraPosition;
            [ReadOnly] public float m_TanFOV;
            [ReadOnly] public float m_HandleScreenSize;

            private const float kReferenceScreenHeight = 1080f;
            // Tube radius of the rotate circles, relative to their radius: scales with the camera like the other handles.
            private const float kRotateArcThickness = 0.03f;

            private int m_ReuseIndex;

            public void Execute()
            {
                m_ReuseIndex = 0;
                if (m_Mode == Mode.Default || !m_HasGizmo)
                {
                    DestroyAllEntities();
                    return;
                }

                float3 pos = m_UseDragGizmo ? m_DragGizmo.Value.m_Position : m_Position;
                quaternion rot = m_UseDragGizmo ? m_DragGizmo.Value.m_Rotation : m_Rotation;

                float distance = math.distance(pos, m_CameraPosition);
                float targetFraction = m_HandleScreenSize / kReferenceScreenHeight;
                float handleSize = math.max(distance * m_TanFOV * 2f * targetFraction, 0.01f);
                float3 size = new(handleSize, handleSize, handleSize);

                switch (m_Mode)
                {
                    case Mode.Move:
                        UpdateMoveHandle(pos, rot, size);
                        break;
                    case Mode.Rotate:
                        UpdateRotateHandle(pos, rot, size);
                        break;
                    case Mode.Scale: 
                        break;
                    default:
                        break;
                }

                DestroyAllEntities();
            }

            private void UpdateMoveHandle(float3 pos, quaternion rot, float3 size)
            {
                float radius = math.cmax(size) * 0.1f;
                float3 xAxis = new float3(1, 0, 0);
                float3 yAxis = new float3(0, 1, 0);
                float3 zAxis = new float3(0, 0, 1);

                if (m_UseLocalAxis)
                {
                    xAxis = math.rotate(rot, xAxis);
                    yAxis = math.rotate(rot, yAxis);
                    zAxis = math.rotate(rot, zAxis);
                }

                xAxis *= size.x;
                yAxis *= size.y;
                zAxis *= size.z;

                UpdateMoveArrow(Handle.X, pos, pos + xAxis, Color.red);
                UpdateMoveArrow(Handle.Y, pos, pos + yAxis, Color.green);
                UpdateMoveArrow(Handle.Z, pos, pos + zAxis, Color.blue);

                switch(m_XZHandleMode)
                {
                    case XZHandleMode.FollowSurface:
                        UpdateMoveSphere(Handle.XZ, pos, radius, Color.cyan);
                        break;

                    case XZHandleMode.FixedX:
                        UpdateMoveSphere(Handle.XZ, pos, radius, Color.red);
                        break;

                    case XZHandleMode.FixedY:
                        UpdateMoveSphere(Handle.XZ, pos, radius, Color.green);
                        break;

                    case XZHandleMode.FixedZ:
                        UpdateMoveSphere(Handle.XZ, pos, radius, Color.blue);
                        break;
                }
            }

            private void UpdateMoveArrow(Handle handle, float3 A, float3 B, Color color)
            {
                Entity gizmos = GetOrCreateEntity();
                GizmosData data = GizmosUtils.DrawArrow(A, B, color, math.distance(A, B) * 0.2f);
                m_CommandBuffer.AddComponent(gizmos, data);

                AddHandleComponent(handle, gizmos);
                UpdateHighlight(handle, gizmos);
            }

            private void UpdateMoveSphere(Handle handle, float3 pos, float radius, Color color)
            {
                Entity gizmos = GetOrCreateEntity();
                GizmosData data = GizmosUtils.DrawWireSphere(pos, radius, color);
                m_CommandBuffer.AddComponent(gizmos, data);

                AddHandleComponent(handle, gizmos);
                UpdateHighlight(handle, gizmos);
            }

            private void UpdateRotateHandle(float3 pos, quaternion rot, float3 size)
            {
                float radius = math.cmax(size);

                float3 xAxis = new float3(1, 0, 0);
                float3 yAxis = new float3(0, 1, 0);
                float3 zAxis = new float3(0, 0, 1);

                if (m_UseLocalAxis)
                {
                    xAxis = math.rotate(rot, xAxis);
                    yAxis = math.rotate(rot, yAxis);
                    zAxis = math.rotate(rot, zAxis);
                }

                float3 fromX = GetPerpendicular(xAxis);
                float3 fromY = GetPerpendicular(yAxis);
                float3 fromZ = GetPerpendicular(zAxis);

                UpdateRotateArc(Handle.X, pos, xAxis, fromX, radius, Color.red);
                UpdateRotateArc(Handle.Y, pos, yAxis, fromY, radius, Color.green);
                UpdateRotateArc(Handle.Z, pos, zAxis, fromZ, radius, Color.blue);
            }

            private void UpdateRotateArc(Handle handle, float3 center, float3 normal, float3 from, float radius, Color color)
            {
                Entity gizmos = GetOrCreateEntity();
                GizmosData data = GizmosUtils.DrawWireArc(center, normal, from, 360, radius, color, thickness: radius * kRotateArcThickness, flags: GizmoFlags.CullBackHalf);
                m_CommandBuffer.AddComponent(gizmos, data);

                AddHandleComponent(handle, gizmos);
                UpdateHighlight(handle, gizmos);
            }

            private float3 GetPerpendicular(float3 n)
            {
                float3 refAxis = math.abs(n.y) < 0.99f
                    ? new float3(0, 1, 0)
                    : new float3(1, 0, 0);

                float3 perp = math.normalize(math.cross(n, refAxis));

                return perp;
            }


            private void UpdateScaleGizmos(float3 pos, quaternion rot, float3 size)
            {

            }

            private void AddHandleComponent(Handle handle, Entity entity)
            {
                m_CommandBuffer.AddComponent<TransformGizmosHandle>(entity, new TransformGizmosHandle(handle));
            }

            private void UpdateHighlight(Handle handle, Entity gizmos)
            {
                if (handle == m_SelectedHandle)
                {
                    if (!m_HighlightedLookup.HasComponent(gizmos)) m_CommandBuffer.AddComponent<Highlighted>(gizmos);
                }
                else if (m_HighlightedLookup.HasComponent(gizmos))
                {
                    m_CommandBuffer.RemoveComponent<Highlighted>(gizmos);
                }
            }

            private Entity GetOrCreateEntity()
            {
                Entity e;

                if (m_ReuseIndex < m_Handles.Length)
                {
                    e = m_Handles[m_ReuseIndex];
                    m_ReuseIndex++;
                }
                else
                {
                    e = m_CommandBuffer.CreateEntity();
                    m_ReuseIndex++;
                }

                return e;
            }


            private void DestroyAllEntities()
            {

                for (int i = m_ReuseIndex; i < m_Handles.Length; i++)
                {
                    Entity e = m_Handles[i];
                    m_CommandBuffer.DestroyEntity(e);
                }
            }
        }

        // Lookups of UpdateObjectJob, one set per scheduling system, created in OnCreate for its dependency.
        internal struct UpdateObjectLookups
        {
            public ComponentLookup<Transform> m_Transform;
            public ComponentLookup<Building> m_Building;
            public ComponentLookup<Curve> m_Curve;
            public ComponentLookup<Temp> m_Temp;
            public ComponentLookup<ServiceUpgrade> m_ServiceUpgrade;
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrade;
            public BufferLookup<SubArea> m_SubArea;
            public BufferLookup<Node> m_Node;
            public BufferLookup<SubObject> m_SubObject;
            public BufferLookup<SubNet> m_SubNet;

            public UpdateObjectLookups(ref SystemState state)
            {
                m_Transform = state.GetComponentLookup<Transform>(true);
                m_Building = state.GetComponentLookup<Building>(true);
                m_Curve = state.GetComponentLookup<Curve>(true);
                m_Temp = state.GetComponentLookup<Temp>(true);
                m_ServiceUpgrade = state.GetComponentLookup<ServiceUpgrade>(true);
                m_InstalledUpgrade = state.GetBufferLookup<InstalledUpgrade>(true);
                m_SubArea = state.GetBufferLookup<SubArea>(true);
                m_Node = state.GetBufferLookup<Node>(true);
                m_SubObject = state.GetBufferLookup<SubObject>(true);
                m_SubNet = state.GetBufferLookup<SubNet>(true);
            }

            public void Update(ref SystemState state)
            {
                m_Transform.Update(ref state);
                m_Building.Update(ref state);
                m_Curve.Update(ref state);
                m_Temp.Update(ref state);
                m_ServiceUpgrade.Update(ref state);
                m_InstalledUpgrade.Update(ref state);
                m_SubArea.Update(ref state);
                m_Node.Update(ref state);
                m_SubObject.Update(ref state);
                m_SubNet.Update(ref state);
            }
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct UpdateObjectJob : IJob
        {
            // One per entity. A default position / rotation keeps the current one.
            [ReadOnly] public NativeArray<Entity> m_Entities;
            [ReadOnly] public NativeArray<Transform> m_Transforms;
            [ReadOnly] public bool m_MoveSubBuildings;
            [ReadOnly] public ComponentLookup<Transform> m_TransformLookup;
            [ReadOnly] public ComponentLookup<Building> m_BuildingLookup;
            [ReadOnly] public ComponentLookup<Curve> m_CurveLookup;
            [ReadOnly] public ComponentLookup<Temp> m_TempLookup;
            [ReadOnly] public ComponentLookup<ServiceUpgrade> m_ServiceUpgradeLookup;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgradeLookup;
            [ReadOnly] public BufferLookup<SubArea> m_SubAreaLookup;
            [ReadOnly] public BufferLookup<Node> m_NodeLookup;
            [ReadOnly] public BufferLookup<SubObject> m_SubObjectLookup;
            [ReadOnly] public BufferLookup<SubNet> m_SubNetLookup;

            public EntityCommandBuffer m_CommandBuffer;

            private bool isTemp;

            public void Execute()
            {
                for (int i = 0; i < m_Entities.Length; i++)
                {
                    Entity entity = m_Entities[i];
                    isTemp = m_TempLookup.HasComponent(entity);

                    UpdateObject(entity, m_Transforms[i].m_Position, m_Transforms[i].m_Rotation);
                }
            }


            private void UpdateObject(Entity entity, float3 position, quaternion rotation)
            {
                if (!m_TransformLookup.TryGetComponent(entity, out Transform transform)) return;

                SetTempFlag(entity);

                position = !position.Equals(default) ? position : transform.m_Position;
                rotation = !rotation.Equals(default) ? rotation : transform.m_Rotation;

                float3 positionOffset = position - transform.m_Position;
                quaternion rotationOffset = math.mul(rotation, math.inverse(transform.m_Rotation));

                UpdateSubElement(entity, transform, positionOffset, rotationOffset);

                transform.m_Position = position;
                transform.m_Rotation = rotation;

                m_CommandBuffer.SetComponent(entity, transform);
                m_CommandBuffer.AddComponent(entity, new Updated());
            }

            private void UpdateSubElement(Entity entity, Transform parentTransform, float3 positionOffset, quaternion rotationOffset)
            {
                UpdateInstalledUpgrade(entity, parentTransform, positionOffset, rotationOffset);
                UpdateSubArea(entity, parentTransform, positionOffset, rotationOffset);
                UpdateSubObject(entity, parentTransform, positionOffset, rotationOffset);
                UpdateSubNet(entity, parentTransform, positionOffset, rotationOffset);
            }

            private void UpdateInstalledUpgrade(Entity entity, Transform parentTransform, float3 positionOffset, quaternion rotationOffset)
            {
                if (!m_InstalledUpgradeLookup.TryGetBuffer(entity, out DynamicBuffer<InstalledUpgrade> installedUpgrades)) return;

                foreach (InstalledUpgrade installedUpgrade in installedUpgrades)
                {

                    if (!m_TransformLookup.TryGetComponent(installedUpgrade, out Transform transform)) continue;

                    if(m_MoveSubBuildings || !m_BuildingLookup.HasComponent(installedUpgrade)) transform = ApplyTransformOffset(transform, parentTransform.m_Position, positionOffset, rotationOffset);

                    UpdateObject(installedUpgrade, transform.m_Position, transform.m_Rotation);
                }
            }

            private void UpdateSubArea(Entity entity, Transform parentTransform, float3 positionOffset, quaternion rotationOffset)
            {
                if (m_SubAreaLookup.TryGetBuffer(entity, out DynamicBuffer<SubArea> subAreas))
                {
                    foreach (SubArea subArea in subAreas)
                    {
                        SetTempFlag(subArea.m_Area);
                        if (m_NodeLookup.TryGetBuffer(subArea.m_Area, out DynamicBuffer<Node> nodes))
                        {
                            for (int i = 0; i < nodes.Length; i++)
                            {
                                float3 ogVector = nodes.ElementAt(i).m_Position - parentTransform.m_Position;
                                float3 offset = math.mul(rotationOffset, ogVector);
                                nodes.ElementAt(i).m_Position = parentTransform.m_Position + positionOffset + offset;
                            }
                        }
                        m_CommandBuffer.AddComponent(subArea.m_Area, new Updated());
                    }
                }
            }

            private void UpdateSubObject(Entity entity, Transform parentTransform, float3 positionOffset, quaternion rotationOffset)
            {
                if (m_SubObjectLookup.TryGetBuffer(entity, out DynamicBuffer<SubObject> subObjects))
                {
                    foreach (SubObject subObject in subObjects)
                    {
                        if (m_ServiceUpgradeLookup.HasComponent(subObject.m_SubObject)) continue;

                        SetTempFlag(subObject.m_SubObject);
                        if (m_TransformLookup.TryGetComponent(subObject.m_SubObject, out Transform transform))
                        {

                            transform = ApplyTransformOffset(transform, parentTransform.m_Position, positionOffset, rotationOffset);

                            m_CommandBuffer.SetComponent(subObject.m_SubObject, transform);
                            
                        }
                        m_CommandBuffer.AddComponent(subObject.m_SubObject, new Updated());
                    }
                }
            }

            private void UpdateSubNet(Entity entity, Transform parentTransform, float3 positionOffset, quaternion rotationOffset)
            {
                if (m_SubNetLookup.TryGetBuffer(entity, out DynamicBuffer<SubNet> subNets))
                {

                    foreach (SubNet subNet in subNets)
                    {
                        SetTempFlag(subNet.m_SubNet);
                        m_CommandBuffer.AddComponent(subNet.m_SubNet, new Updated());

                        if (!m_CurveLookup.TryGetComponent(subNet.m_SubNet, out Curve curve)) continue;
                        
                        float3 ogVector = curve.m_Bezier.a - parentTransform.m_Position;
                        float3 offset = math.mul(rotationOffset, ogVector);
                        curve.m_Bezier.a = parentTransform.m_Position + positionOffset + offset;

                        ogVector = curve.m_Bezier.b - parentTransform.m_Position;
                        offset = math.mul(rotationOffset, ogVector);
                        curve.m_Bezier.b = parentTransform.m_Position + positionOffset + offset;

                        ogVector = curve.m_Bezier.c - parentTransform.m_Position;
                        offset = math.mul(rotationOffset, ogVector);
                        curve.m_Bezier.c = parentTransform.m_Position + positionOffset + offset;

                        ogVector = curve.m_Bezier.d - parentTransform.m_Position;
                        offset = math.mul(rotationOffset, ogVector);
                        curve.m_Bezier.d = parentTransform.m_Position + positionOffset + offset;

                        m_CommandBuffer.SetComponent(subNet.m_SubNet, curve);
                    }
                }
            }

            private Transform ApplyTransformOffset(Transform transform, float3 parentPos, float3 posOffset, quaternion rotOffset)
            {
                float3 ogVector = transform.m_Position - parentPos;
                float3 offset = math.mul(rotOffset, ogVector);

                transform.m_Position = parentPos + posOffset + offset;
                transform.m_Rotation = math.mul(rotOffset, transform.m_Rotation);
                return transform;
            }

            private void SetTempFlag(Entity entity)
            {
                if (!isTemp) return;

                if(m_TempLookup.TryGetComponent(entity, out Temp temp))
                {
                    temp.m_Flags |= TempFlags.Dragging;
                    m_CommandBuffer.SetComponent(entity, temp);
                }
            }
        }

        private enum ActionType
        {
            Select,
            Transform,
            Create,
        }

        private struct ActionHistory
        {
            public ActionType ActionType;
            public Entity Entity;
            public Entity SelectedEntity;
            public float3 Position;
            public quaternion Rotation;
            //public int m_SelectedIndex; // For later if I add Net support.

            // Select: Entity / SelectedEntity are the new / previous primary entity, these are the full selections.
            public Entity[] Selection;
            public Entity[] PreviousSelection;

            // Transform: the whole group, one transform per entity.
            public Entity[] Entities;
            public Transform[] Transforms;

            public ActionHistory(ActionType actionType, Entity entity, Entity selectedEntity, float3 position, quaternion rotation)
            {
                ActionType = actionType;
                Entity = entity;
                SelectedEntity = selectedEntity;
                Position = position;
                Rotation = rotation;
                Selection = null;
                PreviousSelection = null;
                Entities = null;
                Transforms = null;
            }

            public static ActionHistory NewSelect(Entity newPrimary, Entity[] newSelection, Entity previousPrimary, Entity[] previousSelection)
            {
                return new ActionHistory(ActionType.Select, newPrimary, previousPrimary, default, default)
                {
                    Selection = newSelection,
                    PreviousSelection = previousSelection,
                };
            }

            public static ActionHistory NewTransform(Entity[] entities, Transform[] transforms)
            {
                return new ActionHistory(ActionType.Transform, Entity.Null, Entity.Null, default, default)
                {
                    Entities = entities,
                    Transforms = transforms,
                };
            }

            // Create: Entities / Entity are the duplicates, PreviousSelection / SelectedEntity their sources.
            public static ActionHistory NewCreate(Entity[] created, Entity createdPrimary, Entity[] sources, Entity sourcePrimary)
            {
                return new ActionHistory(ActionType.Create, createdPrimary, sourcePrimary, default, default)
                {
                    Entities = created,
                    PreviousSelection = sources,
                };
            }
        }

        // One selected entity being dragged. Pivot and start transform are frozen at StartDragging.
        private struct DragEntry
        {
            public Entity Original;
            public Entity Temp;
            public Transform StartTransform;
            public float3 Pivot;
        }

        public enum Handle
        {
            None, X, Y, Z, XZ,
        }

        public enum AllowHightlightState
        {
            Default,
            Enabled,
            Disabled,
        }

        public enum Mode
        {
            Default = 0,
            Move,
            Rotate,
            Scale
        }

        public enum XZHandleMode
        {
            FollowSurface = 0,
            FixedX,
            FixedY,
            FixedZ,
        }

        public enum State
        {
            Idle,
            Dragging
        }

        [Flags]
        public enum RaycastFilter : uint
        {
            None = 0,
            StaticObject = 1,
            Decals = 2,
            Buildings = 4,
            MovingObject = 8,
            Water = 16,
            Net = 32,
            All = uint.MaxValue,
        }

        public override string toolID => "TransformGizmoTool";

        private AudioManager m_AudioManager;
        private TerrainSystem m_TerrainSystem;
        private WaterSystem m_WaterSystem;
        private Game.Objects.SearchSystem m_ObjectSearchSystem;
        private GizmosRaycastSystem m_GimzosRaycastSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private SelectedInfoUISystem m_SelectedInfoUISystem;
        private DuplicateEntitySystem m_DuplicateEntitySystem;

        private Mode m_Mode = Mode.Default;
        private XZHandleMode m_XZHandleMode = XZHandleMode.FollowSurface;

        private State m_State = State.Idle;

        private RaycastFilter m_RaycastFilter = RaycastFilter.None;

        private EntityQuery m_SoundQuery;
        private EntityQuery m_DefinitionQuery;
        private EntityQuery m_TempQuery;
        private EntityQuery m_HandleQuery;

        private Entity m_LastRaycastEntity;
        private Entity m_SelectedEntity; // Primary entity of the selection (last one added).
        private Entity m_SelectedTempEntity; // Temp of the primary entity while dragging.

        // Multi Selection
        private readonly List<Entity> m_SelectedEntities = new();
        private bool m_DefinitionsDirty = false;
        private bool m_AdditivePress = false;

        // Pivot, see TransformGizmoToolPivot.cs
        private PivotSettings m_PivotSettings = new()
        {
            Source = PivotSource.Origin,
            Aggregation = PivotAggregation.Median,
            Orientation = PivotOrientation.LocalPrimary,
        };

        private Handle m_SelectedHandle;

        private int m_LastSelectedIndex;
        private int m_SelectedIndex;
        private float3 m_DragStartGizmoPos;
        private float3 m_DragStartSurfacePos;
        private quaternion m_DragStartGizmoRot;
        private float3 m_DragStartMouseHitPos;

        // Drag
        private readonly List<DragEntry> m_DragEntries = new();
        private int m_DragPrimaryIndex;
        private quaternion m_DragStartPrimaryRot;
        // Gizmo of the drag: written by TransformToolSnapJob, read by UpdateGizmosJob, never on the main thread.
        private NativeReference<DragGizmoResult> m_DragGizmo;
        private JobHandle m_DragGizmoDeps;

        private UpdateObjectLookups m_UpdateObjectLookups;

        // Follow Surface: keep the object upright instead of tilting it to the surface.
        internal bool m_Upright = false;

        // Grid
        internal bool m_GridEnabled = false;
        internal double m_PosOffset = 1;
        internal double m_RotOffset = 45;

        // Bindings
        private ProxyAction m_UndoAction;
        private ProxyAction m_RedoAction;
        private ProxyAction m_MoveAction;
        private ProxyAction m_RotateAction;
        private ProxyAction m_DuplicateAction;
        private ProxyAction m_AddToSelectionAction;
        private ProxyAction m_SetPrimaryAction;

        // Undo / Redo
        private Stack<ActionHistory> m_UndoHistory;
        private Stack<ActionHistory> m_RedoHistory;

        // Quick Actions
        private bool m_RequestSnapOnGround = false;
        private bool m_RequestUndo = false;
        private bool m_RequestRedo = false;
        private bool m_RequestDuplicate = false;
        private bool m_WaitingForDuplicate = false;
        private Entity[] m_PendingDuplicateSources = null;
        private Entity m_PendingDuplicatePrimary = Entity.Null;

        // SIP Support
        private bool m_WasAnEntitySelected = false;

        // Anarchy Support
        internal bool m_AddPreventOverride = true;
        internal bool m_AddTransformLock = true;

        // Kept for the UI / TransformSection, it's now the pivot orientation.
        public bool m_UseLocalAxis
        {
            get => m_PivotSettings.Orientation != PivotOrientation.Global;
            internal set => m_PivotSettings.Orientation = value ? PivotOrientation.LocalPrimary : PivotOrientation.Global;
        }
        public bool m_MoveSubBuildings { get; internal set; } = true;
        public bool m_Underground { get; private set; } = false;
        public AllowHightlightState m_AllowHightlight { get; set; } = AllowHightlightState.Default;

        public override int uiModeIndex => (int)m_Mode;
        public override bool allowUnderground { get => true; protected set { } }

        public Mode mode => m_Mode;

        public XZHandleMode xzHandleMode { get { return m_XZHandleMode; } internal set { m_XZHandleMode = value; } }
        public RaycastFilter raycastFilter { get { return m_RaycastFilter; } internal set { m_RaycastFilter = value; } }
        public PivotAggregation pivotAggregation { get { return m_PivotSettings.Aggregation; } internal set { m_PivotSettings.Aggregation = value; } }
        public PivotSource pivotSource { get { return m_PivotSettings.Source; } internal set { m_PivotSettings.Source = value; } }

        public Entity SelectedTempEntity => m_SelectedTempEntity;

        public Entity SelectedEntity => m_SelectedEntity;

        public IReadOnlyList<Entity> SelectedEntities => m_SelectedEntities;

        public int SelectionCount => m_SelectedEntities.Count;

        // Events
        public event Action<Mode> ModeChanged;
        public event Action<Entity> SelectedEntityChanged;
        public event Action<IReadOnlyList<Entity>> SelectionChanged;

        public override PrefabBase GetPrefab() { return null; }

        public override bool TrySetPrefab(PrefabBase prefab) { return false; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;

            if(AnarchyBridge.Initialize(true))
            {
                if(AnarchyBridge.TryAddToolSystem(this))
                    EDT.Logger.Info($"Registered {toolID} to Anarchy.");
                else
                    EDT.Logger.Warn($"Anarchy available but failed to register {toolID} to Anarchy.");
            }
            else
            {
                EDT.Logger.Warn($"Anarchy not available. {toolID} will not be registered to Anarchy and may not work properly.");
            }

            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_ObjectSearchSystem = World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_AudioManager = World.GetOrCreateSystemManaged<AudioManager>();
            m_GimzosRaycastSystem = World.GetOrCreateSystemManaged<GizmosRaycastSystem>();
            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            m_DuplicateEntitySystem = World.GetOrCreateSystemManaged<DuplicateEntitySystem>();

            m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
            m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
            m_HandleQuery = GetEntityQuery(ComponentType.ReadOnly<TransformGizmosHandle>());
            m_DefinitionQuery = GetDefinitionQuery();

            m_DragGizmo = new NativeReference<DragGizmoResult>(Allocator.Persistent);
            m_UpdateObjectLookups = new UpdateObjectLookups(ref CheckedStateRef);

            m_UndoHistory = new();
            m_RedoHistory = new();

            m_UndoAction = EDT.m_Settings.GetAction(EDT.m_Settings.UndoBinding.actionName);
            m_RedoAction = EDT.m_Settings.GetAction(EDT.m_Settings.RedoBinding.actionName);
            m_MoveAction = EDT.m_Settings.GetAction(EDT.m_Settings.EnterMoveBinding.actionName);
            m_RotateAction = EDT.m_Settings.GetAction(EDT.m_Settings.EnterRotateBinding.actionName);
            m_DuplicateAction = EDT.m_Settings.GetAction(EDT.m_Settings.DuplicateBinding.actionName);
            m_AddToSelectionAction = EDT.m_Settings.GetAction(EDT.m_Settings.AddToSelectionBinding.actionName);
            m_SetPrimaryAction = EDT.m_Settings.GetAction(EDT.m_Settings.SetPrimaryBinding.actionName);
        }

        protected override void OnDestroy()
        {
            m_DragGizmoDeps.Complete();
            m_DragGizmo.Dispose();
            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

#if DEBUG
            if (!AnarchyBridge.IsAvailable)
            {
                if (AnarchyBridge.Initialize(true))
                {
                    if (AnarchyBridge.TryAddToolSystem(this))
                        EDT.Logger.Info($"Registered {toolID} to Anarchy.");
                    else
                        EDT.Logger.Warn($"Anarchy available but failed to register {toolID} to Anarchy.");
                }
                else
                {
                    EDT.Logger.Warn($"Anarchy not available. {toolID} will not be registered to Anarchy and may not work properly.");
                }
            }
#endif

            m_UndoAction.shouldBeEnabled = true;
            m_RedoAction.shouldBeEnabled = true;
            m_MoveAction.shouldBeEnabled = true;
            m_RotateAction.shouldBeEnabled = true;
            m_DuplicateAction.shouldBeEnabled = true;
            m_AddToSelectionAction.shouldBeEnabled = true;
            m_SetPrimaryAction.shouldBeEnabled = true;
            SetSelectedEntity(m_ToolSystem.selected);
            m_WasAnEntitySelected = m_SelectedEntity != Entity.Null;
            m_ToolSystem.selected = Entity.Null;
            SetMode(Mode.Default);
            EnableActions(true);
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_UndoAction.shouldBeEnabled = false;
            m_RedoAction.shouldBeEnabled = false;
            m_MoveAction.shouldBeEnabled = false;
            m_RotateAction.shouldBeEnabled = false;
            m_DuplicateAction.shouldBeEnabled = false;
            m_AddToSelectionAction.shouldBeEnabled = false;
            m_SetPrimaryAction.shouldBeEnabled = false;
            var es = m_HandleQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in es)
            {
                EntityManager.DestroyEntity(e);
            }
            es.Dispose();
            if (m_WasAnEntitySelected)
            {
                m_ToolSystem.selected = m_SelectedEntity;
            }
            m_UndoHistory.Clear();
            m_RedoHistory.Clear();
            SetMode(Mode.Default);
            EnableActions(false);
        }

        public override void SetUnderground(bool underground)
        {
            m_Underground = underground;
            requireUnderground = underground;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            if(m_Mode == Mode.Default)
            {
                if (m_Underground)
                {
                    m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
                }
                else
                {
                    m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                }

                if(!m_RaycastFilter.HasFlag(RaycastFilter.StaticObject))
                {
                    m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.Decals))
                        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Decals;

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.Buildings))
                        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots | RaycastFlags.SubBuildings;
                }

                if(!m_RaycastFilter.HasFlag(RaycastFilter.MovingObject))
                    m_ToolRaycastSystem.typeMask |= TypeMask.MovingObjects;

                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_ToolRaycastSystem.raycastFlags &= ~RaycastFlags.SubBuildings;
                    m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.Markers | RaycastFlags.UpgradeIsMain | RaycastFlags.EditorContainers;
                }
            } else
            {
                GizmosRaycastInput input = new GizmosRaycastInput()
                {
                    m_Type = GizmosRaycastType.ALL,
                    m_Line = ToolRaycastSystem.CalculateRaycastLine(Camera.main),
                    m_Tolerance = 0, // The handles have their own volume, scaled with the camera.
                    m_Debug = false
                };
                m_GimzosRaycastSystem.AddInput(this, input);

                // Objects under the cursor, for Shift+Click / Alt+Click.
                if (m_State == State.Idle)
                {
                    m_ToolRaycastSystem.collisionMask = m_Underground ? CollisionMask.Underground : CollisionMask.OnGround | CollisionMask.Overground;

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.StaticObject))
                    {
                        m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;

                        if (!m_RaycastFilter.HasFlag(RaycastFilter.Decals))
                            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Decals;

                        if (!m_RaycastFilter.HasFlag(RaycastFilter.Buildings))
                            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots | RaycastFlags.SubBuildings;
                    }

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.MovingObject))
                        m_ToolRaycastSystem.typeMask |= TypeMask.MovingObjects;
                }

                if (m_XZHandleMode == XZHandleMode.FollowSurface)
                {
                    if (m_Underground)
                    {
                        m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
                    }
                    else
                    {
                        m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                        m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
                        // Any object can snap on water, false water hits on dry slopes are fixed in TransformToolSnapJob.FixWaterHit.
                        if(!m_RaycastFilter.HasFlag(RaycastFilter.Water))
                        {
                            m_ToolRaycastSystem.typeMask |= TypeMask.Water;
                        }
                    }

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.StaticObject))
                    {
                        m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    }

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.MovingObject))
                        m_ToolRaycastSystem.typeMask |= TypeMask.MovingObjects;

                    if (!m_RaycastFilter.HasFlag(RaycastFilter.Net))
                    {
                        m_ToolRaycastSystem.typeMask |= TypeMask.Net | TypeMask.Lanes;
                        m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.Fence | Layer.TrainTrack | Layer.SubwayTrack | Layer.TrainTrack;
                    }

                    if (m_ToolSystem.actionMode.IsEditor())
                    {
                        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }
                }
            }
        }

        private void PlaySelectedSound(Entity selected, bool forcePlay = false)
        {
            Entity clipEntity = (
                (
                    base.EntityManager.TryGetComponent(selected, out Game.Creatures.Resident component)
                    && base.EntityManager.TryGetComponent<Citizen>(component.m_Citizen, out Citizen component2)
                    && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Citizen, out PrefabRef component3)
                ) ? CitizenUtils.GetCitizenSelectedSound(base.EntityManager, component.m_Citizen, component2, component3.m_Prefab)
                : ((
                    !base.EntityManager.TryGetComponent<PrefabRef>(selected, out PrefabRef component4)
                    || !base.EntityManager.TryGetComponent<SelectedSoundData>(component4.m_Prefab, out SelectedSoundData component5)
                ) ? m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_SelectEntitySound : component5.m_selectedSound)
            );
            if (forcePlay)
            {
                m_AudioManager.PlayUISound(clipEntity);
            }
            else
            {
                m_AudioManager.PlayUISoundIfNotPlaying(clipEntity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {

            if(m_FocusChanged)
            {
                return inputDeps;
            }

            if (m_State == State.Idle)
            {
                ValidateSelection();
            }

            if (m_MoveAction.WasPressedThisFrame())
            {
                if (m_Mode != Mode.Move)
                    SetMode(Mode.Move);
                else
                    SetMode(Mode.Default);
            }

            if (m_RotateAction.WasPressedThisFrame())
            {
                if (m_Mode != Mode.Rotate)
                    SetMode(Mode.Rotate);
                else
                    SetMode(Mode.Default);
            }

            if (m_RequestSnapOnGround)
            {
                m_RequestSnapOnGround = false;
                if (m_State == State.Idle && m_SelectedEntities.Count > 0)
                {
                    List<Entity> entities = new();
                    List<Transform> transforms = new();
                    int primaryIndex = CollectRootTransforms(false, entities, transforms);

                    if (entities.Count > 0)
                    {
                        m_UndoHistory.Push(ActionHistory.NewTransform(entities.ToArray(), transforms.ToArray()));

                        // Individual: every entity is put on the ground, otherwise the pivot is and the others keep their offset.
                        TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
                        ComputeGroupPivots(null, transforms, primaryIndex, out float3 groupPivot, out _, null);
                        float groupDelta = TerrainUtils.SampleHeight(ref heightData, groupPivot) - groupPivot.y;
                        bool individual = m_PivotSettings.Aggregation == PivotAggregation.Individual;
                        for (int i = 0; i < entities.Count; i++)
                        {
                            float3 snapPos = transforms[i].m_Position;
                            snapPos.y = individual ? TerrainUtils.SampleHeight(ref heightData, snapPos) : snapPos.y + groupDelta;
                            inputDeps = UpdateObject(inputDeps, entities[i], snapPos);
                        }
                    }
                }
            }

            if (m_WaitingForDuplicate)
            {
                if (m_DuplicateEntitySystem.TryGetDuplicated(this, out List<Entity> sources, out List<Entity> duplicated))
                {
                    m_WaitingForDuplicate = false;

                    List<Entity> created = new();
                    Entity createdPrimary = Entity.Null;
                    for (int i = 0; i < duplicated.Count; i++)
                    {
                        if (duplicated[i] == Entity.Null) continue;
                        created.Add(duplicated[i]);
                        if (sources[i] == m_PendingDuplicatePrimary) createdPrimary = duplicated[i];
                    }

                    if (created.Count > 0)
                    {
                        if (createdPrimary == Entity.Null) createdPrimary = created[created.Count - 1];

                        m_UndoHistory.Push(ActionHistory.NewCreate(created.ToArray(), createdPrimary, m_PendingDuplicateSources, m_PendingDuplicatePrimary));

                        SetSelection(created, createdPrimary);
                        m_SelectedIndex = -1;
                        SetMode(Mode.Move);
                    }

                    m_PendingDuplicateSources = null;
                    m_PendingDuplicatePrimary = Entity.Null;
                }
            }

            if (m_RequestDuplicate || m_DuplicateAction.WasPressedThisFrame())
            {
                m_RequestDuplicate = false;

                // Roots only: DuplicateEntityJob already copies the sub-objects / upgrades of each root.
                List<Entity> sources = new();
                if (m_State == State.Idle && !m_WaitingForDuplicate && CanDuplicateSelection())
                {
                    foreach (Entity entity in GetSelectionRoots())
                    {
                        if (EntityManager.HasComponent<PrefabRef>(entity) && EntityManager.HasComponent<Transform>(entity)) sources.Add(entity);
                    }
                }

                if (sources.Count > 0)
                {
                    EDT.Logger.Info($"Duplicate requested for {sources.Count} entities, primary: {m_SelectedEntity}");

                    NativeArray<Entity> sourceArray = new NativeArray<Entity>(sources.ToArray(), Allocator.TempJob);
                    inputDeps = m_DuplicateEntitySystem.Duplicate(inputDeps, this, sourceArray);
                    inputDeps = sourceArray.Dispose(inputDeps);

                    // The primary may be a sub-object of a root: its root's duplicate becomes the new primary.
                    m_PendingDuplicatePrimary = sources.Contains(m_SelectedEntity) ? m_SelectedEntity : sources[sources.Count - 1];
                    m_PendingDuplicateSources = sources.ToArray();
                    SetSelectedEntity(Entity.Null);
                    m_SelectedTempEntity = Entity.Null;
                    m_SelectedIndex = -1;
                    m_WaitingForDuplicate = true;
                    SetMode(Mode.Default);
                }
                else
                {
                    EDT.Logger.Warn("Duplicate: No entity selected or selected entity has no PrefabRef or in invalid state.");
                }
            }

            if (m_RequestUndo || m_UndoAction.WasPressedThisFrame())
            {
                m_RequestUndo = false;
                if (m_State == State.Idle && m_UndoHistory.Count > 0)
                {
                    ActionHistory actionHistory = m_UndoHistory.Pop();
                    while (m_UndoHistory.Count > 0 && !IsHistoryEntryUsable(actionHistory, forRedo: false))
                    {
                        actionHistory = m_UndoHistory.Pop();
                    }

                    if (IsHistoryEntryUsable(actionHistory, forRedo: false))
                    {
                        Entity entity = actionHistory.Entity;
                        ActionType actionType = actionHistory.ActionType;
                        if (actionType == ActionType.Select)
                        {
                            m_RedoHistory.Push(actionHistory);
                            SetSelection(actionHistory.PreviousSelection, actionHistory.SelectedEntity);
                            m_SelectedIndex = -1; // Note: if in future we support Net, fix this.

                            if (m_SelectedEntity == Entity.Null) SetMode(Mode.Default); // Keeps the current mode otherwise.
                            m_SelectedInfoUISystem.Focus(m_SelectedEntity);
                        }
                        else if (actionType == ActionType.Transform)
                        {
                            m_RedoHistory.Push(CaptureTransforms(actionHistory.Entities));
                            inputDeps = RestoreTransforms(inputDeps, actionHistory);
                        }
                        else if (actionType == ActionType.Create)
                        {
                            m_RedoHistory.Push(actionHistory);

                            bool selectionHadCreated = false;
                            foreach (Entity created in actionHistory.Entities)
                            {
                                if (m_SelectedEntities.Contains(created)) selectionHadCreated = true;
                                if (EntityManager.Exists(created) && !EntityManager.HasComponent<Deleted>(created)) EntityManager.AddComponent<Deleted>(created);
                            }

                            if (selectionHadCreated)
                            {
                                SetSelectionFiltered(actionHistory.PreviousSelection, actionHistory.SelectedEntity);
                                m_SelectedIndex = -1; // Note: if in future we support Net, fix this.

                                if (m_SelectedEntity == Entity.Null) SetMode(Mode.Default); // Keeps the current mode otherwise.
                                m_SelectedInfoUISystem.Focus(m_SelectedEntity);
                            }
                        }
                        else
                        {
                            EDT.Logger.Warn($"Undo: unhandled action type {actionType}");
                        }
                    }
                }
            }
            else if (m_RequestRedo || m_RedoAction.WasPressedThisFrame())
            {
                m_RequestRedo = false;
                if (m_State == State.Idle && m_RedoHistory.Count > 0)
                {
                    ActionHistory actionHistory = m_RedoHistory.Pop();
                    while (m_RedoHistory.Count > 0 && !IsHistoryEntryUsable(actionHistory, forRedo: true))
                    {
                        actionHistory = m_RedoHistory.Pop();
                    }

                    if (IsHistoryEntryUsable(actionHistory, forRedo: true))
                    {
                        Entity entity = actionHistory.Entity;
                        ActionType actionType = actionHistory.ActionType;
                        if (actionType == ActionType.Create)
                        {
                            // Duplicates the sources again: the undo deleted the previous duplicates for good.
                            SetSelectionFiltered(actionHistory.PreviousSelection, actionHistory.SelectedEntity);
                            m_SelectedIndex = -1;
                            m_RequestDuplicate = true;
                        }
                        else if (actionType == ActionType.Select)
                        {
                            m_UndoHistory.Push(actionHistory);
                            SetSelection(actionHistory.Selection, actionHistory.Entity);
                            m_SelectedIndex = -1; // Note: if in future we support Net, fix this.

                            if (m_SelectedEntity == Entity.Null) SetMode(Mode.Default); // Keeps the current mode otherwise.
                            m_SelectedInfoUISystem.Focus(m_SelectedEntity);
                        }
                        else if (actionType == ActionType.Transform)
                        {
                            m_UndoHistory.Push(CaptureTransforms(actionHistory.Entities));
                            inputDeps = RestoreTransforms(inputDeps, actionHistory);
                        }
                        else
                        {
                            EDT.Logger.Warn($"Redo: unhandled action type {actionType}");
                        }
                    }
                }
            }

            if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable)) == 0)// | RaycastFlags.UIDisable)
            {
                switch (m_Mode)
                {
                    case Mode.Default:
                        {
                            // Checked first: Shift+Click / Alt+Click also trigger the apply action.
                            if (m_AddToSelectionAction.WasPressedThisFrame())
                            {
                                m_AdditivePress = true;
                                return Apply(inputDeps, additive: true);
                            }
                            else if (m_SetPrimaryAction.WasPressedThisFrame())
                            {
                                m_AdditivePress = true;
                                return Apply(inputDeps, setPrimary: true);
                            }
                            else if(applyAction.WasPressedThisFrame())
                            {
                                m_AdditivePress = false;
                                return Apply(inputDeps, base.applyAction.WasReleasedThisFrame(), base.cancelAction.WasPressedThisFrame());
                            } else if(applyAction.WasReleasedThisFrame())
                            {
                                // The release of a Shift+Click / Alt+Click must not replace the selection.
                                if (m_AdditivePress)
                                {
                                    m_AdditivePress = false;
                                    return Update(inputDeps);
                                }
                                return Apply(inputDeps);
                            }
                            else if(cancelAction.WasReleasedThisFrame())
                            {
                                return Cancel(inputDeps);
                            }
                            return Update(inputDeps);
                        }
                    case Mode.Move:
                    case Mode.Rotate:
                        {
                            if (cancelAction.WasReleasedThisFrame())
                            {
                                return Cancel(inputDeps);
                            }
                            else if (m_State == State.Idle && (m_AddToSelectionAction.WasPressedThisFrame() || m_SetPrimaryAction.WasPressedThisFrame()))
                            {
                                // Shift+Click / Alt+Click on an object, checked before the gizmo (they also trigger the apply action).
                                if (GetRaycastResult(out Entity hovered, out RaycastHit _))
                                {
                                    if (EntityManager.TryGetComponent(hovered, out Temp temp) && temp.m_Original != Entity.Null) hovered = temp.m_Original;
                                    if (m_SetPrimaryAction.WasPressedThisFrame()) SetPrimaryInSelection(hovered);
                                    else ToggleInSelection(hovered);
                                    PlaySelectedSound(hovered, forcePlay: true);
                                }
                                return Update(inputDeps);
                            }
                            else if ((m_State == State.Idle && base.applyAction.WasPressedThisFrame()) || (m_State == State.Dragging && base.applyAction.WasReleasedThisFrame()))
                            {
                                return Apply(inputDeps);
                            }
                            return Update(inputDeps);
                        }
                }
            }

            m_State = State.Idle;
            SetMode(Mode.Default);
            return inputDeps;
        }

        private JobHandle Update(JobHandle inputDeps)
        {
            if (m_Mode == Mode.Default)
            {
                if (GetRaycastResult(out var entity2, out var hit2, out var forceUpdate)
                    && entity2 == m_LastRaycastEntity
                    && !forceUpdate
                    && !m_DefinitionsDirty)
                {
                    applyMode = ApplyMode.None;
                }
                else
                {
                    m_LastRaycastEntity = entity2;

                    applyMode = ApplyMode.Clear;
                    inputDeps = UpdateDefinitions(inputDeps, entity2, hit2.m_CellIndex.x);
                }
            }
            else
            {
                if (m_State == State.Idle)
                {
                    NativeArray<RaycastResult> raycastResults = m_GimzosRaycastSystem.GetResult(this);

                    if (raycastResults.Length > 0)
                    {
                        RaycastResult raycastResult = raycastResults[0];
                        Entity e = raycastResult.m_Hit.m_HitEntity;
                        if (EntityManager.TryGetComponent<TransformGizmosHandle>(e, out var c))
                        {
                            m_SelectedHandle = c.Handle;
                        }
                        else
                        {
                            m_SelectedHandle = Handle.None;
                        }
                    }

                    // Object under the cursor (Shift/Alt+Click target), not over a handle: a drag must not carry this temp.
                    Entity hovered = Entity.Null;
                    if (m_SelectedHandle == Handle.None && GetRaycastResult(out Entity hit, out RaycastHit _))
                    {
                        if (EntityManager.TryGetComponent(hit, out Temp temp) && temp.m_Original != Entity.Null) hit = temp.m_Original;
                        if (EntityManager.HasComponent<Transform>(hit)) hovered = hit;
                    }

                    applyMode = ApplyMode.Clear;
                    inputDeps = UpdateDefinitions(inputDeps, hovered, m_SelectedIndex);
                }
                else if (m_State == State.Dragging)
                {
                    applyMode = ApplyMode.None;

                    if (m_DragEntries.Count == 0)
                    {
                        EDT.Logger.Error("Dragging without any drag entry.");
                    }
                    else
                    {
                        inputDeps = UpdateDrag(inputDeps);
                    }
                }
            }

            inputDeps = UpdateGizmos(inputDeps);
            return inputDeps;
        }

        private bool IsPrimaryPlacedOnWater()
        {
            if (m_DragEntries.Count == 0) return false;
            Entity primary = m_DragEntries[m_DragPrimaryIndex].Original;
            return EntityManager.TryGetComponent(primary, out PrefabRef prefabRef)
                && EntityManager.TryGetComponent(prefabRef.m_Prefab, out PlaceableObjectData placeableObjectData)
                && (placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != 0;
        }

        // The drag runs in TransformToolSnapJob, the main thread gives the ray / hit and applies the results.
        private JobHandle UpdateDrag(JobHandle inputDeps)
        {
            bool followSurface = m_XZHandleMode == XZHandleMode.FollowSurface && m_SelectedHandle == Handle.XZ;
            ControlPoint raw = default;
            bool hasHit = false;
            if (followSurface && GetRaycastResult(out Entity entity, out RaycastHit hit))
            {
                hasHit = true;
                raw = new ControlPoint(entity, hit)
                {
                    m_Position = hit.m_HitPosition,
                    m_Rotation = m_DragStartPrimaryRot,
                };
            }

            Camera camera = Camera.main;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            int count = m_DragEntries.Count;
            NativeArray<Entity> originals = new NativeArray<Entity>(count, Allocator.TempJob);
            NativeArray<Transform> startTransforms = new NativeArray<Transform>(count, Allocator.TempJob);
            NativeArray<float3> pivots = new NativeArray<float3>(count, Allocator.TempJob);
            NativeArray<Transform> results = new NativeArray<Transform>(count, Allocator.TempJob);
            NativeArray<Entity> temps = new NativeArray<Entity>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                originals[i] = m_DragEntries[i].Original;
                startTransforms[i] = m_DragEntries[i].StartTransform;
                pivots[i] = m_DragEntries[i].Pivot;
                temps[i] = m_DragEntries[i].Temp;
            }

            SnapContext context = SnapContext.Create(this, m_TerrainSystem, m_WaterSystem, m_ObjectSearchSystem, out JobHandle contextDeps);
            JobHandle jobHandle = new TransformToolSnapJob
            {
                m_Mode = m_Mode,
                m_Handle = m_SelectedHandle,
                m_UseLocalAxis = m_UseLocalAxis,
                m_SelectedEntity = m_DragEntries[m_DragPrimaryIndex].Original,
                m_RayOrigin = ray.origin,
                m_RayDirection = ray.direction,
                m_CameraRight = camera.transform.right,
                m_CameraForward = camera.transform.forward,
                m_RaycastLine = ToolRaycastSystem.CalculateRaycastLine(camera),
                m_AxisDir = GetSelectedAxisDirection(m_SelectedHandle, m_DragStartGizmoRot),
                m_DragStartGizmoPos = m_DragStartGizmoPos,
                m_DragStartSurfacePos = m_DragStartSurfacePos,
                m_DragStartGizmoRot = m_DragStartGizmoRot,
                m_DragStartPrimaryRot = m_DragStartPrimaryRot,
                m_DragStartMouseHitPos = m_DragStartMouseHitPos,
                m_XZHandleMode = m_XZHandleMode,
                m_HasHit = hasHit,
                m_ControlPoint = raw,
                m_Upright = m_Upright,
                m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
                m_GridEnabled = m_GridEnabled,
                m_SnapEach = m_PivotSettings.Aggregation == PivotAggregation.Individual,
                m_PosOffset = (float)m_PosOffset,
                m_RotOffset = (float)m_RotOffset,
                m_Context = context,
                m_Originals = originals,
                m_StartTransforms = startTransforms,
                m_Pivots = pivots,
                m_PrimaryIndex = m_DragPrimaryIndex,
                m_Results = results,
                m_Gizmo = m_DragGizmo,
            }.Schedule(JobHandle.CombineDependencies(inputDeps, contextDeps, m_DragGizmoDeps));
            SnapContext.AddReaders(m_WaterSystem, m_ObjectSearchSystem, jobHandle);
            originals.Dispose(jobHandle);
            startTransforms.Dispose(jobHandle);
            pivots.Dispose(jobHandle);

            // The temps are drag entries (Temp): no main thread part, see UpdateObject.
            m_UpdateObjectLookups.Update(ref CheckedStateRef);
            jobHandle = ScheduleUpdateObjects(jobHandle, temps, results, m_ToolOutputBarrier, m_UpdateObjectLookups);
            temps.Dispose(jobHandle);
            results.Dispose(jobHandle);

            m_DragGizmoDeps = jobHandle;
            return jobHandle;
        }

        private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false, bool toggleSelected = false, bool additive = false, bool setPrimary = false)
        {

            if(m_Mode == Mode.Default)
            {
                applyMode = ApplyMode.None;
                m_SelectedHandle = Handle.None;
                JobHandle jobHandle = SelectTempEntity(inputDeps, toggleSelected, additive, setPrimary);
                return jobHandle;
            }

            if (m_State == State.Idle)
            {
                applyMode = ApplyMode.None;
                NativeArray<RaycastResult> raycastResults = m_GimzosRaycastSystem.GetResult(this);

                if (raycastResults.Length <= 0) return inputDeps;

                RaycastResult raycastResult = raycastResults[0];

                if (raycastResult.m_Owner == Entity.Null) return inputDeps;

                StartDragging(raycastResult.m_Hit);

                return inputDeps;
            }

            if (m_State == State.Dragging)
            {
                if(m_DragEntries.Count > 0)
                {
                    if (GetAllowApply())
                    {
                        applyMode = ApplyMode.Apply;

                        Entity primary = m_DragEntries[m_DragPrimaryIndex].Original;
                        if (EntityManager.HasComponent<Building>(primary) || EntityManager.HasComponent<AssetStamp>(primary))
                        {
                            m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_RelocateBuildingSound);
                        }
                        else
                        {
                            m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlacePropSound);
                        }

                        Entity[] entities = new Entity[m_DragEntries.Count];
                        Transform[] startTransforms = new Transform[m_DragEntries.Count];
                        for (int i = 0; i < m_DragEntries.Count; i++)
                        {
                            entities[i] = m_DragEntries[i].Original;
                            startTransforms[i] = m_DragEntries[i].StartTransform;
                        }
                        m_UndoHistory.Push(ActionHistory.NewTransform(entities, startTransforms));
                        m_RedoHistory.Clear();

                        foreach (DragEntry entry in m_DragEntries)
                        {
                            if (EntityManager.TryGetComponent(entry.Temp, out Transform transform))
                            {
                                inputDeps = UpdateObject(inputDeps, entry.Original, transform.m_Position, transform.m_Rotation);
                            }
                        }
                    }
                    else
                    {
                        m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
                    }
                }
                else
                {
                    EDT.Logger.Warn("Try to Apply but there was nothing dragged.");
                }

                StopDragging();

                return inputDeps;
            }

            return inputDeps;
        }

        private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
        {
            if(m_Mode == Mode.Default)
            {
                applyMode = ApplyMode.Clear;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                return inputDeps;
            }

            if (m_State == State.Idle)
            {
                SetMode(Mode.Default);
                return UpdateGizmos(inputDeps);
            }

            if (m_State == State.Dragging)
            {
                StopDragging();
                inputDeps = UpdateGizmos(inputDeps);
                return inputDeps;
            }

            return inputDeps;
        }

        private JobHandle SelectTempEntity(JobHandle inputDeps, bool toggleSelected, bool additive = false, bool setPrimary = false)
        {
            if (m_TempQuery.IsEmptyIgnoreFilter)
            {
                if (!additive && !setPrimary) SetSelectedEntity(Entity.Null);
                return inputDeps;
            }
            NativeList<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out JobHandle outJobHandle);
            NativeReference<Entity> selected = new NativeReference<Entity>(Allocator.TempJob);
            JobHandle jobHandle = IJobExtensions.Schedule(new SelectEntityJob
            {
                m_Chunks = chunks,
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(true),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(true),
                m_AttachmentType = SystemAPI.GetComponentTypeHandle<Attachment>(true),
                m_ControllerType = SystemAPI.GetComponentTypeHandle<Controller>(true),
                m_EntityLookup = SystemAPI.GetEntityStorageInfoLookup(),
                m_TempData = SystemAPI.GetComponentLookup<Temp>(true),
                m_OwnerData = SystemAPI.GetComponentLookup<Owner>(true),
                m_TargetData = SystemAPI.GetComponentLookup<Target>(true),
                m_DebugData = SystemAPI.GetComponentLookup<Game.Tools.Debug>(true),
                m_IconData = SystemAPI.GetComponentLookup<Icon>(true),
                m_VehicleData = SystemAPI.GetComponentLookup<Vehicle>(true),
                m_BuildingData = SystemAPI.GetComponentLookup<Building>(true),
                m_DebugSelect = false,
                m_Selected = selected,
                m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
            }, JobHandle.CombineDependencies(inputDeps, outJobHandle));
            chunks.Dispose(jobHandle);
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();

            // With the selection highlighted, the vanilla job returns any Select temp: prefer the hovered one.
            Entity hovered = m_LastRaycastEntity;
            Entity selectedEntity;
            if (hovered == Entity.Null) selectedEntity = m_SelectedEntity;
            else if (HasSelectTemp(hovered)) selectedEntity = hovered;
            else selectedEntity = selected.Value;
            selected.Dispose();

            if (!base.EntityManager.HasBuffer<AggregateElement>(selectedEntity))
            {
                m_LastSelectedIndex = -1;
            }

            if (additive || setPrimary)
            {
                if (hovered != Entity.Null && selectedEntity != Entity.Null)
                {
                    if (setPrimary) SetPrimaryInSelection(selectedEntity);
                    else ToggleInSelection(selectedEntity);
                    PlaySelectedSound(selectedEntity, forcePlay: true);
                }
            }
            else if (m_SelectedEntity != selectedEntity || m_SelectedIndex != m_LastSelectedIndex || (hovered != Entity.Null && m_SelectedEntities.Count > 1))
            {
                SelectEntity(selectedEntity, m_LastSelectedIndex);
                PlaySelectedSound(selectedEntity, forcePlay: true);
            }
            else if (toggleSelected)
            {
                SelectEntity(Entity.Null, -1);
            }
            else
            {
                PlaySelectedSound(selectedEntity);
            }
            return jobHandle;
        }

        private bool HasSelectTemp(Entity original)
        {
            bool found = false;
            NativeArray<Temp> temps = m_TempQuery.ToComponentDataArray<Temp>(Allocator.Temp);
            for (int i = 0; i < temps.Length; i++)
            {
                // The primary of a multi-selection has a Parent temp instead (green highlight).
                TempFlags selectFlags = original == m_SelectedEntity ? TempFlags.Select | TempFlags.Parent : TempFlags.Select;
                if (temps[i].m_Original == original && (temps[i].m_Flags & selectFlags) != 0)
                {
                    found = true;
                    break;
                }
            }
            temps.Dispose();
            return found;
        }

        // extraEntity: the hovered entity in Default mode, gets a definition on top of the selection.
        private JobHandle UpdateDefinitions(JobHandle inputDeps, Entity extraEntity, int index)
        {
            m_DefinitionsDirty = false;
            JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);

            List<Entity> entities = GetSelectionRoots();
            if (extraEntity != Entity.Null && !m_SelectedEntities.Contains(extraEntity) && !IsOwnedBy(extraEntity, m_SelectedEntities))
            {
                entities.Add(extraEntity);
            }

            if (entities.Count > 0)
            {
                NativeArray<Entity> definitionEntities = new(entities.Count, Allocator.TempJob);
                NativeArray<bool> allowHighlights = new(entities.Count, Allocator.TempJob);
                for (int i = 0; i < entities.Count; i++)
                {
                    definitionEntities[i] = entities[i];
                    allowHighlights[i] = AllowHightlight(entities[i]);
                }
                NativeParallelHashSet<Entity> added = new(64, Allocator.TempJob);

                JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
                {
                    m_Entities = definitionEntities,
                    m_AllowHighlights = allowHighlights,
                    m_Primary = m_SelectedEntities.Count > 1 ? m_SelectedEntity : Entity.Null,
                    m_Added = added,
                    m_Position = default,
                    m_SetPosition = false,
                    m_Edges = SystemAPI.GetBufferLookup<ConnectedEdge>(true),
                    m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(true),
                    m_BuildingData = SystemAPI.GetComponentLookup<Building>(true),
                    m_LocalTransformCacheData = SystemAPI.GetComponentLookup<LocalTransformCache>(true),
                    m_EdgeData = SystemAPI.GetComponentLookup<Edge>(true),
                    m_NodeData = SystemAPI.GetComponentLookup<Game.Net.Node>(true),
                    m_CurveData = SystemAPI.GetComponentLookup<Curve>(true),
                    m_OwnerData = SystemAPI.GetComponentLookup<Owner>(true),
                    m_EditorContainerData = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(true),
                    m_TransformData = SystemAPI.GetComponentLookup<Transform>(true),
                    m_ElevationData = SystemAPI.GetComponentLookup<Game.Objects.Elevation>(true),
                    m_AttachedData = SystemAPI.GetComponentLookup<Attached>(true),
                    m_AttachmentData = SystemAPI.GetComponentLookup<Attachment>(true),
                    m_ServiceUpgradeData = SystemAPI.GetComponentLookup<Game.Buildings.ServiceUpgrade>(true),
                    m_LotData = SystemAPI.GetComponentLookup<Game.Areas.Lot>(true),
                    m_RoutePositionData = SystemAPI.GetComponentLookup<Position>(true),
                    m_RouteConnectedData = SystemAPI.GetComponentLookup<Connected>(true),
                    m_IconData = SystemAPI.GetComponentLookup<Icon>(true),
                    m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    m_AreaNodes = SystemAPI.GetBufferLookup<Game.Areas.Node>(true),
                    m_SubAreas = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true),
                    m_RouteWaypoints = SystemAPI.GetBufferLookup<RouteWaypoint>(true),
                    m_AggregateElements = SystemAPI.GetBufferLookup<AggregateElement>(true),
                    m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                    m_MoveSubBuildings = m_MoveSubBuildings,
                }, inputDeps);
                definitionEntities.Dispose(jobHandle2);
                allowHighlights.Dispose(jobHandle2);
                added.Dispose(jobHandle2);
                m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
                jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
                m_LastSelectedIndex = index;
            }
            return jobHandle;
        }

        // Idle: gizmo on the selection pivot. Dragging: the gizmo computed by UpdateDrag.
        private JobHandle UpdateGizmos(JobHandle inputDeps)
        {
            if (m_State == State.Dragging)
            {
                return UpdateGizmos(inputDeps, true, default, quaternion.identity, true);
            }

            List<Entity> entities = new();
            List<Transform> transforms = new();
            int primaryIndex = CollectRootTransforms(true, entities, transforms);
            if (entities.Count == 0)
            {
                return UpdateGizmos(inputDeps, false, default, quaternion.identity);
            }

            ComputeGroupPivots(entities, transforms, primaryIndex, out float3 gizmoPos, out quaternion gizmoRot, null);
            return UpdateGizmos(inputDeps, true, gizmoPos, gizmoRot);
        }

        private JobHandle UpdateGizmos(JobHandle inputDeps, bool hasGizmo, float3 position, quaternion rotation, bool useDragGizmo = false)
        {
            var c = m_HandleQuery.ToEntityListAsync(Allocator.TempJob, out JobHandle dep);

            Camera cam = Camera.main;
            float tanFOV = math.tan(math.radians(cam.fieldOfView) * 0.5f);

            JobHandle jobHandle = IJobExtensions.Schedule(new UpdateGizmosJob
            {
                m_Mode = m_Mode,
                m_State = m_State,
                m_HasGizmo = hasGizmo,
                m_SelectedHandle = m_SelectedHandle,
                m_Handles = c,
                m_GizmosDataLookup = SystemAPI.GetComponentLookup<GizmosData>(true),
                m_HighlightedLookup = SystemAPI.GetComponentLookup<Highlighted>(true),
                m_UseLocalAxis = m_UseLocalAxis,
                m_XZHandleMode = m_XZHandleMode,
                m_Position = position,
                m_Rotation = rotation,
                m_UseDragGizmo = useDragGizmo,
                m_DragGizmo = m_DragGizmo,
                m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
                m_CameraPosition = cam.transform.position,
                m_TanFOV = tanFOV,
                m_HandleScreenSize = EDT.m_Settings.HandleScreenSize,
            }, JobHandle.CombineDependencies(inputDeps, dep, m_DragGizmoDeps));
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            m_DragGizmoDeps = jobHandle;
            jobHandle = c.Dispose(jobHandle);
            return jobHandle;
        }

        private JobHandle UpdateObject(JobHandle inputDeps, Entity entity, float3 position)
        {
            return UpdateObject(inputDeps, entity, position, default);
        }

        private JobHandle UpdateObject(JobHandle inputDeps, Entity entity, quaternion rotation)
        {
            return UpdateObject(inputDeps, entity, default, rotation);
        }

        private JobHandle UpdateObject(JobHandle inputDeps, Entity entity, float3 position, quaternion rotation)
        {
            m_UpdateObjectLookups.Update(ref CheckedStateRef);
            return UpdateObject(inputDeps, entity, position, rotation, m_ToolOutputBarrier, m_UpdateObjectLookups);
        }

        // lookups: those of the calling system (see UpdateObjectLookups), already updated.
        internal JobHandle UpdateObject(JobHandle inputDeps, Entity entity, float3 position, quaternion rotation, SafeCommandBufferSystem ecb, UpdateObjectLookups lookups)
        {
            if (entity == Entity.Null) return inputDeps;

            if (!EntityManager.HasComponent<Temp>(entity))
            {
                if (EntityManager.HasComponent<Building>(entity))
                {
                    m_TerrainSystem.OnBuildingMoved(entity);

                    if(EntityManager.TryGetBuffer<InstalledUpgrade>(entity, true, out DynamicBuffer<InstalledUpgrade> buffer))
                    {
                        foreach (var upgrade in buffer)
                        {
                            if(EntityManager.HasComponent<Building>(upgrade.m_Upgrade)) m_TerrainSystem.OnBuildingMoved(upgrade.m_Upgrade);
                        }
                    }
                }

                if (AnarchyBridge.IsAvailable && EntityManager.TryGetComponent(entity, out Transform transform))
                {
                    ComponentType transformLock = AnarchyBridge.GetTransformLockComponentType();
                    ComponentType preventOverride = AnarchyBridge.GetAnarchyComponentType();

                    Transform newTransform = new(
                        position.Equals(default) ? transform.m_Position : position,
                        rotation.Equals(default) ? transform.m_Rotation : rotation
                    );

                    bool isStaticAndNotBuilding = EntityManager.HasComponent<Static>(entity) && !EntityManager.HasComponent<Building>(entity);

                    if (m_AddPreventOverride && isStaticAndNotBuilding && preventOverride != default && !EntityManager.HasComponent(entity, preventOverride))
                    {
                        AnarchyBridge.TryAddAnarchyComponent(entity);
                    }

                    if (transformLock != default && EntityManager.HasComponent(entity, transformLock))
                        AnarchyBridge.TryUpdateTransformLockComponent(entity, newTransform);
                    else if ((m_AddTransformLock && isStaticAndNotBuilding))
                        AnarchyBridge.TryAddTransformLockComponent(entity, newTransform);
                }
            }

            NativeArray<Entity> entities = new NativeArray<Entity>(1, Allocator.TempJob);
            NativeArray<Transform> transforms = new NativeArray<Transform>(1, Allocator.TempJob);
            entities[0] = entity;
            transforms[0] = new Transform(position, rotation);
            JobHandle jobHandle = ScheduleUpdateObjects(inputDeps, entities, transforms, ecb, lookups);
            entities.Dispose(jobHandle);
            transforms.Dispose(jobHandle);
            return jobHandle;
        }

        // One transform per entity, a default position / rotation keeps the current one. Temp entities only.
        private JobHandle ScheduleUpdateObjects(JobHandle inputDeps, NativeArray<Entity> entities, NativeArray<Transform> transforms, SafeCommandBufferSystem ecb, UpdateObjectLookups lookups)
        {
            JobHandle jobHandle = IJobExtensions.Schedule(new UpdateObjectJob
            {
                m_Entities = entities,
                m_Transforms = transforms,
                m_BuildingLookup = lookups.m_Building,
                m_TransformLookup = lookups.m_Transform,
                m_TempLookup = lookups.m_Temp,
                m_CurveLookup = lookups.m_Curve,
                m_ServiceUpgradeLookup = lookups.m_ServiceUpgrade,
                m_InstalledUpgradeLookup = lookups.m_InstalledUpgrade,
                m_NodeLookup = lookups.m_Node,
                m_SubAreaLookup = lookups.m_SubArea,
                m_SubNetLookup = lookups.m_SubNet,
                m_SubObjectLookup = lookups.m_SubObject,
                m_MoveSubBuildings = m_MoveSubBuildings,
                m_CommandBuffer = ecb.CreateCommandBuffer(),
            }, inputDeps);
            ecb.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }

        public void SetMode(Mode mode)
        {
            if(m_State == State.Dragging)
            {
                StopDragging();
            }
            SetState(State.Idle);

            if(mode != Mode.Default && m_SelectedEntity == Entity.Null)
            {
                return;
            }

            if (m_Mode == mode)
            {
                return;
            }

            m_Mode = mode;
            m_DefinitionsDirty = true;
            ModeChanged?.Invoke(m_Mode);
        }

        private void SetSelectedEntity(Entity entity)
        {
            SetSelection(null, entity);
        }

        // Replaces the whole selection. Null entities and duplicates are dropped, primary is added if missing.
        private void SetSelection(IEnumerable<Entity> entities, Entity primary)
        {
            List<Entity> selection = new();
            if (entities != null)
            {
                foreach (Entity entity in entities)
                {
                    if (entity != Entity.Null && !selection.Contains(entity)) selection.Add(entity);
                }
            }

            if (primary != Entity.Null && !selection.Contains(primary)) selection.Add(primary);
            if (primary == Entity.Null && selection.Count > 0) primary = selection[selection.Count - 1];

            bool selectionChanged = selection.Count != m_SelectedEntities.Count;
            for (int i = 0; !selectionChanged && i < selection.Count; i++)
            {
                selectionChanged = selection[i] != m_SelectedEntities[i];
            }

            if (selectionChanged)
            {
                m_SelectedEntities.Clear();
                m_SelectedEntities.AddRange(selection);
                m_DefinitionsDirty = true;
            }

            if (m_SelectedEntity != primary)
            {
                m_SelectedEntity = primary;
                m_DefinitionsDirty = true;
                SelectedEntityChanged?.Invoke(m_SelectedEntity);
            }

            if (selectionChanged)
            {
                SelectionChanged?.Invoke(m_SelectedEntities);
            }
        }

        private void SelectEntity(Entity entity, int index = -1)
        {
            Entity previousPrimary = m_SelectedEntity;
            Entity[] previousSelection = m_SelectedEntities.ToArray();
            SetSelectedEntity(entity);
            m_UndoHistory.Push(ActionHistory.NewSelect(m_SelectedEntity, m_SelectedEntities.ToArray(), previousPrimary, previousSelection));
            m_SelectedIndex = index;
            SetMode(m_SelectedEntity != Entity.Null ? Mode.Move : Mode.Default);
        }

        private void SelectEntities(IEnumerable<Entity> entities, Entity primary, bool switchMode)
        {
            Entity previousPrimary = m_SelectedEntity;
            Entity[] previousSelection = m_SelectedEntities.ToArray();
            SetSelection(new List<Entity>(entities), primary);
            m_UndoHistory.Push(ActionHistory.NewSelect(m_SelectedEntity, m_SelectedEntities.ToArray(), previousPrimary, previousSelection));
            m_SelectedIndex = -1;
            if (switchMode) SetMode(m_SelectedEntity != Entity.Null ? Mode.Move : Mode.Default);
        }

        // Shift+Click: adds the entity (it becomes the primary one) or removes it.
        private void ToggleInSelection(Entity entity)
        {
            List<Entity> selection = new(m_SelectedEntities);
            Entity primary;
            if (selection.Remove(entity))
            {
                primary = m_SelectedEntity != entity ? m_SelectedEntity : (selection.Count > 0 ? selection[selection.Count - 1] : Entity.Null);
            }
            else
            {
                selection.Add(entity);
                primary = entity;
            }
            SelectEntities(selection, primary, switchMode: false);
            if (m_SelectedEntity == Entity.Null) SetMode(Mode.Default);
        }

        // Alt+Click: the entity becomes the primary one, added to the selection if needed.
        private void SetPrimaryInSelection(Entity entity)
        {
            if (entity == m_SelectedEntity) return;
            List<Entity> selection = new(m_SelectedEntities);
            if (!selection.Contains(entity)) selection.Add(entity);
            SelectEntities(selection, entity, switchMode: false);
        }

        // Drops deleted entities (undo of a duplicate, removed by another tool, ...).
        private void ValidateSelection()
        {
            bool invalid = false;
            foreach (Entity entity in m_SelectedEntities)
            {
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity))
                {
                    invalid = true;
                    break;
                }
            }

            if (!invalid) return;

            List<Entity> valid = new();
            foreach (Entity entity in m_SelectedEntities)
            {
                if (EntityManager.Exists(entity) && !EntityManager.HasComponent<Deleted>(entity)) valid.Add(entity);
            }

            SetSelection(valid, valid.Contains(m_SelectedEntity) ? m_SelectedEntity : Entity.Null);
            if (m_SelectedEntity == Entity.Null) SetMode(Mode.Default);
        }

        // Selected entities that aren't owned (directly or not) by another selected entity, the owner moves them already.
        private List<Entity> GetSelectionRoots()
        {
            List<Entity> roots = new();
            foreach (Entity entity in m_SelectedEntities)
            {
                if (!EntityManager.Exists(entity)) continue;
                if (IsOwnedBy(entity, m_SelectedEntities)) continue;
                roots.Add(entity);
            }
            return roots;
        }

        private bool IsOwnedBy(Entity entity, List<Entity> owners)
        {
            Entity current = entity;
            for (int guard = 0; guard < 32 && EntityManager.TryGetComponent(current, out Owner owner) && owner.m_Owner != Entity.Null; guard++)
            {
                if (owner.m_Owner != entity && owners.Contains(owner.m_Owner)) return true;
                current = owner.m_Owner;
            }
            return false;
        }

        // Selection roots with a Transform. Returns the index of the primary entity (or of the root that owns it).
        private int CollectRootTransforms(bool interpolated, List<Entity> entities, List<Transform> transforms)
        {
            entities.Clear();
            transforms.Clear();
            foreach (Entity entity in GetSelectionRoots())
            {
                Transform transform;
                if (interpolated && EntityManager.TryGetComponent(entity, out InterpolatedTransform interpolatedTransform))
                {
                    transform = interpolatedTransform.ToTransform();
                }
                else if (!EntityManager.TryGetComponent(entity, out transform))
                {
                    continue;
                }
                entities.Add(entity);
                transforms.Add(transform);
            }

            Entity current = m_SelectedEntity;
            for (int guard = 0; guard < 32 && current != Entity.Null; guard++)
            {
                int index = entities.IndexOf(current);
                if (index >= 0) return index;
                current = EntityManager.TryGetComponent(current, out Owner owner) ? owner.m_Owner : Entity.Null;
            }
            return entities.Count - 1;
        }

        // entities: null to use the origins whatever the pivot source.
        private void ComputeGroupPivots(List<Entity> entities, List<Transform> transforms, int primaryIndex, out float3 gizmoPos, out quaternion gizmoRot, float3[] pivots)
        {
            NativeArray<Transform> transformArray = new(transforms.Count, Allocator.Temp);
            NativeArray<Bounds3> boundsArray = new(entities != null ? transforms.Count : 0, Allocator.Temp);
            NativeArray<float3> pivotArray = new(transforms.Count, Allocator.Temp);
            for (int i = 0; i < transforms.Count; i++)
            {
                transformArray[i] = transforms[i];
            }
            for (int i = 0; i < boundsArray.Length; i++)
            {
                if (EntityManager.TryGetComponent(entities[i], out PrefabRef prefabRef) && EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData))
                {
                    boundsArray[i] = geometryData.m_Bounds;
                }
            }

            ComputePivots(transformArray, boundsArray, primaryIndex, m_PivotSettings, out gizmoPos, out gizmoRot, pivotArray);

            if (pivots != null)
            {
                for (int i = 0; i < pivots.Length && i < pivotArray.Length; i++)
                {
                    pivots[i] = pivotArray[i];
                }
            }

            transformArray.Dispose();
            boundsArray.Dispose();
            pivotArray.Dispose();
        }

        private ActionHistory CaptureTransforms(Entity[] entities)
        {
            Transform[] transforms = new Transform[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                EntityManager.TryGetComponent(entities[i], out transforms[i]);
            }
            return ActionHistory.NewTransform(entities, transforms);
        }

        private JobHandle RestoreTransforms(JobHandle inputDeps, ActionHistory actionHistory)
        {
            for (int i = 0; i < actionHistory.Entities.Length; i++)
            {
                Entity entity = actionHistory.Entities[i];
                if (!EntityManager.Exists(entity) || !EntityManager.HasComponent<Transform>(entity)) continue;

                Transform transform = actionHistory.Transforms[i];
                inputDeps = UpdateObject(inputDeps, entity, transform.m_Position, transform.m_Rotation);
            }
            return inputDeps;
        }

        public void SnapOnGround()
        {
            m_RequestSnapOnGround = true;
        }

        public void Duplicate()
        {
            m_RequestDuplicate = true;
        }

        private void SetState(State state)
        {
            m_State = state;
        }

        private void EnableActions(bool enable)
        {
            applyAction.shouldBeEnabled = enable;
            secondaryApplyAction.shouldBeEnabled = enable;
            cancelAction.shouldBeEnabled = enable;
        }

        private void StartDragging(RaycastHit raycastHit)
        {
            m_SelectedTempEntity = Entity.Null;
            m_DragEntries.Clear();
            if (m_TempQuery.IsEmptyIgnoreFilter)
            {
                EDT.Logger.Warn("Try to drag but no temp found.");
                SetState(State.Idle);
                return;
            }

            if (!EntityManager.TryGetComponent<TransformGizmosHandle>(raycastHit.m_HitEntity, out var component))
            {
                SetState(State.Idle);
                return;
            }

            List<Entity> entities = new();
            List<Transform> transforms = new();
            int primaryIndex = CollectRootTransforms(false, entities, transforms);

            // Temp of each selection root.
            Dictionary<Entity, Entity> tempEntities = new();
            Dictionary<Entity, Temp> temps = new();
            NativeArray<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);

            try
            {
                var entityHandle = EntityManager.GetEntityTypeHandle();
                var tempHandle = GetComponentTypeHandle<Temp>(true);

                for (int i = 0; i < chunks.Length; i++)
                {
                    ArchetypeChunk chunk = chunks[i];

                    NativeArray<Entity> chunkEntities = chunk.GetNativeArray(entityHandle);
                    NativeArray<Temp> chunkTemps = chunk.GetNativeArray(ref tempHandle);

                    for (int j = 0; j < chunkEntities.Length; j++)
                    {
                        Temp temp = chunkTemps[j];
                        if (!tempEntities.ContainsKey(temp.m_Original) && entities.Contains(temp.m_Original))
                        {
                            tempEntities.Add(temp.m_Original, chunkEntities[j]);
                            temps.Add(temp.m_Original, temp);
                        }
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }

            Entity primaryEntity = primaryIndex >= 0 ? entities[primaryIndex] : Entity.Null;
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (tempEntities.ContainsKey(entities[i])) continue;
                entities.RemoveAt(i);
                transforms.RemoveAt(i);
            }

            if (entities.Count == 0)
            {
                EDT.Logger.Warn("Try to drag but m_SelectedTempEntity was null.");
                SetState(State.Idle);
                return;
            }

            primaryIndex = math.max(entities.IndexOf(primaryEntity), 0);

            // Pivots are frozen until StopDragging, recomputing them would make them follow the objects.
            float3[] pivots = new float3[entities.Count];
            ComputeGroupPivots(entities, transforms, primaryIndex, out float3 gizmoPos, out quaternion gizmoRot, pivots);

            for (int i = 0; i < entities.Count; i++)
            {
                m_DragEntries.Add(new DragEntry
                {
                    Original = entities[i],
                    Temp = tempEntities[entities[i]],
                    StartTransform = transforms[i],
                    Pivot = pivots[i],
                });
            }

            m_DragPrimaryIndex = primaryIndex;
            m_SelectedTempEntity = m_DragEntries[primaryIndex].Temp;
            m_SelectedHandle = component.Handle;

            m_DragStartGizmoPos = gizmoPos;
            ComputeGroupPivots(null, transforms, primaryIndex, out m_DragStartSurfacePos, out _, null);
            m_DragStartGizmoRot = gizmoRot;
            m_DragStartPrimaryRot = transforms[primaryIndex].m_Rotation;
            // The gizmo jobs of the previous frames may still use it.
            m_DragGizmoDeps.Complete();
            m_DragGizmo.Value = new DragGizmoResult { m_Position = gizmoPos, m_Rotation = gizmoRot };

            if(m_XZHandleMode != XZHandleMode.FollowSurface || m_SelectedHandle != Handle.XZ)
            {
                float3 axisDir = GetSelectedAxisDirection(m_SelectedHandle, m_DragStartGizmoRot);
                Camera camera = Camera.main;
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                bool axisIsNormal = m_Mode == Mode.Rotate || m_SelectedHandle == Handle.XZ;

                if (RaycastDragPlane(ray.origin, ray.direction, axisDir, m_DragStartGizmoPos, axisIsNormal, camera.transform.right, camera.transform.forward, out float3 hitPos))
                {
                    m_DragStartMouseHitPos = hitPos;
                    if (m_Mode == Mode.Rotate)
                    {
                        m_DragStartMouseHitPos = math.normalize(m_DragStartMouseHitPos - m_DragStartGizmoPos);
                    }
                }
                else
                {
                    m_DragStartMouseHitPos = m_DragStartGizmoPos;
                }
            }

            EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            ComponentType preventOverride = AnarchyBridge.IsAvailable ? AnarchyBridge.GetAnarchyComponentType() : default;

            foreach (DragEntry entry in m_DragEntries)
            {
                Temp temp = temps[entry.Original];
                temp.m_Flags |= TempFlags.Dragging;
                // Restored by the definitions recreated once back to Idle.
                if (!AllowDragHightlight(entry.Original)) temp.m_Flags &= ~(TempFlags.Select | TempFlags.Parent);
                entityCommandBuffer.SetComponent(entry.Temp, temp);
                entityCommandBuffer.AddComponent(entry.Temp, default(Updated));

                if (preventOverride != default && EntityManager.HasComponent(entry.Original, preventOverride))
                {
                    entityCommandBuffer.RemoveComponent(entry.Original, preventOverride);
                }

                entityCommandBuffer.AddComponent<Overridden>(entry.Original);
            }

            SetState(State.Dragging);
        }

        private void StopDragging()
        {
            if (m_DragEntries.Count > 0)
            {
                EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
                foreach (DragEntry entry in m_DragEntries)
                {
                    if (EntityManager.TryGetComponent(entry.Temp, out Temp temp))
                    {
                        temp.m_Flags &= ~TempFlags.Dragging;
                        entityCommandBuffer.SetComponent(entry.Temp, temp);
                        entityCommandBuffer.AddComponent(entry.Temp, default(Updated));
                    }
                    entityCommandBuffer.RemoveComponent<Overridden>(entry.Original);
                }
            }
            else
            {
                EDT.Logger.Warn("Try to stop dragging but m_SelectedTempEntity was null.");
            }
            m_DragEntries.Clear();
            m_SelectedTempEntity = Entity.Null;
            m_SelectedHandle = Handle.None;
            SetState(State.Idle);
        }

        // rot: the gizmo rotation (identity in Global orientation).
        private float3 GetSelectedAxisDirection(Handle handle, quaternion rot)
        {
            if (handle == Handle.X)
                return math.rotate(rot, new float3(1, 0, 0));

            if (handle == Handle.Y)
                return math.rotate(rot, new float3(0, 1, 0));

            if (handle == Handle.Z)
                return math.rotate(rot, new float3(0, 0, 1));

            if (handle == Handle.XZ)
            {
                switch(m_XZHandleMode)
                {
                    case XZHandleMode.FollowSurface:
                    case XZHandleMode.FixedY:
                        return new float3(0, 1, 0);

                    case XZHandleMode.FixedX:
                        return math.rotate(rot, new float3(1, 0, 0));

                    case XZHandleMode.FixedZ:
                        return math.rotate(rot, new float3(0, 0, 1));
                }
            }

            return float3.zero;
        }

        // Idle: the selection stays highlighted in Move / Rotate too, to see what Shift+Click / Alt+Click change.
        private bool AllowHightlight(Entity entity)
        {
            return m_Mode == Mode.Default || m_AllowHightlight != AllowHightlightState.Disabled;
        }

        // While dragging, the highlight hides where the objects go: removed from the dragged temps (see StartDragging).
        private bool AllowDragHightlight(Entity entity)
        {
            if (m_AllowHightlight == AllowHightlightState.Enabled) return true;
            if (m_AllowHightlight == AllowHightlightState.Disabled) return false;

            if (EntityManager.HasComponent<Building>(entity)
                || EntityManager.HasBuffer<SubObject>(entity)
            )
                return true;

            return false;
        }

        private bool IsHistoryEntryUsable(ActionHistory actionHistory, bool forRedo)
        {
            switch (actionHistory.ActionType)
            {
                case ActionType.Select:
                    {
                        Entity target = forRedo ? actionHistory.Entity : actionHistory.SelectedEntity;
                        return target == Entity.Null || EntityManager.Exists(target);
                    }
                case ActionType.Create:
                    return FilterExisting(forRedo ? actionHistory.PreviousSelection : actionHistory.Entities).Count > 0;
                case ActionType.Transform:
                    {
                        if (actionHistory.Entities == null) return false;
                        foreach (Entity entity in actionHistory.Entities)
                        {
                            if (entity != Entity.Null && EntityManager.Exists(entity) && EntityManager.HasComponent<Transform>(entity)) return true;
                        }
                        return false;
                    }
                default:
                    return false;
            }
        }

        // Panel edit of a group: the primary goes to primaryTarget, the others get the same delta around their pivots.
        internal JobHandle TransformSelection(JobHandle inputDeps, Transform primaryTarget, SafeCommandBufferSystem ecb, UpdateObjectLookups lookups)
        {
            List<Entity> entities = new();
            List<Transform> transforms = new();
            int primaryIndex = CollectRootTransforms(false, entities, transforms);
            if (entities.Count == 0) return inputDeps;

            float3 positionOffset = primaryTarget.m_Position - transforms[primaryIndex].m_Position;
            quaternion rotationDelta = math.mul(primaryTarget.m_Rotation, math.inverse(transforms[primaryIndex].m_Rotation));

            float3[] pivots = new float3[entities.Count];
            ComputeGroupPivots(entities, transforms, primaryIndex, out _, out _, pivots);

            for (int i = 0; i < entities.Count; i++)
            {
                Transform start = transforms[i];
                float3 position = pivots[i] + positionOffset + math.mul(rotationDelta, start.m_Position - pivots[i]);
                quaternion rotation = math.normalize(math.mul(rotationDelta, start.m_Rotation));
                inputDeps = UpdateObject(inputDeps, entities[i], position, rotation, ecb, lookups);
            }
            return inputDeps;
        }

        // Every entity of the selection must be copyable to be duplicated.
        public bool CanDuplicateSelection()
        {
            if (m_SelectedEntities.Count == 0) return false;
            foreach (Entity entity in m_SelectedEntities)
            {
                if (!IsCopyable(entity)) return false;
            }
            return true;
        }

        public bool IsCopyable(Entity entity)
        {
            // Maybe also exclude Owner ?
            return EntityManager.HasComponent<Game.Objects.Object>(entity) &&
                EntityManager.HasComponent<Transform>(entity) &&
                EntityManager.HasComponent<PrefabRef>(entity) &&
                !EntityManager.HasComponent<Building>(entity) &&
                !EntityManager.HasComponent<Game.Buildings.Extension>(entity) &&
                !EntityManager.HasComponent<Game.Objects.Moving>(entity) &&
                !EntityManager.HasComponent<Temp>(entity) &&
                !EntityManager.HasComponent<Deleted>(entity);
        }

        // SetSelection without the entities deleted since the history entry was made.
        private void SetSelectionFiltered(Entity[] entities, Entity primary)
        {
            List<Entity> existing = FilterExisting(entities);
            SetSelection(existing, existing.Contains(primary) ? primary : Entity.Null);
        }

        private List<Entity> FilterExisting(Entity[] entities)
        {
            List<Entity> result = new();
            if (entities == null) return result;
            foreach (Entity entity in entities)
            {
                if (entity != Entity.Null && EntityManager.Exists(entity) && !EntityManager.HasComponent<Deleted>(entity)) result.Add(entity);
            }
            return result;
        }
    }
}
