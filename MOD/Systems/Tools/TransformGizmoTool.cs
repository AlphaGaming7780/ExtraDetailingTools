using Colossal.Entities;
using Colossal.Mathematics;
using ExtraDetailingTools.Components;
using ExtraDetailingTools.Gizmos;
using ExtraDetailingTools.Systems.UI;
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
using Game.Vehicles;
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
using Plane = UnityEngine.Plane;
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
            [ReadOnly]
            public Entity m_Entity;

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

            [ReadOnly]
            public bool AllowHighlight;

            public void Execute()
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
                Entity e = m_CommandBuffer.CreateEntity();
                CreationDefinition component = new CreationDefinition
                {
                    m_Original = entity
                };
                if (isParent)
                {
                    component.m_Flags |= CreationFlags.Parent | CreationFlags.Duplicate;
                }
                else if (AllowHighlight)
                {
                    component.m_Flags |= CreationFlags.Select;
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
            [ReadOnly] public Entity m_SelectedEntity;
            [ReadOnly] public Handle m_SelectedHandle;
            [ReadOnly] public ComponentLookup<Transform> m_TransformLookup;
            [ReadOnly] public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformLookup;
            [ReadOnly] public ComponentLookup<GizmosData> m_GizmosDataLookup;
            [ReadOnly] public ComponentLookup<Highlighted> m_HighlightedLookup;

            [ReadOnly] public bool m_UseLocalAxis;
            [ReadOnly] public XZHandleMode m_XZHandleMode;
            [ReadOnly] public float3 m_Position;
            [ReadOnly] public quaternion m_Rotation;

            [ReadOnly] public EntityCommandBuffer m_CommandBuffer;

            [ReadOnly] public TerrainHeightData m_TerrainHeightData;

            [ReadOnly] public NativeList<Entity> m_Handles;

            // Screen-proportional handle sizing: the handle keeps the same size relative to the vertical
            // field of view, so it looks the same size on screen at any resolution (not a fixed pixel count).
            [ReadOnly] public float3 m_CameraPosition;
            [ReadOnly] public float m_TanFOV;
            [ReadOnly] public float m_HandleScreenSize; // desired handle size, in pixels, at kReferenceScreenHeight (user setting)

            private const float kReferenceScreenHeight = 1080f;

            private int m_ReuseIndex;

            public void Execute()
            {
                m_ReuseIndex = 0;
                if (m_Mode == Mode.Default)
                {
                    DestroyAllEntities();
                    return;
                }

                Transform transform;
                if (m_InterpolatedTransformLookup.TryGetComponent(m_SelectedEntity, out InterpolatedTransform interpolatedTransform))
                {
                    transform = interpolatedTransform.ToTransform();
                }
                else if (!m_TransformLookup.TryGetComponent(m_SelectedEntity, out transform))
                {
                    DestroyAllEntities();
                    return;
                }

                float3 pos =     !m_Position.Equals(default) ? m_Position : transform.m_Position;
                quaternion rot = !m_Rotation.Equals(default) ? m_Rotation : transform.m_Rotation;

                // Keep the handle at a constant screen-space size (relative to the vertical FOV, not a fixed
                // pixel count), regardless of the selected object's size, its distance to the camera, or the
                // render resolution — pixelHeight cancels out entirely when sizing by a fraction of the screen.
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
                float radius = math.cmax(size) * 0.1f; //(math.csum(size) / 3f) * 0.115f;
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
                GizmosData data = GizmosUtils.DrawWireArc(center, normal, from, 360, radius, color);
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

#if RELEASE
        [BurstCompile]
#endif
        private struct UpdateObjectJob : IJob
        {
            [ReadOnly] public float3 m_Position;
            [ReadOnly] public quaternion m_Rotation;
            [ReadOnly] public Entity m_SelectedEntity;
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
                isTemp = m_TempLookup.HasComponent(m_SelectedEntity);

                UpdateObject(m_SelectedEntity, m_Position, m_Rotation);
            }


            private void UpdateObject(Entity entity, float3 position, quaternion rotation)
            {
                if (!m_TransformLookup.TryGetComponent(entity, out Transform transform)) return;

                SetTempFlag(entity);

                position = !position.Equals(default) ? position : transform.m_Position;
                rotation = !rotation.Equals(default) ? rotation : transform.m_Rotation;

                float3 positionOffset = position - transform.m_Position;
                quaternion rotationOffset = math.mul(rotation, math.inverse(transform.m_Rotation));

                // I need that ? Nop, get updated by the game.
                //if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) && EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData) && EntityManager.TryGetComponent(selectedEntity, out CullingInfo cullingInfo))
                //{
                //    Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
                //    cullingInfo.m_Bounds = bounds3;
                //    EntityManager.SetComponentData(entity, cullingInfo);
                //}

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

        private struct ActionHistory
        {
            public Mode ActionType;
            public Entity Entity;
            public float3 Position;
            public quaternion Rotation;

            public ActionHistory(Mode actionType, Entity entity, float3 position, quaternion rotation)
            {
                ActionType = actionType;
                Entity = entity;
                Position = position;
                Rotation = rotation;
            }

            public static ActionHistory NewMove(Entity entity, float3 position)
            {
                return new ActionHistory(Mode.Move, entity, position, default);
            }

            public static ActionHistory NewRotation(Entity entity, quaternion rotation)
            {
                return new ActionHistory(Mode.Rotate, entity, default, rotation);
            }

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
            All = uint.MaxValue,
        }

        public override string toolID => "TransformGizmoTool";

        private AudioManager m_AudioManager;
        private TerrainSystem m_TerrainSystem;
        private GizmosRaycastSystem m_GimzosRaycastSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private TransformGizmoToolUI m_TransformGizmoToolUI;

        private Mode m_Mode = Mode.Default;
        private XZHandleMode m_XZHandleMode = XZHandleMode.FollowSurface;

        private State m_State = State.Idle;

        private RaycastFilter m_RaycastFilter = RaycastFilter.None;

        private EntityQuery m_SoundQuery;
        private EntityQuery m_DefinitionQuery;
        private EntityQuery m_TempQuery;
        private EntityQuery m_HandleQuery;

        private Entity m_LastRaycastEntity;
        private Entity m_SelectedEntity;
        private Entity m_SelectedTempEntity;

        private Handle m_SelectedHandle;

        private int m_LastSelectedIndex;
        private int m_SelectedIndex;
        private float3 m_DragStartGizmoPos;
        private quaternion m_DragStartGizmoRot;
        private float3 m_DragStartMouseHitPos;

        // Bindings
        private ProxyAction m_UndoAction;
        private ProxyAction m_RedoAction;
        private ProxyAction m_MoveAction;
        private ProxyAction m_RotateAction;

        // Undo / Redo
        private Stack<ActionHistory> m_UndoHistory;
        private Stack<ActionHistory> m_RedoHistory;

        // Quick Actions
        private bool m_RequestSnapOnGround = false;
        private bool m_RequestUndo = false;
        private bool m_RequestRedo = false;

        // SIP Support
        private bool m_WasAnEntitySelected = false;

        // Anarchy Support
        internal bool m_AddPreventOverride = true;
        internal bool m_AddTransformLock = true;

        public bool m_UseLocalAxis { get; internal set; } = true;
        public bool m_MoveSubBuildings { get; internal set; } = true;
        public bool m_Underground { get; private set; } = false;
        public AllowHightlightState m_AllowHightlight { get; set; } = AllowHightlightState.Default;

        public override int uiModeIndex => (int)m_Mode;
        public override bool allowUnderground { get => true; protected set { } }

        public Mode mode => m_Mode;

        public XZHandleMode xzHandleMode { get { return m_XZHandleMode; } internal set { m_XZHandleMode = value; } }
        public RaycastFilter raycastFilter { get { return m_RaycastFilter; } internal set { m_RaycastFilter = value; } }

        public Entity SelectedTempEntity => m_SelectedTempEntity;

        public Entity SelectedEntity => m_SelectedEntity;

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

            m_TransformGizmoToolUI = World.GetOrCreateSystemManaged<TransformGizmoToolUI>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_AudioManager = World.GetOrCreateSystemManaged<AudioManager>();
            m_GimzosRaycastSystem = World.GetOrCreateSystemManaged<GizmosRaycastSystem>();

            m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
            m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
            m_HandleQuery = GetEntityQuery(ComponentType.ReadOnly<TransformGizmosHandle>());
            m_DefinitionQuery = GetDefinitionQuery();

            m_UndoHistory = new();
            m_RedoHistory = new();

            m_UndoAction = EDT.m_Settings.GetAction(EDT.m_Settings.UndoBinding.actionName);
            m_RedoAction = EDT.m_Settings.GetAction(EDT.m_Settings.RedoBinding.actionName);
            m_MoveAction = EDT.m_Settings.GetAction(EDT.m_Settings.EnterMoveBinding.actionName);
            m_RotateAction = EDT.m_Settings.GetAction(EDT.m_Settings.EnterRotateBinding.actionName);
        }

        protected override void OnDestroy()
        {
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
            m_SelectedEntity = m_ToolSystem.selected;
            m_WasAnEntitySelected = m_SelectedEntity != Entity.Null;
            m_ToolSystem.selected = Entity.Null;
            m_TransformGizmoToolUI.SetMode(Mode.Default);
            EnableActions(true);
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_UndoAction.shouldBeEnabled = false;
            m_RedoAction.shouldBeEnabled = false;
            m_MoveAction.shouldBeEnabled = false;
            m_RotateAction.shouldBeEnabled = false;
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
            m_TransformGizmoToolUI.SetMode(Mode.Default);
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
                    m_Tolerance = m_Mode == Mode.Rotate ? 0.1f : 0,
                    m_Debug = false
                };
                m_GimzosRaycastSystem.AddInput(this, input);

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
                    }

                    m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net | TypeMask.Lanes;
                    m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.Fence | Layer.TrainTrack | Layer.SubwayTrack | Layer.TrainTrack;

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

            if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable)) == 0)// | RaycastFlags.UIDisable)
            {
                switch (m_Mode)
                {
                    case Mode.Default:
                        {
                            if(applyAction.WasPressedThisFrame())
                            {
                                return Apply(inputDeps, base.applyAction.WasReleasedThisFrame(), base.cancelAction.WasPressedThisFrame());
                            } else if(applyAction.WasReleasedThisFrame())
                            {
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
                            else if ((m_State == State.Idle && base.applyAction.WasPressedThisFrame()) || (m_State == State.Dragging && base.applyAction.WasReleasedThisFrame()))
                            {
                                return Apply(inputDeps);
                            }
                            return Update(inputDeps);
                        }
                }
            }

            m_State = State.Idle;
            m_TransformGizmoToolUI.SetMode((int)Mode.Default);
            return inputDeps;
        }

        private JobHandle Update(JobHandle inputDeps)
        {
            float3 newPos = default;
            quaternion newRot = default;

            if(m_MoveAction.WasPressedThisFrame())
            {
                if(m_Mode != Mode.Move)
                    m_TransformGizmoToolUI.SetMode(Mode.Move);
                else
                    m_TransformGizmoToolUI.SetMode(Mode.Default);
            }

            if (m_RotateAction.WasPressedThisFrame())
            {
                if (m_Mode != Mode.Rotate)
                    m_TransformGizmoToolUI.SetMode(Mode.Rotate);
                else
                    m_TransformGizmoToolUI.SetMode(Mode.Default);
            }

            if (m_RequestSnapOnGround)
            {
                m_RequestSnapOnGround = false;
                if (m_State == State.Idle && m_SelectedEntity != Entity.Null && EntityManager.TryGetComponent(m_SelectedEntity, out Transform snapTransform))
                {
                    TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
                    float terrainHeight = TerrainUtils.SampleHeight(ref heightData, snapTransform.m_Position);
                    float3 snapPos = snapTransform.m_Position;
                    snapPos.y = terrainHeight;
                    inputDeps = UpdateObject(inputDeps, m_SelectedEntity, snapPos);
                    m_UndoHistory.Push(ActionHistory.NewMove(m_SelectedEntity, snapTransform.m_Position));
                }
            }

            if (m_RequestUndo || m_UndoAction.WasPressedThisFrame())
            {
                m_RequestUndo = false;
                if(m_State == State.Idle && m_UndoHistory.Count > 0)
                {
                    ActionHistory actionHistory = m_UndoHistory.Pop();
                    Entity entity = actionHistory.Entity;
                    while (m_UndoHistory.Count > 0 && (
                        entity == Entity.Null ||
                        !EntityManager.Exists(entity) ||
                        !EntityManager.TryGetComponent<Transform>(entity, out _)))
                    {
                        actionHistory = m_UndoHistory.Pop();
                        entity = actionHistory.Entity;
                    }

                    if (entity != Entity.Null && EntityManager.Exists(entity) && EntityManager.TryGetComponent<Transform>(entity, out Transform currentTransform))
                    {
                        Mode actionType = actionHistory.ActionType;
                        if (actionType == Mode.Move)
                        {
                            m_RedoHistory.Push(ActionHistory.NewMove(entity, currentTransform.m_Position));
                            inputDeps = UpdateObject(inputDeps, entity, actionHistory.Position);
                        }
                        else if (actionType == Mode.Rotate)
                        {
                            m_RedoHistory.Push(ActionHistory.NewRotation(entity, currentTransform.m_Rotation));
                            inputDeps = UpdateObject(inputDeps, entity, actionHistory.Rotation);
                        }
                        else
                        {
                            EDT.Logger.Warn($"Undo: unhandled action type {actionType}");
                        }
                    }
                }
            }
            else if(m_RequestRedo || m_RedoAction.WasPressedThisFrame())
            {
                m_RequestRedo = false;
                if (m_State == State.Idle && m_RedoHistory.Count > 0)
                {
                    ActionHistory actionHistory = m_RedoHistory.Pop();
                    Entity entity = actionHistory.Entity;
                    while (m_RedoHistory.Count > 0 && (
                        entity == Entity.Null ||
                        !EntityManager.Exists(entity) ||
                        !EntityManager.TryGetComponent<Transform>(entity, out _)))
                    {
                        actionHistory = m_RedoHistory.Pop();
                        entity = actionHistory.Entity;
                    }

                    if (entity != Entity.Null && EntityManager.Exists(entity) && EntityManager.TryGetComponent<Transform>(entity, out Transform currentTransform))
                    {
                        Mode actionType = actionHistory.ActionType;
                        if (actionType == Mode.Move)
                        {
                            m_UndoHistory.Push(ActionHistory.NewMove(entity, currentTransform.m_Position));
                            inputDeps = UpdateObject(inputDeps, entity, actionHistory.Position);
                        }
                        else if (actionType == Mode.Rotate)
                        {
                            m_UndoHistory.Push(ActionHistory.NewRotation(entity, currentTransform.m_Rotation));
                            inputDeps = UpdateObject(inputDeps, entity, actionHistory.Rotation);
                        }
                        else
                        {
                            EDT.Logger.Warn($"Redo: unhandled action type {actionType}");
                        }
                    }
                }
            }


            if (m_Mode == Mode.Default)
            {
                if (GetRaycastResult(out var entity2, out var hit2, out var forceUpdate)
                    && entity2 == m_LastRaycastEntity
                    && !forceUpdate)
                {
                    applyMode = ApplyMode.None;
                }
                else
                {
                    m_LastRaycastEntity = entity2;
                    if (entity2 == Entity.Null) entity2 = m_SelectedEntity;

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

                    applyMode = ApplyMode.Clear;
                    inputDeps = UpdateDefinitions(inputDeps, m_SelectedEntity, m_SelectedIndex);
                }
                else if (m_State == State.Dragging)
                {
                    applyMode = ApplyMode.None;

                    if (m_XZHandleMode == XZHandleMode.FollowSurface && m_SelectedHandle == Handle.XZ)
                    {
                        if (GetRaycastResult(out Entity entity, out RaycastHit hit))
                        {
                            newPos = hit.m_HitPosition;

                            if(!EntityManager.HasComponent<Game.Common.Terrain>(entity))
                            {
                                float3 up = math.normalize(hit.m_HitDirection);
                                float3 r = math.abs(up.y) < 0.99f ? math.up() : math.forward();
                                float3 right = math.normalize(math.cross(up, r));
                                float3 fwd = math.cross(right, up);
                                newRot = quaternion.LookRotationSafe(fwd, up);
                            }
                            else if(EntityManager.TryGetComponent<Transform>(m_SelectedEntity, out var transformComponent))
                            {
                                newRot = transformComponent.m_Rotation;
                            } 
                            else
                            {
                                newRot = quaternion.LookRotationSafe(math.forward(), math.up());
                            }

                            if (m_SelectedTempEntity != Entity.Null)
                            {
                                inputDeps = UpdateObject(inputDeps, m_SelectedTempEntity, newPos, newRot);
                            }
                            else
                            {
                                EDT.Logger.Error("m_SelectedTempEntity is null");
                            }
                        }
                    }
                    else
                    {
                        float3 axisDir = GetSelectedAxisDirection(m_SelectedHandle);
                        Plane dragPlane = CreateDragPlane(axisDir, m_DragStartGizmoPos);
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (dragPlane.Raycast(ray, out float enter))
                        {
                            float3 hitPos = ray.origin + ray.direction * enter;
                            if (m_Mode == Mode.Move)
                            {
                                float3 mouseDelta = hitPos - m_DragStartMouseHitPos;

                                if (m_SelectedHandle == Handle.XZ)
                                {
                                    newPos = m_DragStartGizmoPos + mouseDelta;
                                }
                                else
                                {
                                    float3 projectedDelta = math.dot(mouseDelta, axisDir) * axisDir;
                                    newPos = projectedDelta + m_DragStartGizmoPos;
                                }

                            }
                            else if (m_Mode == Mode.Rotate)
                            {
                                float3 currentDir = math.normalize(hitPos - m_DragStartGizmoPos);
                                float3 startDir = m_DragStartMouseHitPos;

                                float angle = math.atan2(
                                    math.dot(axisDir, math.cross(startDir, currentDir)),
                                    math.dot(startDir, currentDir)
                                );

                                quaternion deltaRot = quaternion.AxisAngle(axisDir, angle);

                                newRot = math.mul(deltaRot, m_DragStartGizmoRot);
                            } 
                            else
                            {
                                EDT.Logger.Warn($"Try to update in State.Dragging but the tool mode wasn't move or rotate, current tool mode: {m_Mode}");
                            }

                            if (m_SelectedTempEntity != Entity.Null)
                            {
                                inputDeps = UpdateObject(inputDeps, m_SelectedTempEntity, newPos, newRot);
                            }
                            else
                            {
                                EDT.Logger.Error("m_SelectedTempEntity is null");
                            }
                        }
                    }
                }
            }

            inputDeps = UpdateGizmos(inputDeps, newPos, newRot);
            return inputDeps;
        }

        private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false, bool toggleSelected = false)
        {

            if(m_Mode == Mode.Default)
            {
                applyMode = ApplyMode.None;
                m_SelectedHandle = Handle.None;
                JobHandle jobHandle = SelectTempEntity(inputDeps, toggleSelected);
                if (m_SelectedEntity != Entity.Null)
                {
                    m_TransformGizmoToolUI.SetMode((int)Mode.Move);
                    m_UndoHistory.Clear();
                    m_RedoHistory.Clear();
                }
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
                if(m_SelectedEntity != Entity.Null)
                {
                    if (GetAllowApply())
                    {
                        applyMode = ApplyMode.Apply;
                        float3 pos = default;
                        quaternion rot = default;
                        if (m_SelectedTempEntity != Entity.Null && EntityManager.TryGetComponent(m_SelectedTempEntity, out Transform transform))
                        {
                            pos = transform.m_Position;
                            rot = transform.m_Rotation;
                        }

                        if (EntityManager.HasComponent<Building>(m_SelectedEntity) || EntityManager.HasComponent<AssetStamp>(m_SelectedEntity))
                        {
                            m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_RelocateBuildingSound);
                        }
                        else
                        {
                            m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlacePropSound);
                        }

                        if (m_Mode == Mode.Move)
                        {
                            m_UndoHistory.Push(ActionHistory.NewMove(m_SelectedEntity, m_DragStartGizmoPos));
                            m_RedoHistory.Clear();
                        }
                        else if (m_Mode == Mode.Rotate)
                        {
                            m_UndoHistory.Push(ActionHistory.NewRotation(m_SelectedEntity, m_DragStartGizmoRot));
                            m_RedoHistory.Clear();
                        }

                        inputDeps = UpdateObject(inputDeps, m_SelectedEntity, pos, rot);
                    }
                    else
                    {
                        m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
                    }
                }
                else
                {
                    EDT.Logger.Warn("Try to Apply but m_SelectedEntity was null");
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
                m_TransformGizmoToolUI.SetMode((int)Mode.Default);
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

        private JobHandle SelectTempEntity(JobHandle inputDeps, bool toggleSelected)
        {
            if (m_TempQuery.IsEmptyIgnoreFilter)
            {
                m_SelectedEntity = Entity.Null;
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
            if (!base.EntityManager.HasBuffer<AggregateElement>(selected.Value))
            {
                m_LastSelectedIndex = -1;
            }
            if (m_SelectedEntity != selected.Value || m_SelectedIndex != m_LastSelectedIndex)
            {
                m_SelectedEntity = selected.Value;
                m_SelectedIndex = m_LastSelectedIndex;
                PlaySelectedSound(selected.Value, forcePlay: true);
            }
            else if (toggleSelected)
            {
                m_SelectedEntity = Entity.Null;
                m_SelectedIndex = -1;
            }
            else
            {
                PlaySelectedSound(selected.Value);
            }
            selected.Dispose();
            return jobHandle;
        }

        private JobHandle UpdateDefinitions(JobHandle inputDeps, Entity entity, int index, float3 position = default, bool setPosition = false)
        {
            JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
            if (entity != Entity.Null)
            {
                JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
                {
                    m_Entity = entity,
                    m_Position = position,
                    m_SetPosition = setPosition,
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
                    AllowHighlight = AllowHightlight(entity)
                }, inputDeps);
                m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
                jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
                m_LastSelectedIndex = index;
            }
            return jobHandle;
        }

        private JobHandle UpdateGizmos(JobHandle inputDeps)
        {
            return UpdateGizmos(inputDeps, default, default);
        }

        private JobHandle UpdateGizmos(JobHandle inputDeps, float3 position)
        {
            return UpdateGizmos(inputDeps, position, default);
        }

        private JobHandle UpdateGizmos(JobHandle inputDeps, quaternion rotation)
        {
            return UpdateGizmos(inputDeps, default, rotation);
        }

        private JobHandle UpdateGizmos(JobHandle inputDeps, float3 position, quaternion rotation)
        {
            var c = m_HandleQuery.ToEntityListAsync(Allocator.TempJob, out JobHandle dep);

            Camera cam = Camera.main;
            float tanFOV = math.tan(math.radians(cam.fieldOfView) * 0.5f);

            JobHandle jobHandle = IJobExtensions.Schedule(new UpdateGizmosJob
            {
                m_Mode = m_Mode,
                m_State = m_State,
                m_SelectedEntity = m_SelectedEntity,
                m_SelectedHandle = m_SelectedHandle,
                m_Handles = c,
                m_TransformLookup = SystemAPI.GetComponentLookup<Transform>(true),
                m_InterpolatedTransformLookup = SystemAPI.GetComponentLookup<InterpolatedTransform>(true),
                m_GizmosDataLookup = SystemAPI.GetComponentLookup<GizmosData>(true),
                m_HighlightedLookup = SystemAPI.GetComponentLookup<Highlighted>(true),
                m_UseLocalAxis = m_UseLocalAxis,
                m_XZHandleMode = m_XZHandleMode,
                m_Position = position,
                m_Rotation = rotation,
                m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
                m_CameraPosition = cam.transform.position,
                m_TanFOV = tanFOV,
                m_HandleScreenSize = EDT.m_Settings.HandleScreenSize,
            }, JobHandle.CombineDependencies(inputDeps, dep));
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
            return UpdateObject(inputDeps, entity, position, rotation, m_ToolOutputBarrier);
        }

        public JobHandle UpdateObject(JobHandle inputDeps, Entity entity, float3 position, quaternion rotation, SafeCommandBufferSystem ecb)
        {
            if (entity == Entity.Null) return inputDeps;

            if (!EntityManager.HasComponent<Temp>(entity))
            {
                if (EntityManager.HasComponent<Building>(entity))
                {
                    m_TerrainSystem.OnBuildingMoved(m_SelectedEntity);

                    if(EntityManager.TryGetBuffer<InstalledUpgrade>(SelectedEntity, true, out DynamicBuffer<InstalledUpgrade> buffer))
                    {
                        foreach (var upgrade in buffer)
                        {
                            if(EntityManager.HasComponent<Building>(upgrade.m_Upgrade)) m_TerrainSystem.OnBuildingMoved(m_SelectedEntity);
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

                    if (m_AddPreventOverride && isStaticAndNotBuilding && preventOverride != default && !EntityManager.HasComponent(m_SelectedEntity, preventOverride))
                    {
                        AnarchyBridge.TryAddAnarchyComponent(entity);
                    }

                    if (transformLock != default && EntityManager.HasComponent(entity, transformLock))
                        AnarchyBridge.TryUpdateTransformLockComponent(m_SelectedEntity, newTransform);
                    else if ((m_AddTransformLock && isStaticAndNotBuilding))
                        AnarchyBridge.TryAddTransformLockComponent(m_SelectedEntity, newTransform);
                }
            }

            JobHandle jobHandle = IJobExtensions.Schedule(new UpdateObjectJob
            {
                m_SelectedEntity = entity,
                m_BuildingLookup = GetComponentLookup<Building>(true),
                m_TransformLookup = GetComponentLookup<Transform>(true),
                m_TempLookup = GetComponentLookup<Temp>(true),
                m_CurveLookup = GetComponentLookup<Curve>(true),
                m_ServiceUpgradeLookup = GetComponentLookup<ServiceUpgrade>(true),
                m_InstalledUpgradeLookup = GetBufferLookup<InstalledUpgrade>(true),
                m_NodeLookup = GetBufferLookup<Node>(true),
                m_SubAreaLookup = GetBufferLookup<SubArea>(true),
                m_SubNetLookup = GetBufferLookup<SubNet>(true),
                m_SubObjectLookup = GetBufferLookup<SubObject>(true),
                m_MoveSubBuildings = m_MoveSubBuildings,
                m_Position = position,
                m_Rotation = rotation,
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

            m_Mode = mode;
        }

        public void SnapOnGround()
        {
            m_RequestSnapOnGround = true;
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
            if (m_TempQuery.IsEmptyIgnoreFilter)
            {
                EDT.Logger.Warn("Try to drag but no temp found.");
                SetState(State.Idle);
                return;
            }

            Temp temp = default;

            NativeArray<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);

            try
            {
                var entityHandle = EntityManager.GetEntityTypeHandle();
                var tempHandle = GetComponentTypeHandle<Temp>(true);

                for (int i = 0; i < chunks.Length; i++)
                {
                    ArchetypeChunk chunk = chunks[i];

                    NativeArray<Entity> entities = chunk.GetNativeArray(entityHandle);
                    NativeArray<Temp> temps = chunk.GetNativeArray(ref tempHandle);

                    for (int j = 0; j < entities.Length; j++)
                    {
                        temp = temps[j];

                        if(temp.m_Original == m_SelectedEntity)
                        {
                            m_SelectedTempEntity = entities[j];
                            break;
                        }
                    }

                    if (m_SelectedTempEntity != Entity.Null)
                        break;
                }
            }
            finally
            {
                chunks.Dispose();
            }

            if (m_SelectedTempEntity != Entity.Null && EntityManager.TryGetComponent(m_SelectedEntity, out Transform transform) && EntityManager.TryGetComponent<TransformGizmosHandle>(raycastHit.m_HitEntity, out var component))
            {
                m_SelectedHandle = component.Handle;

                m_DragStartGizmoPos = transform.m_Position;
                m_DragStartGizmoRot = transform.m_Rotation;

                if(m_XZHandleMode != XZHandleMode.FollowSurface || m_SelectedHandle != Handle.XZ)
                {
                    float3 axisDir = GetSelectedAxisDirection(m_SelectedHandle);
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Plane dragPlane = CreateDragPlane(axisDir, m_DragStartGizmoPos);

                    if (dragPlane.Raycast(ray, out float enter))
                    {
                        m_DragStartMouseHitPos = ray.origin + ray.direction * enter;
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
                temp.m_Flags |= TempFlags.Dragging;
                entityCommandBuffer.SetComponent(m_SelectedTempEntity, temp);
                entityCommandBuffer.AddComponent(m_SelectedTempEntity, default(Updated));

                if(AnarchyBridge.IsAvailable)
                {
                    ComponentType preventOverride = AnarchyBridge.GetAnarchyComponentType();
                    if (preventOverride != default && EntityManager.HasComponent(m_SelectedEntity, preventOverride))
                    {
                        entityCommandBuffer.RemoveComponent(m_SelectedEntity, preventOverride);
                    }
                }

                entityCommandBuffer.AddComponent<Overridden>(m_SelectedEntity);
                SetState(State.Dragging);
            }
            else
            {
                EDT.Logger.Warn("Try to drag but m_SelectedTempEntity was null.");
                SetState(State.Idle);
            }
        }

        private void StopDragging()
        {
            if (m_SelectedTempEntity != Entity.Null)
            {
                EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
                Temp temp = GetComponentLookup<Temp>(true)[m_SelectedTempEntity];
                temp.m_Flags &= ~TempFlags.Dragging;
                entityCommandBuffer.SetComponent(m_SelectedTempEntity, temp);
                entityCommandBuffer.AddComponent(m_SelectedTempEntity, default(Updated));
                entityCommandBuffer.RemoveComponent<Overridden>(m_SelectedEntity);
            }
            else
            {
                EDT.Logger.Warn("Try to stop dragging but m_SelectedTempEntity was null.");
            }
            m_SelectedTempEntity = Entity.Null;
            m_SelectedHandle = Handle.None;
            SetState(State.Idle);
        }

        public Plane CreateDragPlane(float3 axisDir, float3 gizmoCenter)
        {
            if(m_Mode == Mode.Rotate || m_SelectedHandle == Handle.XZ)
                return new Plane(axisDir, gizmoCenter);

            // Camera vectors
            float3 camForward = Camera.main.transform.forward;
            float3 camRight = Camera.main.transform.right;

            // Try to build a plane perpendicular to the axis using camRight
            float3 planeNormal = math.normalize(math.cross(axisDir, camRight));

            // If axis is parallel to camRight, fallback to camForward
            if (math.lengthsq(planeNormal) < 1e-6f)
                planeNormal = math.normalize(math.cross(axisDir, camForward));

            return new Plane(planeNormal, gizmoCenter);
        }

        private float3 GetSelectedAxisDirection(Handle handle)
        {
            quaternion rot;
            if (m_UseLocalAxis && EntityManager.TryGetComponent(m_SelectedEntity, out Transform transform))
            {
                rot = transform.m_Rotation;
            }
            else
            {
                rot = quaternion.identity;
            }

            if (handle == Handle.X)
                return m_UseLocalAxis ? math.rotate(rot, new float3(1, 0, 0)) : new float3(1, 0, 0);

            if (handle == Handle.Y)
                return m_UseLocalAxis ? math.rotate(rot, new float3(0, 1, 0)) : new float3(0, 1, 0);

            if (handle == Handle.Z)
                return m_UseLocalAxis ? math.rotate(rot, new float3(0, 0, 1)) : new float3(0, 0, 1);

            if (handle == Handle.XZ)
            {
                switch(m_XZHandleMode)
                {
                    case XZHandleMode.FollowSurface:
                    case XZHandleMode.FixedY:
                        return new float3(0, 1, 0);

                    case XZHandleMode.FixedX:
                        return m_UseLocalAxis ? math.rotate(rot, new float3(1, 0, 0)) : new float3(1, 0, 0);

                    case XZHandleMode.FixedZ:
                        return m_UseLocalAxis ? math.rotate(rot, new float3(0, 0, 1)) : new float3(0, 0, 1);
                }
            }
                
            return float3.zero;
        }
    
        private bool AllowHightlight(Entity entity)
        {
            if (m_Mode == Mode.Default) return true;

            if (m_AllowHightlight == AllowHightlightState.Enabled) return true;
            if (m_AllowHightlight == AllowHightlightState.Disabled) return false;

            if (EntityManager.HasComponent<Building>(entity)
                || EntityManager.HasBuffer<SubObject>(entity)
            ) 
                return true;

            return false;
        }
    }
}
