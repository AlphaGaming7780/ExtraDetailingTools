using ExtraDetailingTools.ComponentsData;
using Game;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace ExtraDetailingTools.Systems
{
    internal partial class EditTempEntitiesSystem : GameSystemBase
    {
        private ModificationEndBarrier _modificationBarrier;
        private EntityQuery _updateQueryTempEntities;
        protected override void OnCreate()
        {
            base.OnCreate();
            _modificationBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            _updateQueryTempEntities = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<MeshBatch>(), ComponentType.ReadOnly<Temp>() },
                    Any = new ComponentType[] { /*ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<BatchesUpdated>(), ComponentType.ReadOnly<UpdateFrame>()*/ },
                    None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() },
                }
            );
            RequireAnyForUpdate(_updateQueryTempEntities);
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = _modificationBarrier.CreateCommandBuffer();
            AddTransformObjectToTempObject addTransformObjectToTempObject = new()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                TransformObjectLookup = SystemAPI.GetComponentLookup<TransformObject>(),
                TempLookup = SystemAPI.GetComponentLookup<Temp>(true),
                EntityCommandBuffer = entityCommandBuffer
            };
            JobHandle addTransformObjectToTempObjectJob = addTransformObjectToTempObject.Schedule(_updateQueryTempEntities, Dependency);
            _modificationBarrier.AddJobHandleForProducer(addTransformObjectToTempObjectJob);
            Dependency = addTransformObjectToTempObjectJob;
        }

#if RELEASE
	[BurstCompile]
#endif
        private struct AddTransformObjectToTempObject : IJobChunk
        {
            public EntityTypeHandle EntityTypeHandle;
            public ComponentLookup<TransformObject> TransformObjectLookup;
            public ComponentLookup<Temp> TempLookup;
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                foreach (Entity entity in entities)
                {
                    Temp temp = TempLookup[entity];
                    if (TransformObjectLookup.TryGetComponent(temp.m_Original, out TransformObject transformObject))
                    {
                        if(TransformObjectLookup.HasComponent(entity)) EntityCommandBuffer.SetComponent(entity, transformObject);
                        else EntityCommandBuffer.AddComponent(entity, transformObject);
                    }
                }
            }
        }
    }
}
