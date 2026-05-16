using Colossal;
using Colossal.Collections;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Objects;
using System.Collections.Generic;
using TMPro;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using RaycastHit = Game.Common.RaycastHit;

namespace ExtraDetailingTools.Gizmos
{
    internal partial class GizmosRaycastSystem : GameSystemBase
    {

        [BurstCompile]
        private struct RaycastResultJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeAccumulator<RaycastResult> m_Accumulator;

            [NativeDisableParallelForRestriction]
            public NativeList<RaycastResult> m_Result;

            public void Execute(int index)
            {
                m_Result[index] = m_Accumulator.GetResult(index);
            }
        }
#if RELEASE
        [BurstCompile]
#endif
        private struct GizmoRaycastJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityHandle;
            [ReadOnly] public ComponentTypeHandle<GizmosData> GizmoHandle;

            [ReadOnly] public NativeArray<GizmosRaycastInput> Inputs;

            public NativeAccumulator<RaycastResult>.ParallelWriter Accumulator;

            [ReadOnly] public float tanFOV;
            [ReadOnly] public float3 CameraPosition;
            [ReadOnly] public float PixelScale;
            [ReadOnly] public GizmoBatcher Batcher;

            private bool m_Debug;

            public void Execute(
                in ArchetypeChunk chunk,
                int chunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var gizmos = chunk.GetNativeArray(ref GizmoHandle);

                for (int i = 0; i < gizmos.Length; i++)
                {
                    var entity = entities[i];
                    var gizmo = gizmos[i];

                    for (int inputIndex = 0; inputIndex < Inputs.Length; inputIndex++)
                    {
                        var input = Inputs[inputIndex];
                        m_Debug = input.m_Debug;

                        if (!MatchType(gizmo.Type, input.m_Type))
                        {
                            continue;
                        }

                        if (Intersect(gizmo, input.m_Line, input.m_Tolerance, out RaycastHit hit))
                        {
                            hit.m_HitEntity = entity;

                            Accumulator.Accumulate(inputIndex, new RaycastResult
                            {
                                m_Hit = hit,
                                m_Owner = entity
                            });
                        }
                    }
                }
            }

            private bool MatchType(GizmoType type, GizmosRaycastType mask)
            {
                return ((GizmosRaycastType)(1u << (int)type) & mask) != 0;
            }

            private bool Intersect(GizmosData g, Line3.Segment line, float tolerance, out RaycastHit hit)
            {
                switch (g.Type)
                {
                    case GizmoType.Line:
                        return IntersectCapsule(g.A, g.B, tolerance, line, out hit);

                    case GizmoType.Sphere:
                        return IntersectSphere(g.A, g.Params0.x + tolerance, line, out hit);

                    case GizmoType.Cube:
                        return IntersectAABB(g, line, tolerance, out hit);

                    case GizmoType.Arrow:
                        return IntersectArrow(g.A, g.B, g.Params0.x, g.Params0.y, line, tolerance, out hit);

                    case GizmoType.WireArc:
                        return IntersectWireArc(g, line, tolerance, out hit);

                    default:
                        hit = default;
                        return false;
                }
            }

            private bool IntersectSphere(float3 center, float radius, Line3.Segment ray, out RaycastHit hit)
            {

                if (m_Debug)
                {
                    Batcher.DrawWireSphere(center, radius, Color.green);
                }

                hit = default;

                float3 dir = ray.b - ray.a;
                float rayLength = math.length(dir);

                if (rayLength <= 1e-6f)
                    return false;

                dir /= rayLength;

                if (!MathUtils.Intersect(new Sphere3(radius, center), ray, out float2 t))
                    return false;

                float tRay = t.x;

                float3 pRay = ray.a + dir * tRay;

                float3 normal = pRay - center;
                float normalLen = math.length(normal);

                if (normalLen > 1e-5f)
                    normal /= normalLen;
                else
                    normal = dir;

                hit = new RaycastHit
                {
                    m_Position = center,
                    m_HitPosition = pRay,
                    m_HitDirection = normal,
                    m_NormalizedDistance = tRay / rayLength,
                    m_CurvePosition = 0f
                };

                return true;
            }

            // NEED REWORK
            private static bool IntersectAABB(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {

                float3 min = g.A - g.Params0.xyz * 0.5f - tolerance;
                float3 max = g.A + g.Params0.xyz * 0.5f + tolerance;

                float3 extents = (max - min) * 0.5f;
                float3 center = min + extents;

                // Construire un Bounds Unity propre
                Bounds3 bounds = new Bounds3( min, max );

                if (!MathUtils.Intersect(bounds, ray, out float2 t))
                {
                    hit = default;
                    return false;
                }

                // Si t.x < 0 → l'intersection est derrière le début du segment
                if (t.x < 0f)
                {
                    hit = default;
                    return false;
                }

                // Position du hit sur le segment
                float3 hitPos = MathUtils.Position(ray, t.x);

                hit = new RaycastHit
                {
                    m_Position = center,
                    m_HitPosition = hitPos,
                    m_NormalizedDistance = t.x,
                };

                return true;
            }

            private bool IntersectArrow(float3 a, float3 b, float headLength, float headAngleDeg, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                // Compute ray direction and length
                float3 rayDir = ray.b - ray.a;
                float rayLength = math.length(rayDir);

                // Degenerate ray → no hit
                if (rayLength <= 0f)
                {
                    hit = default;
                    return false;
                }

                float angleRad = math.radians(headAngleDeg);
                float coneRadius = math.tan(angleRad) * headLength;
                float radius = coneRadius + tolerance;

                float3 ab = b - a;
                float abLen = math.length(ab);

                if (abLen <= 1e-6f)
                {
                    hit = default;
                    return false;
                }

                float3 abDir = ab / abLen;

                // Inner capsule segment:
                //
                // Without tolerance:
                //   - The capsule (cylinder + hemispheres) must start exactly at A and end exactly at B.
                //   - That means the internal segment of the capsule is [A + coneRadius * dir, B - coneRadius * dir].
                //
                // With tolerance:
                //   - We only grow the radius (coneRadius + tolerance),
                //   - So the shape can extend beyond A and B, but the "logical" arrow is still [A, B].
                float3 innerA = a + abDir * coneRadius;
                float3 innerB = b - abDir * coneRadius;

                // If the arrow is too short to contain the inner segment
                if (math.dot(innerB - innerA, abDir) <= 0f)
                {
                    hit = default;
                    return false;
                }

                // Perform the capsule intersection test on the inner segment
                if (IntersectCapsule(innerA, innerB, radius, ray, out hit))
                {
                    return true;
                }

                hit = default;
                return false;
            }

            private bool IntersectWireArc(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                hit = default;

                float3 center = g.A;
                float3 normal = g.B;
                float3 from = g.C;
                float angle = g.Params0.x;
                float radius = g.Params0.y;
                int segments = g.Segments;

                float angleRad = math.radians(angle);

                float3 n = math.normalize(normal);
                float3 f = math.normalize(from);

                f = math.normalize(f - n * math.dot(f, n));

                float step = angleRad / segments;

                float3 prev = center + f * radius;

                float bestDist = float.MaxValue;
                bool found = false;

                for (int i = 1; i <= segments; i++)
                {
                    float a = step * i;

                    quaternion rot = quaternion.AxisAngle(n, a);
                    float3 dir = math.rotate(rot, f);

                    float3 curr = center + dir * radius;

                    float capsuleRadius = tolerance;

                    if (IntersectCapsule(prev, curr, capsuleRadius, ray, out RaycastHit h))
                    {
                        if (h.m_NormalizedDistance < bestDist)
                        {
                            bestDist = h.m_NormalizedDistance;
                            hit = h;
                            found = true;
                        }
                    }

                    prev = curr;
                }

                return found;
            }

            private bool IntersectCapsule(float3 a, float3 b, float radius, Line3.Segment ray, out RaycastHit hit)
            {
                if (m_Debug)
                {
                    float3 dir = b - a;
                    float length = math.length(dir);

                    if (length > 1e-5f)
                    {
                        dir /= length;

                        float3 up = dir;
                        float3 arbitrary = math.abs(up.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0);
                        float3 forward = math.normalize(math.cross(arbitrary, up));
                        float3 right = math.normalize(math.cross(up, forward));

                        float4x4 rotM = new float4x4(
                            new float4(right, 0),
                            new float4(up, 0),
                            new float4(forward, 0),
                            new float4(0, 0, 0, 1)
                        );

                        float3 center = (a + b) * 0.5f;

                        float4x4 trs = float4x4.TRS(center, new quaternion(rotM), new float3(1, 1, 1));

                        Batcher.DrawWireCapsule(
                            trs,
                            float3.zero,
                            radius,
                            length + radius * 2f,
                            Color.green
                        );
                    }
                }

                float3 rayDir = ray.b - ray.a;
                float rayLength = math.length(rayDir);

                if (rayLength <= 0f)
                {
                    hit = default;
                    return false;
                }

                rayDir /= rayLength;

                float dist = MathUtils.Distance(new Line3.Segment(a, b), ray, out float2 t);

                if (dist > radius)
                {
                    hit = default;
                    return false;
                }

                float tSeg = t.x;
                float tRay = t.y;

                // points les plus proches
                float3 pRay = ray.a + rayDir * tRay;
                float3 pSeg = math.lerp(a, b, tSeg);

                float3 normal = pRay - pSeg;
                float normalLen = math.length(normal);

                // éviter NaN si pile au centre
                if (normalLen > 1e-5f)
                    normal /= normalLen;
                else
                    normal = math.normalize(rayDir); // fallback

                hit = new RaycastHit
                {
                    m_Position = a,
                    m_HitPosition = pSeg,
                    m_HitDirection = normal,
                    m_NormalizedDistance = tRay / rayLength,
                    m_CurvePosition = tSeg
                };

                return true;
            }

        }

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
            Camera cam = Camera.main;

            float tanFOV = math.tan(math.radians(cam.fieldOfView) * 0.5f);

            float pixelScale = 2f * tanFOV / cam.pixelHeight;

            bool debug = NeedDebug();

            var job = new GizmoRaycastJob
            {
                EntityHandle = GetEntityTypeHandle(),
                GizmoHandle = GetComponentTypeHandle<GizmosData>(true),
                Inputs = m_Input.AsArray(),
                Accumulator = accumulator.AsParallelWriter(),

                tanFOV = tanFOV,
                CameraPosition = cam.transform.position,
                PixelScale = pixelScale,
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
