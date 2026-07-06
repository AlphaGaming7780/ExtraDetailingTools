using Colossal;
using Colossal.Collections;
using Game;
using Game.Common;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Gizmos
{
    internal partial class GizmosRaycastSystem : GameSystemBase
    {

        private bool m_Updating;

        private JobHandle m_Dependencies;

        private List<object> m_InputContext;

        private List<object> m_ResultContext;

        private NativeList<GizmosRaycastInput> m_Input;

        private NativeList<RaycastResult> m_Result;

        private GizmosSystem m_GizmosSystem;

        private EntityQuery m_GizmosDataQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GizmosSystem = World.GetOrCreateSystemManaged<GizmosSystem>();
            m_GizmosDataQuery = GetEntityQuery(ComponentType.ReadOnly<GizmosData>());

            m_InputContext = new List<object>(1);
            m_ResultContext = new List<object>(1);
            m_Input = new NativeList<GizmosRaycastInput>(1, Allocator.Persistent);
            m_Result = new NativeList<RaycastResult>(1, Allocator.Persistent);

            //RequireForUpdate(m_GizmosDataQuery);
        }

        protected override void OnDestroy()
        {
            m_Dependencies.Complete();
            m_Input.Dispose();
            m_Result.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            CompleteRaycast();
            m_ResultContext.Clear();
            m_ResultContext.AddRange(m_InputContext);
            m_Result.ResizeUninitialized(m_Input.Length);
            NativeAccumulator<RaycastResult> accumulator = new NativeAccumulator<RaycastResult>(m_Input.Length, Allocator.TempJob);
            m_Dependencies = PerformRaycast(accumulator);
            Dependency = m_Dependencies;
            RaycastResultJob jobData = new RaycastResultJob
            {
                m_Accumulator = accumulator,
                m_Result = m_Result
            };
            m_Dependencies = IJobParallelForExtensions.Schedule(jobData, m_Input.Length, 1, m_Dependencies);
            accumulator.Dispose(m_Dependencies);
            m_Updating = true;
        }

        private JobHandle PerformRaycast(NativeAccumulator<RaycastResult> accumulator)
        {
            bool debug = NeedDebug();

            var job = new GizmoRaycastJob
            {
                EntityHandle = GetEntityTypeHandle(),
                GizmoHandle = GetComponentTypeHandle<GizmosData>(true),
                Inputs = m_Input.AsArray(),
                Accumulator = accumulator.AsParallelWriter(),
            };

            if (debug) 
            {
                job.Batcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle dep);
                Dependency = JobHandle.CombineDependencies(Dependency, dep);
            }

            JobHandle jobHandle = job.ScheduleParallel(m_GizmosDataQuery, Dependency);

            if (debug) 
            {
                m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
            }

            return jobHandle;
        }

        public void AddInput(object context, GizmosRaycastInput input)
        {
            CompleteRaycast();
            m_InputContext.Add(context);
            m_Input.Add(in input);
        }

        private void CompleteRaycast()
        {
            if (m_Updating)
            {
                m_Updating = false;
                m_Dependencies.Complete();
                m_InputContext.Clear();
                m_Input.Clear();
            }
        }

        public NativeArray<RaycastResult> GetResult(object context)
        {
            CompleteRaycast();
            int num = -1;
            for (int i = 0; i < m_ResultContext.Count; i++)
            {
                if (m_ResultContext[i] == context)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                EDT.Logger.Warn($"Context not found: {context}, lenght: {m_ResultContext.Count}");
                return default(NativeArray<RaycastResult>);
            }
            int num2 = 1;
            for (int j = num + 1; j < m_ResultContext.Count && m_ResultContext[j] == context; j++)
            {
                num2++;
            }

            return m_Result.AsArray().GetSubArray(num, num2);
        }

        private bool NeedDebug()
        {
            foreach(GizmosRaycastInput r in m_Input)
            {
                if (r.m_Debug) return true;
            }
            return false;
        }

    }
}
