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
            JobHandle jobHandle = new RenderGizmos()
            {
                Batcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle dep),
                EntityHandle = GetEntityTypeHandle(),
                GizmoHandle = GetComponentTypeHandle<GizmosData>(true),
                HighlightedLookup = GetComponentLookup<Highlighted>(true),
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
            private readonly Color GetHighlightedColor(Color color)
            {
                return Color.Lerp(color, Color.white, 0.4f);
            }
        }
    }
}
