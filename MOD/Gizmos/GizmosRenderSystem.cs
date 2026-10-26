using Colossal;
using Colossal.Mathematics;
using Game;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Gizmos
{
    public partial class GizmosRenderSystem : GameSystemBase
    {

        private GizmosSystem m_GizmosSystem;

        private EntityQuery m_GizmosDataQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GizmosSystem = World.GetOrCreateSystemManaged<GizmosSystem>();
            m_GizmosDataQuery = GetEntityQuery(ComponentType.ReadOnly<GizmosData>());

            RequireForUpdate(m_GizmosDataQuery);

        }

        protected override void OnUpdate()
        {
            Camera camera = Camera.main;
            JobHandle jobHandle = new RenderGizmos()
            {
                Batcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle dep),
                EntityHandle = GetEntityTypeHandle(),
                GizmoHandle = GetComponentTypeHandle<GizmosData>(true),
                HighlightedLookup = GetComponentLookup<Highlighted>(true),
                HasCamera = camera != null,
                CameraPosition = camera != null ? (float3)camera.transform.position : float3.zero,
            }.Schedule(m_GizmosDataQuery, JobHandle.CombineDependencies(Dependency, dep));
            m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
            Dependency = jobHandle;
        }

#if RELEASE
        [BurstCompile]
#endif
        public struct RenderGizmos : IJobChunk
        {
            [ReadOnly] public GizmoBatcher Batcher;
            [ReadOnly] public EntityTypeHandle EntityHandle;
            [ReadOnly] public ComponentTypeHandle<GizmosData> GizmoHandle;
            [ReadOnly] public ComponentLookup<Highlighted> HighlightedLookup;
            [ReadOnly] public bool HasCamera;
            [ReadOnly] public float3 CameraPosition;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {

                var gizmos = chunk.GetNativeArray(ref GizmoHandle);
                var entities = chunk.GetNativeArray(EntityHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity e = entities[i];
                    bool isHighlighted = HighlightedLookup.HasComponent(e);

                    Render(gizmos[i], isHighlighted);
                }
            }

            public void Render(GizmosData g, bool highlighted)
            {
                Color color = highlighted ? GetHighlightedColor(g.Color) : g.Color;

                switch (g.Type)
                {
                    case GizmoType.Line:
                        Batcher.DrawLine(g.A, g.B, color);
                        break;

                    case GizmoType.Bezier:
                        Batcher.DrawBezier(
                            new Bezier4x3(g.A, g.B, g.C, g.D),
                            color,
                            g.Params0.x, /* lenght */
                            g.Segments);
                        break;

                    case GizmoType.ArrowHead:
                        Batcher.DrawArrowHead(
                            g.A,
                            g.B,
                            color,
                            g.Params0.x, /* headLength */
                            g.Params0.y, /* headAngle */
                            g.Segments);
                        break;
                    case GizmoType.Arrow:
                        Batcher.DrawArrow(
                            g.A, 
                            g.B,
                            color, 
                            g.Params0.x, /* headLength */
                            g.Params0.y, /* headAngle */
                            g.Segments);
                        break;
                    case GizmoType.Sphere:
                        Batcher.DrawWireSphere(
                            g.A,
                            g.Params0.x,
                            color,
                            (int)g.Params0.y,
                            (int)g.Params0.z,
                            (int)g.Params0.w
                            );
                        break;
                    case GizmoType.WireArc:
                        if (g.Params0.z > 0f || (g.Flags & (int)GizmoFlags.CullBackHalf) != 0)
                        {
                            RenderTubeArc(g, color);
                            break;
                        }
                        Batcher.DrawWireArc(
                            g.A,
                            g.B,
                            g.C,
                            g.Params0.x,
                            g.Params0.y,
                            color,
                            g.Segments
                            );
                        break;
                    case GizmoType.Cube:
                        Batcher.DrawWireCube(
                            g.TRS,
                            g.A, /* center */
                            g.Params0.xyz, /* size */
                            color
                            );
                        break;
                    case GizmoType.Cylinder:
                        Batcher.DrawWireCylinder(
                            g.TRS,
                            g.A, /* center */
                            g.Params0.x, /* radius */
                            g.Params0.y, /* height */
                            color,
                            g.Segments
                            );
                        break;
                    case GizmoType.Cone:
                        Batcher.DrawWireCone(
                            g.TRS,
                            g.A,
                            g.Params0.x, /* radiusA */
                            g.B,
                            g.Params0.y, /* radiusB */
                            color,
                            g.Segments
                            );
                        break;
                    case GizmoType.Capsule:
                        Batcher.DrawWireCapsule(
                            g.TRS,
                            g.A, /* center */
                            g.Params0.x, /* radius */
                            g.Params0.y, /* height */
                            color,
                            g.Segments
                            );
                        break;
                    case GizmoType.CapsuleConic:
                        Batcher.DrawWireCapsuleConic(
                            g.TRS,
                            g.A,
                            g.Params0.x, /* radiusA */
                            g.B,
                            g.Params0.y, /* radiusB */
                            color,
                            g.Segments
                            );
                        break;
                    case GizmoType.Frustum:
                        Batcher.DrawWireFrustum(
                            g.TRS,
                            g.Params0.x, /* fov */
                            g.Params0.y, /* minRange */
                            g.Params0.z, /* maxRange */
                            g.Params0.w, /* aspect */
                            color
                            );
                        break;
                }
            }
            private const int kTubeRingCount = 12;
            private const int kTubeRingSegments = 8;

            // Tube (4 lines + rings) when thick, faded back half with CullBackHalf, same segments as IntersectWireArc.
            private void RenderTubeArc(GizmosData g, Color color)
            {
                float3 center = g.A;
                float angle = g.Params0.x;
                float radius = g.Params0.y;
                float thickness = g.Params0.z;
                int segments = math.max(g.Segments, 1);
                bool cullBackHalf = HasCamera && (g.Flags & (int)GizmoFlags.CullBackHalf) != 0;
                Color backColor = GetBackHalfColor(color);

                float3 n = math.normalize(g.B);
                float3 f = math.normalize(g.C - n * math.dot(g.C, n));
                float angleRad = math.radians(angle);
                float step = angleRad / segments;

                float3 prevDir = f;
                for (int i = 1; i <= segments; i++)
                {
                    float3 dir = math.rotate(quaternion.AxisAngle(n, step * i), f);
                    Color segmentColor = cullBackHalf && GizmosUtils.IsOnBackHalf(center, n, center + math.normalize(prevDir + dir) * radius, CameraPosition) ? backColor : color;

                    if (thickness > 0f)
                    {
                        Batcher.DrawLine(center + prevDir * (radius + thickness), center + dir * (radius + thickness), segmentColor);
                        Batcher.DrawLine(center + prevDir * math.max(radius - thickness, 0f), center + dir * math.max(radius - thickness, 0f), segmentColor);
                        Batcher.DrawLine(center + n * thickness + prevDir * radius, center + n * thickness + dir * radius, segmentColor);
                        Batcher.DrawLine(center - n * thickness + prevDir * radius, center - n * thickness + dir * radius, segmentColor);
                    }
                    else
                    {
                        Batcher.DrawLine(center + prevDir * radius, center + dir * radius, segmentColor);
                    }

                    prevDir = dir;
                }

                if (thickness <= 0f) return;

                // Same spacing as on a full circle (kTubeRingCount rings), plus one at the end of a partial arc.
                bool fullCircle = angle >= 360f;
                int rings = math.max(1, (int)math.ceil(kTubeRingCount * angle / 360f));
                float ringStep = angleRad / rings;
                if (!fullCircle) rings++;
                for (int i = 0; i < rings; i++)
                {
                    float3 dir = math.rotate(quaternion.AxisAngle(n, ringStep * i), f);
                    float3 tangent = math.cross(n, dir);
                    Color ringColor = cullBackHalf && GizmosUtils.IsOnBackHalf(center, n, center + dir * radius, CameraPosition) ? backColor : color;
                    Batcher.DrawWireArc(center + dir * radius, tangent, dir, 360f, thickness, ringColor, kTubeRingSegments);
                }
            }

            private readonly Color GetBackHalfColor(Color color)
            {
                color.a *= 0.25f;
                return color;
            }

            private readonly Color GetHighlightedColor(Color color)
            {
                return Color.Lerp(color, Color.white, 0.4f);
            }
        }
    }
}
