using Colossal;
using Colossal.Entities;
using Colossal.Mathematics;
using ExtraDetailingTools.Gizmos;
using ExtraDetailingTools.Systems.UI;
using Game;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
using Game.UI.Debug;
using Game.Vehicles;
using PDX.SDK.Contracts.Enums.Errors;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
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
    internal partial class TransformGizmoTool : ToolBaseSystem
    {

        public enum AllowHightlightState
        {
            Default,
            Enabled,
            Disabled,
        }

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
            [ReadOnly] public Entity m_GizmosEntity;
            [ReadOnly] public ComponentLookup<Transform> m_TransformLookup;
            [ReadOnly] public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformLookup;
            [ReadOnly] public ComponentLookup<GizmosData> m_GizmosDataLookup;
            [ReadOnly] public ComponentLookup<Highlighted> m_HighlightedLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public ComponentLookup<ObjectGeometryData> m_ObjectGeometryDataLookup;

            [ReadOnly] public bool m_UseLocalAxis;
            [ReadOnly] public float3 m_Position;
            [ReadOnly] public quaternion m_Rotation;

            [ReadOnly] public EntityCommandBuffer m_CommandBuffer;
            [ReadOnly] public GizmoBatcher m_Batcher;

            [ReadOnly] public Entity m_xAxisEntity;
            [ReadOnly] public Entity m_yAxisEntity;
            [ReadOnly] public Entity m_zAxisEntity;

            public void Execute()
            {

                if (m_Mode == Mode.Default)
                {
                    DestroyAllEntity();
                    return;
                }

                Transform transform;
                if (m_InterpolatedTransformLookup.TryGetComponent(m_SelectedEntity, out InterpolatedTransform interpolatedTransform))
                {
                    transform = interpolatedTransform.ToTransform();
                }
                else if (!m_TransformLookup.TryGetComponent(m_SelectedEntity, out transform))
                {
                    return;
                }

                float3 pos =     !m_Position.Equals(default) ? m_Position : transform.m_Position;
                quaternion rot = !m_Rotation.Equals(default) ? m_Rotation : transform.m_Rotation;

                Bounds3 bounds3 = new(new(0, 0, 0), new(10, 10, 10));

                if (m_PrefabRefLookup.TryGetComponent(m_SelectedEntity, out PrefabRef prefabRef) && m_ObjectGeometryDataLookup.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData))
                {
                    bounds3 = m_UseLocalAxis ? geometryData.m_Bounds : ObjectUtils.CalculateBounds(pos, rot, geometryData);
                }

                //float3 center = MathUtils.Center(bounds3);
                float3 size = new(bounds3.x.max - bounds3.x.min, bounds3.y.max - bounds3.y.min, bounds3.z.max - bounds3.z.min);

                RenderOriginPoint(transform.m_Position, size);

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
                        DestroyAllEntity();
                        break;
                }
            }

            private void RenderOriginPoint(Vector3 pos, float3 size)
            {
                float radius = math.cmin(size) * 0.1f;
                m_Batcher.DrawWireSphere(pos, radius, Color.yellow);
            }

            private void UpdateMoveHandle(float3 pos, quaternion rot, float3 size)
            {

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

                UpdateMoveArrow(m_xAxisEntity, pos, pos + xAxis, Color.red);
                UpdateMoveArrow(m_yAxisEntity, pos, pos + yAxis, Color.green);
                UpdateMoveArrow(m_zAxisEntity, pos, pos + zAxis, Color.blue);
            }

            private void UpdateMoveArrow(Entity gizmos, float3 A, float3 B, Color color)
            {
                GizmosData data = GizmosUtils.DrawArrow(A, B, color);
                m_CommandBuffer.AddComponent(gizmos, data);

                if (gizmos == m_GizmosEntity)
                {
                    if(!m_HighlightedLookup.HasComponent(gizmos)) m_CommandBuffer.AddComponent<Highlighted>(gizmos);
                }
                else if (m_HighlightedLookup.HasComponent(gizmos))
                {
                    m_CommandBuffer.RemoveComponent<Highlighted>(gizmos);
                }
            }

            private void UpdateRotateHandle(float3 pos, quaternion rot, float3 size)
            {
                float radius = math.cmax(size) / 2;

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

                UpdateRotateArc(m_xAxisEntity, pos, xAxis, fromX, radius, Color.red);
                UpdateRotateArc(m_yAxisEntity, pos, yAxis, fromY, radius, Color.green);
                UpdateRotateArc(m_zAxisEntity, pos, zAxis, fromZ, radius, Color.blue);
            }

            private void UpdateRotateArc(Entity gizmos, float3 center, float3 normal, float3 from, float radius, Color color)
            {
                GizmosData data = GizmosUtils.DrawWireArc(center, normal, from, 360, radius, color);
                m_CommandBuffer.AddComponent(gizmos, data);

                if (gizmos == m_GizmosEntity)
                {
                    if (!m_HighlightedLookup.HasComponent(gizmos))
                        m_CommandBuffer.AddComponent<Highlighted>(gizmos);
                }
                else if (m_HighlightedLookup.HasComponent(gizmos))
                {
                    m_CommandBuffer.RemoveComponent<Highlighted>(gizmos);
                }
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



            private void DestroyAllEntity()
            {
                m_CommandBuffer.RemoveComponent<GizmosData>(m_xAxisEntity);
                m_CommandBuffer.RemoveComponent<GizmosData>(m_yAxisEntity);
                m_CommandBuffer.RemoveComponent<GizmosData>(m_zAxisEntity);
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

        public enum Mode
        {
            Default = 0,
            Move,
            Rotate,
            Scale
        }

        public enum State
        {
            Idle,
            Dragging
        }

        public override string toolID => "TransformGizmoTool";

        private AudioManager m_AudioManager;
        private GizmosSystem m_GizmosSystem;
        private GizmosRaycastSystem m_GimzosRaycastSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private TransformGizmoToolUI m_TransformGizmoToolUI;

        private Mode m_Mode = Mode.Default;

        private State m_State = State.Idle;

        private EntityQuery m_SoundQuery;
        private EntityQuery m_DefinitionQuery;
        private EntityQuery m_TempQuery;

        private Entity m_LastRaycastEntity;
        private Entity m_LastRaycastGizmos;
        private Entity m_SelectedEntity;
        private Entity m_SelectedTempEntity;

        private Entity m_xAxisEntity;
        private Entity m_yAxisEntity;
        private Entity m_zAxisEntity;

        private int m_LastSelectedIndex;
        private int m_SelectedIndex;
        private float3 m_DragStartGizmoPos;
        private quaternion m_DragStartGizmoRot;
        private float3 m_DragStartMouseHitPos;

        public bool m_UseLocalAxis { get; set; } = true;
        public bool m_MoveSubBuildings { get; set; } = true;
        public bool m_Underground { get; private set; } = false;
        public AllowHightlightState m_AllowHightlight { get; set; } = AllowHightlightState.Default;

        public override int uiModeIndex => (int)m_Mode;
        public override bool allowUnderground { get => true; protected set { } }

        public Mode mode => m_Mode;

        public Entity SelectedTempEntity => m_SelectedTempEntity;

        public Entity SelectedEntity => m_SelectedEntity;

        public override PrefabBase GetPrefab() { return null; }

        public override bool TrySetPrefab(PrefabBase prefab) { return false; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
            m_TransformGizmoToolUI = World.GetOrCreateSystemManaged<TransformGizmoToolUI>();
            m_GizmosSystem = World.GetOrCreateSystemManaged<GizmosSystem>();
            m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
            m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
            m_GimzosRaycastSystem = World.GetOrCreateSystemManaged<GizmosRaycastSystem>();
            m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
            m_DefinitionQuery = GetDefinitionQuery();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_xAxisEntity = EntityManager.CreateEntity();
            m_yAxisEntity = EntityManager.CreateEntity();
            m_zAxisEntity = EntityManager.CreateEntity();
            m_SelectedEntity = m_ToolSystem.selected;
            EnableActions(true);
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            EntityManager.DestroyEntity(m_xAxisEntity);
            EntityManager.DestroyEntity(m_yAxisEntity);
            EntityManager.DestroyEntity(m_zAxisEntity);
            if(m_ToolSystem.selected != Entity.Null)
            {
                m_ToolSystem.selected = m_SelectedEntity;
            }
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
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Labels | TypeMask.Icons;
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.OutsideConnections | RaycastFlags.Decals | RaycastFlags.BuildingLots;
                m_ToolRaycastSystem.netLayerMask = Layer.None;
                m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
                m_ToolRaycastSystem.iconLayerMask = IconLayerMask.Default;
                if (!m_Underground)
                {
                    m_ToolRaycastSystem.typeMask |= TypeMask.Areas;
                    m_ToolRaycastSystem.areaTypeMask |= AreaTypeMask.Lots;
                }
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.Markers | RaycastFlags.UpgradeIsMain | RaycastFlags.EditorContainers;
                }
                else
                {
                    m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings;
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
            //m_Mode = Mode.Default;
            return inputDeps;
        }

        private JobHandle Update(JobHandle inputDeps)
        {
            float3 newPos = default;
            quaternion newRot = default;

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
                        m_LastRaycastGizmos = raycastResult.m_Hit.m_HitEntity;
                    }

                    applyMode = ApplyMode.Clear;
                    inputDeps = UpdateDefinitions(inputDeps, m_SelectedEntity, m_SelectedIndex);
                }
                else if (m_State == State.Dragging)
                {
                    if (m_Mode == Mode.Move)
                    {
                        applyMode = ApplyMode.None;
                        float3 axisDir = GetSelectedAxisDirection(m_LastRaycastGizmos);
                        Plane dragPlane = CreateDragPlane(axisDir, m_DragStartGizmoPos);

                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (dragPlane.Raycast(ray, out float enter))
                        {
                            float3 hitPos = ray.origin + ray.direction * enter;
                            float3 mouseDelta = hitPos - m_DragStartMouseHitPos;
                            float3 projectedDelta = math.dot(mouseDelta, axisDir) * axisDir;

                            newPos = projectedDelta + m_DragStartGizmoPos;

                            if (m_SelectedTempEntity != Entity.Null)
                            {
                                inputDeps = UpdateObject(inputDeps, m_SelectedTempEntity, newPos);
                            }
                            else
                            {
                                EDT.Logger.Error("m_SelectedTempEntity is null");
                            }
                        }
                    }

                    else if (m_Mode == Mode.Rotate)
                    {
                        applyMode = ApplyMode.None;

                        float3 axisDir = GetSelectedAxisDirection(m_LastRaycastGizmos);
                        Plane dragPlane = CreateDragPlane(axisDir, m_DragStartGizmoPos);

                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                        if (dragPlane.Raycast(ray, out float enter))
                        {
                            float3 hitPos = ray.origin + ray.direction * enter;
                            float3 currentDir = math.normalize(hitPos - m_DragStartGizmoPos);
                            float3 startDir = m_DragStartMouseHitPos;

                            float angle = math.atan2(
                                math.dot(axisDir, math.cross(startDir, currentDir)),
                                math.dot(startDir, currentDir)
                            );

                            quaternion deltaRot = quaternion.AxisAngle(axisDir, angle);

                            newRot = math.mul(deltaRot, m_DragStartGizmoRot);

                            if (m_SelectedTempEntity != Entity.Null)
                            {
                                inputDeps = UpdateObject(inputDeps, m_SelectedTempEntity, newRot);
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
                m_LastRaycastGizmos = Entity.Null;
                JobHandle jobHandle = SelectTempEntity(inputDeps, toggleSelected);
                if (m_SelectedEntity != Entity.Null)
                {
                    m_TransformGizmoToolUI.SetMode((int)Mode.Move);
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
                float3 pos = default;
                quaternion rot = default;
                if (m_SelectedTempEntity != Entity.Null && EntityManager.TryGetComponent(m_SelectedTempEntity, out Transform transform))
                {
                    pos = transform.m_Position;
                    rot = transform.m_Rotation;
                }
                StopDragging();
                inputDeps = UpdateObject(inputDeps, m_SelectedEntity, pos, rot);
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
                //m_Mode = Mode.Default;
                m_TransformGizmoToolUI.SetMode((int)Mode.Default);
                return UpdateGizmos(inputDeps);
            }

            if (m_State == State.Dragging)
            {
                StopDragging();
                //base.applyMode = ApplyMode.Clear;
                inputDeps = UpdateGizmos(inputDeps);
                return inputDeps; //UpdateDefinitions(inputDeps, m_ToolSystem.selected, m_LastSelectedIndex);
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

            JobHandle jobHandle = IJobExtensions.Schedule(new UpdateGizmosJob
            {
                m_Mode = m_Mode,
                m_State = m_State,
                m_SelectedEntity = m_SelectedEntity,
                m_GizmosEntity = m_LastRaycastGizmos,
                m_xAxisEntity = m_xAxisEntity,
                m_yAxisEntity = m_yAxisEntity,
                m_zAxisEntity = m_zAxisEntity,
                m_TransformLookup = SystemAPI.GetComponentLookup<Transform>(true),
                m_InterpolatedTransformLookup = SystemAPI.GetComponentLookup<InterpolatedTransform>(true),
                m_GizmosDataLookup = SystemAPI.GetComponentLookup<GizmosData>(true),
                m_HighlightedLookup = SystemAPI.GetComponentLookup<Highlighted>(true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                m_ObjectGeometryDataLookup = SystemAPI.GetComponentLookup<ObjectGeometryData>(true),
                m_UseLocalAxis = m_UseLocalAxis,
                m_Position = position,
                m_Rotation = rotation,
                m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                m_Batcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle dep)
            }, JobHandle.CombineDependencies(inputDeps, dep));
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
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
            if (entity == Entity.Null) return inputDeps;
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
                m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
            }, inputDeps);
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }

        public void SetMode(Mode mode)
        {
            if(m_State == State.Dragging)
            {
                StopDragging();
            }

            SetState(State.Idle);
            m_Mode = mode;
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

            if (m_SelectedTempEntity != Entity.Null && EntityManager.TryGetComponent(m_SelectedEntity, out Transform transform))
            {
                m_LastRaycastGizmos = raycastHit.m_HitEntity;

                m_DragStartGizmoPos = transform.m_Position;
                m_DragStartGizmoRot = transform.m_Rotation;

                float3 axisDir = GetSelectedAxisDirection(m_LastRaycastGizmos);
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

                EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
                temp.m_Flags |= TempFlags.Dragging;
                entityCommandBuffer.SetComponent(m_SelectedTempEntity, temp);
                entityCommandBuffer.AddComponent(m_SelectedTempEntity, default(Updated));
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
            }
            m_SelectedTempEntity = Entity.Null;
            m_LastRaycastGizmos = Entity.Null;
            SetState(State.Idle);
        }

        public Plane CreateDragPlane(float3 axisDir, float3 gizmoCenter)
        {
            if(m_Mode == Mode.Rotate)
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

        private float3 GetSelectedAxisDirection(Entity entity)
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

            if (entity == m_xAxisEntity)
                return m_UseLocalAxis ? math.rotate(rot, new float3(1, 0, 0)) : new float3(1, 0, 0);

            if (entity == m_yAxisEntity)
                return m_UseLocalAxis ? math.rotate(rot, new float3(0, 1, 0)) : new float3(0, 1, 0);

            if (entity == m_zAxisEntity)
                return m_UseLocalAxis ? math.rotate(rot, new float3(0, 0, 1)) : new float3(0, 0, 1);
            

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
