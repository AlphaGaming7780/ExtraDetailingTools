using Colossal.Rendering;
using ExtraDetailingTools.ComponentsData;
using Game;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ExtraDetailingTools.Systems
{
	internal partial class TransformObjectSystem : GameSystemBase
	{
		private BatchManagerSystem _batchManagerSystem;
		private PreCullingSystem _preCullingSystem;
		private EntityQuery _updateQuery;
		
        protected override void OnCreate()
		{
			base.OnCreate();
			EDT.Logger.Info("EDT_BatchDataSystem Created");
			_batchManagerSystem = World.GetOrCreateSystemManaged<BatchManagerSystem>();
			_preCullingSystem = World.GetOrCreateSystemManaged<PreCullingSystem>();
			_updateQuery = GetEntityQuery(
				new EntityQueryDesc
				{
					All = new ComponentType[] { ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<TransformObject>(), ComponentType.ReadOnly<MeshBatch>() },
					Any = new ComponentType[] { ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<BatchesUpdated>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<Temp>() },
					None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() },
				}
			);


            RequireAnyForUpdate(  _updateQuery );
        }

		protected override void OnUpdate()
		{   // V1
			//NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = _batchManagerSystem.GetNativeBatchGroups(true, out JobHandle job);
			//NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = _batchManagerSystem.GetNativeBatchInstances(false, out JobHandle jb);
			//jb.Complete();

			//ScalMeshJob_ParallelForDefer scalMeshJob = new()
			//{
			//	CullingDatas = _preCullingSystem.GetCullingData(true, out JobHandle job2),
			//	MeshBatcheLookup = SystemAPI.GetBufferLookup<MeshBatch>(),
			//	TransformLookup = SystemAPI.GetComponentLookup<Transform>(),
			//	InterpolatedTransformLookup = SystemAPI.GetComponentLookup<InterpolatedTransform>(),
			//	CullingInfoLookup = SystemAPI.GetComponentLookup<CullingInfo>(),
			//	TransformObjectLookup = SystemAPI.GetComponentLookup<TransformObject>(),
			//	HiddenLookup = SystemAPI.GetComponentLookup<Hidden>(isReadOnly: true),
			//	NativeBatchInstances = nativeBatchInstances.AsParallelInstanceWriter(),
			//	NativeBatchGroups = nativeBatchGroups,
			//};
			//JobHandle jobHandle = scalMeshJob.Schedule(scalMeshJob.CullingDatas, 16, JobHandle.CombineDependencies(Dependency, job2, job));
			//_preCullingSystem.AddCullingDataReader(jobHandle);
			//Dependency = jobHandle;

			// V2

            NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = _batchManagerSystem.GetNativeBatchGroups(true, out JobHandle job);
			NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = _batchManagerSystem.GetNativeBatchInstances(false, out JobHandle jb);
			jb.Complete();

			ScalMeshJob scalMeshJob = new()
			{
				EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
				MeshBatcheLookup = SystemAPI.GetBufferLookup<MeshBatch>(),
				TransformLookup = SystemAPI.GetComponentLookup<Transform>(),
				InterpolatedTransformLookup = SystemAPI.GetComponentLookup<InterpolatedTransform>(),
				CullingInfoLookup = SystemAPI.GetComponentLookup<CullingInfo>(),
				TransformObjectLookup = SystemAPI.GetComponentLookup<TransformObject>(),
				HiddenLookup = SystemAPI.GetComponentLookup<Hidden>(isReadOnly: true),
				NativeBatchInstances = nativeBatchInstances.AsParallelInstanceWriter(),
				NativeBatchGroups = nativeBatchGroups,
			};
			JobHandle jobHandle = scalMeshJob.Schedule(_updateQuery, JobHandle.CombineDependencies(Dependency, job));
			_preCullingSystem.AddCullingDataReader(jobHandle);
			Dependency = jobHandle;
		}



#if RELEASE
	[BurstCompile]
#endif
		private struct ScalMeshJob : IJobChunk
		{
			public EntityTypeHandle EntityTypeHandle;
			public BufferLookup<MeshBatch> MeshBatcheLookup;
			public ComponentLookup<Transform> TransformLookup;
            public ComponentLookup<InterpolatedTransform> InterpolatedTransformLookup;
            public ComponentLookup<CullingInfo> CullingInfoLookup;
			public ComponentLookup<TransformObject> TransformObjectLookup;
			public ComponentLookup<Hidden> HiddenLookup;
			public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceWriter NativeBatchInstances;
			public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> NativeBatchGroups;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
				NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                foreach ( Entity entity in entities)
				{

                    TransformObject transformObject = TransformObjectLookup[entity];
                    CullingInfo cullingInfo = CullingInfoLookup[entity];

                    Transform transform;
                    if (InterpolatedTransformLookup.HasComponent(entity))
                    {
                        transform = InterpolatedTransformLookup[entity].ToTransform();
                    }
                    else
                    {
                        transform = TransformLookup[entity];
                    }

                    bool hidden = HiddenLookup.HasComponent(entity);

                    DynamicBuffer<MeshBatch> MeshBatchBuffer = MeshBatcheLookup[entity];
                    for (int i = 0; i < MeshBatchBuffer.Length; i++)
                    {
                        MeshBatch meshBatch = MeshBatchBuffer[i];
                        GroupData groupData = this.NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);

                        float3 translation2 = transform.m_Position + math.rotate(transform.m_Rotation, transformObject.m_Scale * groupData.m_SecondaryCenter);
                        float3 scale2 = transformObject.m_Scale * groupData.m_SecondarySize;
                        float3x4 float3x = TransformHelper.TRS(transform.m_Position, transform.m_Rotation, transformObject.m_Scale);
                        float3x4 secondaryValue = TransformHelper.TRS(translation2, transform.m_Rotation, scale2);

                        ref CullingData ptr2 = ref this.NativeBatchInstances.SetTransformValue(float3x, secondaryValue, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
						ptr2.m_Bounds = cullingInfo.m_Bounds;
						ptr2.isHidden = hidden;
                        ptr2.lodOffset = 0;
                    }
                }
            }
        }

#if RELEASE
	[BurstCompile]
#endif
		private struct ScalMeshJob_ParallelForDefer : IJobParallelForDefer
		{
			public NativeList<PreCullingData> CullingDatas;
			public BufferLookup<MeshBatch> MeshBatcheLookup;
			public ComponentLookup<Transform> TransformLookup;
			public ComponentLookup<InterpolatedTransform> InterpolatedTransformLookup;
			public ComponentLookup<CullingInfo> CullingInfoLookup;
			public ComponentLookup<TransformObject> TransformObjectLookup;
			public ComponentLookup<Hidden> HiddenLookup;
			public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceWriter NativeBatchInstances;
			public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> NativeBatchGroups;

			public void Execute(int index)
			{
				PreCullingData preCullingData = CullingDatas[index];

				//if ((preCullingData.m_Flags & PreCullingFlags.NearCamera) == 0U)
				//{
				//	return;
				//}

				if (!MeshBatcheLookup.TryGetBuffer(preCullingData.m_Entity, out DynamicBuffer<MeshBatch> MeshBatchBuffer) || !TransformObjectLookup.TryGetComponent(preCullingData.m_Entity, out TransformObject transformObject))
				{
					return;
				}

				for (int i = 0; i < MeshBatchBuffer.Length; i++)
				{
					CullingInfo cullingInfo = this.CullingInfoLookup[preCullingData.m_Entity];
					//Transform transform = TransformLookup[preCullingData.m_Entity];
					MeshBatch meshBatch = MeshBatchBuffer[i];
					GroupData groupData = this.NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);

                    Transform transform;
                    if ((preCullingData.m_Flags & PreCullingFlags.InterpolatedTransform) != 0U)
                    {
                        transform = InterpolatedTransformLookup[preCullingData.m_Entity].ToTransform();
                    }
                    else
                    {
                        transform = TransformLookup[preCullingData.m_Entity];
                    }

                    float3 translation2 = transform.m_Position + math.rotate(transform.m_Rotation, transformObject.m_Scale * groupData.m_SecondaryCenter);
					float3 scale2 = transformObject.m_Scale * groupData.m_SecondarySize;
					float3x4 float3x = TransformHelper.TRS(transform.m_Position, transform.m_Rotation, transformObject.m_Scale);
					float3x4 secondaryValue = TransformHelper.TRS(translation2, transform.m_Rotation, scale2);

					ref CullingData ptr2 = ref this.NativeBatchInstances.SetTransformValue(float3x, secondaryValue, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
					ptr2.m_Bounds = cullingInfo.m_Bounds;
					ptr2.isHidden = HiddenLookup.HasComponent(preCullingData.m_Entity);
					ptr2.lodOffset = 0;
				}
			}
		}
	}
}
