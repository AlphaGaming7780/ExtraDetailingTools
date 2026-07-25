using Colossal.Entities;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems
{
    public partial class DuplicateEntitySystem : GameSystemBase
    {
        private struct PendingDuplicate
        {
            public Entity Source;
            public Entity Prefab;
            public float3 Position;
        }

        private struct Candidate
        {
            public Entity Entity;
            public float3 Position;
        }

        private DuplicateEntityBarrier m_Barrier;
        private EntityQuery m_CreatedQuery;

        private NativeList<PendingDuplicate> m_Pending;
        private readonly List<object> m_PendingContext = new List<object>();

        private readonly Dictionary<object, int> m_ExpectedCount = new Dictionary<object, int>();
        private readonly Dictionary<object, List<Entity>> m_DuplicatedEntities = new Dictionary<object, List<Entity>>();

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Barrier = World.GetOrCreateSystemManaged<DuplicateEntityBarrier>();
            m_Pending = new NativeList<PendingDuplicate>(8, Allocator.Persistent);
            m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Transform>(), ComponentType.Exclude<Temp>());
        }

        protected override void OnDestroy()
        {
            Dependency.Complete();
            m_Pending.Dispose();
            base.OnDestroy();
        }

        public JobHandle Duplicate(JobHandle inputDeps, object context, Entity entity)
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(1, Allocator.Temp);
            entities[0] = entity;
            JobHandle handle = Duplicate(inputDeps, context, entities);
            handle = entities.Dispose(handle);
            return handle;
        }

        public JobHandle Duplicate(JobHandle inputDeps, object context, NativeArray<Entity> entities)
        {
            JobHandle deps = JobHandle.CombineDependencies(inputDeps, Dependency);

            ComponentLookup<Transform> transformData = SystemAPI.GetComponentLookup<Transform>(true);
            ComponentLookup<PrefabRef> prefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true);

            NativeList<Entity> validEntities = new NativeList<Entity>(entities.Length, Allocator.TempJob);
            int expected = m_ExpectedCount.TryGetValue(context, out int existingExpected) ? existingExpected : 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!transformData.TryGetComponent(entity, out Transform transform) || !prefabRefData.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    EDT.Logger.Warn($"DuplicateEntitySystem: entity {entity} is missing Transform or PrefabRef, skipping.");
                    continue;
                }

                validEntities.Add(entity);
                m_PendingContext.Add(context);
                m_Pending.Add(new PendingDuplicate
                {
                    Source = entity,
                    Prefab = prefabRef.m_Prefab,
                    Position = transform.m_Position
                });
                expected++;
            }
            m_ExpectedCount[context] = expected;

            if (validEntities.Length > 0)
            {
                JobHandle jobHandle = new DuplicateEntityJob
                {
                    m_Entities = validEntities.AsArray(),
                    m_TransformData = transformData,
                    m_PrefabRefData = prefabRefData,
                    m_ElevationData = SystemAPI.GetComponentLookup<Game.Objects.Elevation>(true),
                    m_LocalTransformCacheData = SystemAPI.GetComponentLookup<LocalTransformCache>(true),
                    m_EditorContainerData = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(true),
                    m_SubAreaData = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true),
                    m_AreaNodeData = SystemAPI.GetBufferLookup<Node>(true),
                    m_SubNetData = SystemAPI.GetBufferLookup<Game.Net.SubNet>(true),
                    m_EdgeData = SystemAPI.GetComponentLookup<Game.Net.Edge>(true),
                    m_CurveData = SystemAPI.GetComponentLookup<Game.Net.Curve>(true),
                    m_NetNodeData = SystemAPI.GetComponentLookup<Game.Net.Node>(true),
                    m_InstalledUpgradeData = SystemAPI.GetBufferLookup<InstalledUpgrade>(true),
                    m_CommandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
                }.Schedule(validEntities.Length, 1, deps);

                m_Barrier.AddJobHandleForProducer(jobHandle);
                deps = validEntities.Dispose(jobHandle);
            }
            else
            {
                EDT.Logger.Warn($"DuplicateEntitySystem: no valid entities to duplicate for context {context}, skipping.");
                validEntities.Dispose();
            }

            Dependency = deps;
            return deps;
        }

        protected override void OnUpdate()
        {
            if (m_Pending.Length == 0)
                return;

            NativeParallelMultiHashMap<Entity, Candidate> candidatesByPrefab = new NativeParallelMultiHashMap<Entity, Candidate>(m_CreatedQuery.CalculateEntityCount(), Allocator.TempJob);
            JobHandle buildMapHandle = new BuildCandidateMapJob
            {
                m_EntityHandle = GetEntityTypeHandle(),
                m_PrefabRefHandle = GetComponentTypeHandle<PrefabRef>(true),
                m_TransformHandle = GetComponentTypeHandle<Transform>(true),
                m_Candidates = candidatesByPrefab.AsParallelWriter(),
            }.ScheduleParallel(m_CreatedQuery, Dependency);

            NativeArray<Entity> results = new NativeArray<Entity>(m_Pending.Length, Allocator.TempJob);
            JobHandle findHandle = new FindDuplicatesJob
            {
                m_Pending = m_Pending.AsDeferredJobArray(),
                m_Candidates = candidatesByPrefab,
                m_Results = results,
            }.Schedule(m_Pending.Length, 8, buildMapHandle);

            findHandle.Complete();
            candidatesByPrefab.Dispose();

            for (int i = m_Pending.Length - 1; i >= 0; i--)
            {
                Entity result = results[i];
                if (result == Entity.Null)
                    continue;

                object context = m_PendingContext[i];
                if (!m_DuplicatedEntities.TryGetValue(context, out List<Entity> list))
                {
                    list = new List<Entity>();
                    m_DuplicatedEntities[context] = list;
                }
                list.Add(result);

                m_Pending.RemoveAtSwapBack(i);
                m_PendingContext[i] = m_PendingContext[m_PendingContext.Count - 1];
                m_PendingContext.RemoveAt(m_PendingContext.Count - 1);
            }
            results.Dispose();
        }

        // True once every entity requested for this context has been located. Consumes the result: a
        // second call for the same context (with nothing new pending) returns false.
        public bool TryGetDuplicated(object context, out List<Entity> result)
        {
            result = null;
            if (!m_ExpectedCount.TryGetValue(context, out int expected))
                return false;

            if (!m_DuplicatedEntities.TryGetValue(context, out List<Entity> list) || list.Count < expected)
                return false;

            result = list;
            m_DuplicatedEntities.Remove(context);
            m_ExpectedCount.Remove(context);
            return true;
        }

        [BurstCompile]
        private struct BuildCandidateMapJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
            [ReadOnly] public ComponentTypeHandle<Transform> m_TransformHandle;
            public NativeParallelMultiHashMap<Entity, Candidate>.ParallelWriter m_Candidates;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                NativeArray<Transform> transforms = chunk.GetNativeArray(ref m_TransformHandle);
                for (int i = 0; i < chunk.Count; i++)
                {
                    m_Candidates.Add(prefabs[i].m_Prefab, new Candidate { Entity = entities[i], Position = transforms[i].m_Position });
                }
            }
        }

        [BurstCompile]
        private struct FindDuplicatesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PendingDuplicate> m_Pending;
            [ReadOnly] public NativeParallelMultiHashMap<Entity, Candidate> m_Candidates;
            [WriteOnly] public NativeArray<Entity> m_Results;

            public void Execute(int index)
            {
                PendingDuplicate pending = m_Pending[index];
                Entity result = Entity.Null;

                if (m_Candidates.TryGetFirstValue(pending.Prefab, out Candidate candidate, out NativeParallelMultiHashMapIterator<Entity> it))
                {
                    do
                    {
                        if (candidate.Entity != pending.Source && math.distancesq(candidate.Position, pending.Position) < 0.0001f)
                        {
                            result = candidate.Entity;
                            break;
                        }
                    }
                    while (m_Candidates.TryGetNextValue(out candidate, ref it));
                }

                m_Results[index] = result;
            }
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct DuplicateEntityJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> m_Entities;

            [ReadOnly] public ComponentLookup<Transform> m_TransformData;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly] public ComponentLookup<Game.Objects.Elevation> m_ElevationData;
            [ReadOnly] public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;
            [ReadOnly] public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;
            [ReadOnly] public BufferLookup<Game.Areas.SubArea> m_SubAreaData;
            [ReadOnly] public BufferLookup<Node> m_AreaNodeData;
            [ReadOnly] public BufferLookup<Game.Net.SubNet> m_SubNetData;
            [ReadOnly] public ComponentLookup<Game.Net.Edge> m_EdgeData;
            [ReadOnly] public ComponentLookup<Game.Net.Curve> m_CurveData;
            [ReadOnly] public ComponentLookup<Game.Net.Node> m_NetNodeData;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgradeData;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(int index)
            {
                AddObject(m_Entities[index], default, index);
            }

            // Duplicates a single object (main entity or an installed upgrade), plus its sub-areas,
            // sub-nets and installed upgrades. ownerDefinition links it back to its parent, if any.
            private void AddObject(Entity source, OwnerDefinition ownerDefinition, int sortKey)
            {
                if (!m_TransformData.TryGetComponent(source, out Transform transform)
                    || !m_PrefabRefData.TryGetComponent(source, out PrefabRef prefabRef))
                    return;

                Entity e = m_CommandBuffer.CreateEntity(sortKey);

                CreationDefinition creationDefinition = new CreationDefinition
                {
                    m_Prefab = prefabRef.m_Prefab,
                    m_Flags = CreationFlags.Permanent,
                    // Adding this spawn nothing
                    //m_Original = source
                };

                m_CommandBuffer.AddComponent(sortKey, e, default(Updated));
                if (ownerDefinition.m_Prefab != Entity.Null)
                {
                    m_CommandBuffer.AddComponent(sortKey, e, ownerDefinition);
                }

                ObjectDefinition objectDefinition = new ObjectDefinition
                {
                    m_Position = transform.m_Position,
                    m_Rotation = transform.m_Rotation,
                    m_Probability = 100,
                    m_PrefabSubIndex = -1
                };

                if (m_ElevationData.TryGetComponent(source, out var elevation))
                {
                    objectDefinition.m_Elevation = elevation.m_Elevation;
                    objectDefinition.m_ParentMesh = ObjectUtils.GetSubParentMesh(elevation.m_Flags);
                    if ((elevation.m_Flags & ElevationFlags.Lowered) != 0)
                    {
                        creationDefinition.m_Flags |= CreationFlags.Lowered;
                    }
                }
                else
                {
                    objectDefinition.m_ParentMesh = -1;
                }

                if (m_LocalTransformCacheData.TryGetComponent(source, out var localTransformCache))
                {
                    objectDefinition.m_LocalPosition = localTransformCache.m_Position;
                    objectDefinition.m_LocalRotation = localTransformCache.m_Rotation;
                    objectDefinition.m_ParentMesh = localTransformCache.m_ParentMesh;
                    objectDefinition.m_GroupIndex = localTransformCache.m_GroupIndex;
                    objectDefinition.m_Probability = localTransformCache.m_Probability;
                    objectDefinition.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
                }
                else if (ownerDefinition.m_Prefab != Entity.Null)
                {
                    Transform local = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
                    objectDefinition.m_LocalPosition = local.m_Position;
                    objectDefinition.m_LocalRotation = local.m_Rotation;
                }
                else
                {
                    objectDefinition.m_LocalPosition = transform.m_Position;
                    objectDefinition.m_LocalRotation = transform.m_Rotation;
                }

                if (m_EditorContainerData.TryGetComponent(source, out var editorContainer))
                {
                    creationDefinition.m_SubPrefab = editorContainer.m_Prefab;
                    objectDefinition.m_Scale = editorContainer.m_Scale;
                    objectDefinition.m_Intensity = editorContainer.m_Intensity;
                    objectDefinition.m_GroupIndex = editorContainer.m_GroupIndex;
                }

                m_CommandBuffer.AddComponent(sortKey, e, objectDefinition);
                m_CommandBuffer.AddComponent(sortKey, e, creationDefinition);

                OwnerDefinition ownOwnerDefinition = new OwnerDefinition
                {
                    m_Prefab = prefabRef.m_Prefab,
                    m_Position = transform.m_Position,
                    m_Rotation = transform.m_Rotation
                };

                AddSubAreas(source, ownOwnerDefinition, sortKey);
                AddSubNets(source, ownOwnerDefinition, sortKey);

                if (m_InstalledUpgradeData.TryGetBuffer(source, out var upgrades))
                {
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        Entity upgrade = upgrades[i].m_Upgrade;
                        if (upgrade != source)
                        {
                            AddObject(upgrade, ownOwnerDefinition, sortKey);
                        }
                    }
                }
            }

            // No Lot filter here: DefaultToolSystem's own Ctrl+D only restricts to Lot-tagged areas when
            // duplicating a service-upgrade parent (to avoid re-duplicating each sub-building's own lot);
            // for a single, non-parent entity every sub-area should come along.
            private void AddSubAreas(Entity source, OwnerDefinition ownerDefinition, int sortKey)
            {
                if (!m_SubAreaData.TryGetBuffer(source, out var subAreas))
                    return;

                for (int i = 0; i < subAreas.Length; i++)
                {
                    Entity area = subAreas[i].m_Area;
                    if (!m_PrefabRefData.TryGetComponent(area, out PrefabRef areaPrefabRef)
                        || !m_AreaNodeData.TryGetBuffer(area, out var areaNodes))
                        continue;

                    Entity areaEntity = m_CommandBuffer.CreateEntity(sortKey);
                    m_CommandBuffer.AddComponent(sortKey, areaEntity, default(Updated));
                    m_CommandBuffer.AddComponent(sortKey, areaEntity, ownerDefinition);

                    DynamicBuffer<Node> newAreaNodes = m_CommandBuffer.AddBuffer<Node>(sortKey, areaEntity);
                    newAreaNodes.ResizeUninitialized(areaNodes.Length);
                    newAreaNodes.CopyFrom(areaNodes.AsNativeArray());

                    m_CommandBuffer.AddComponent(sortKey, areaEntity, new CreationDefinition
                    {
                        m_Prefab = areaPrefabRef.m_Prefab,
                        m_Flags = CreationFlags.Permanent
                    });
                }
            }

            private void AddSubNets(Entity source, OwnerDefinition ownerDefinition, int sortKey)
            {
                if (!m_SubNetData.TryGetBuffer(source, out var subNets))
                    return;

                for (int i = 0; i < subNets.Length; i++)
                {
                    Entity net = subNets[i].m_SubNet;
                    if (!m_PrefabRefData.TryGetComponent(net, out PrefabRef netPrefabRef))
                        continue;

                    NetCourse course;
                    if (m_EdgeData.HasComponent(net) && m_CurveData.TryGetComponent(net, out Game.Net.Curve curve))
                    {
                        Bezier4x3 bezier = curve.m_Bezier;
                        course = default;
                        course.m_Curve = bezier;
                        course.m_Length = MathUtils.Length(course.m_Curve);
                        course.m_FixedIndex = -1;
                        course.m_StartPosition.m_Position = bezier.a;
                        course.m_StartPosition.m_Rotation = Game.Net.NetUtils.GetNodeRotation(MathUtils.StartTangent(bezier));
                        course.m_EndPosition.m_Position = bezier.d;
                        course.m_EndPosition.m_Rotation = Game.Net.NetUtils.GetNodeRotation(MathUtils.EndTangent(bezier));
                        course.m_EndPosition.m_CourseDelta = 1f;
                    }
                    else if (m_NetNodeData.TryGetComponent(net, out Game.Net.Node netNode))
                    {
                        course = new NetCourse
                        {
                            m_Curve = new Bezier4x3(netNode.m_Position, netNode.m_Position, netNode.m_Position, netNode.m_Position),
                            m_Length = 0f,
                            m_FixedIndex = -1,
                            m_StartPosition = { m_Position = netNode.m_Position, m_Rotation = netNode.m_Rotation, m_CourseDelta = 0f },
                            m_EndPosition = { m_Position = netNode.m_Position, m_Rotation = netNode.m_Rotation, m_CourseDelta = 1f }
                        };
                    }
                    else
                    {
                        continue;
                    }

                    Entity netEntity = m_CommandBuffer.CreateEntity(sortKey);
                    m_CommandBuffer.AddComponent(sortKey, netEntity, default(Updated));
                    m_CommandBuffer.AddComponent(sortKey, netEntity, ownerDefinition);
                    m_CommandBuffer.AddComponent(sortKey, netEntity, course);
                    m_CommandBuffer.AddComponent(sortKey, netEntity, new CreationDefinition
                    {
                        m_Prefab = netPrefabRef.m_Prefab,
                        m_Flags = CreationFlags.Permanent
                    });
                }
            }
        }
    }
}
