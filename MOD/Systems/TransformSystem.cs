using Colossal.Rendering;
using Game;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ExtraDetailingTools.Systems
{
    internal partial class TransformSystem : GameSystemBase
    {
        private BatchManagerSystem _batchManagerSystem;
        private PreCullingSystem _preCullingSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            EDT.Logger.Info("EDT_BatchDataSystem Created");
            _batchManagerSystem = World.GetOrCreateSystemManaged<BatchManagerSystem>();
            _preCullingSystem = World.GetOrCreateSystemManaged<PreCullingSystem>();
        }
        protected override void OnUpdate()
        {
            NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = _batchManagerSystem.GetNativeBatchGroups(true, out JobHandle job);
            NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = _batchManagerSystem.GetNativeBatchInstances(false, out JobHandle jb);
            jb.Complete();

            ScalMeshJob scalMeshJob = new ()
            {
                CullingDatas = _preCullingSystem.GetCullingData(true, out JobHandle job2),
                MeshBatcheLookup = SystemAPI.GetBufferLookup<MeshBatch>(isReadOnly: true),
                TransformLookup = SystemAPI.GetComponentLookup<Transform>(isReadOnly: true),
                CullingInfoLookup = SystemAPI.GetComponentLookup<CullingInfo>(isReadOnly: true),
                HiddenLookup = SystemAPI.GetComponentLookup<Hidden>(isReadOnly: true),
                NativeBatchInstances = nativeBatchInstances.AsParallelInstanceWriter(),
                NativeBatchGroups = nativeBatchGroups,
                Scale = new (2, 2, 2),
            };
            JobHandle jobHandle = scalMeshJob.Schedule(scalMeshJob.CullingDatas, 16, JobHandle.CombineDependencies(Dependency, job2, job));
            _preCullingSystem.AddCullingDataReader(jobHandle);
            Dependency = jobHandle;
        }

#if RELEASE
    [BurstCompile]
#endif
        private struct ScalMeshJob : IJobParallelForDefer
        {
            public NativeList<PreCullingData> CullingDatas;
            public BufferLookup<MeshBatch> MeshBatcheLookup;
            public ComponentLookup<Transform> TransformLookup;
            public ComponentLookup<CullingInfo> CullingInfoLookup;
            public ComponentLookup<Hidden> HiddenLookup;
            public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceWriter NativeBatchInstances;
            public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> NativeBatchGroups;
            public PreCullingFlags CullingFlags;
            public float3 Scale;

            public void Execute(int index)
            {
                PreCullingData preCullingData = CullingDatas[index];

                if ((preCullingData.m_Flags & CullingFlags) == 0U || (preCullingData.m_Flags & PreCullingFlags.NearCamera) == 0U)
                {
                    return;
                }



                if (!MeshBatcheLookup.TryGetBuffer(preCullingData.m_Entity, out DynamicBuffer<MeshBatch> MeshBatchBuffer) || !TransformLookup.TryGetComponent(preCullingData.m_Entity, out Transform transform))
                {
                    return;
                }

                for (int i = 0; i < MeshBatchBuffer.Length; i++)
                {
                    CullingInfo cullingInfo = this.CullingInfoLookup[preCullingData.m_Entity];
                    //Transform transform = TransformLookup[preCullingData.m_Entity];
                    MeshBatch meshBatch = MeshBatchBuffer[i];
                    GroupData groupData = this.NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);

                    float3 translation2 = transform.m_Position + math.rotate(transform.m_Rotation, Scale * groupData.m_SecondaryCenter);
                    float3 scale2 = Scale * groupData.m_SecondarySize;
                    float3x4 float3x = TransformHelper.TRS(transform.m_Position, transform.m_Rotation, Scale);
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
