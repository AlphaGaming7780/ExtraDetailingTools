using Colossal.Entities;
using ExtraLib;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static ExtraDetailingTools.ExtraSnap.ObjectToolSystemExtraSnap;
using static Game.Tools.ObjectToolSystem;
using Colossal.Mathematics;

#if RELEASE
using Unity.Burst;
#endif


namespace ExtraDetailingTools.ExtraSnap
{
    internal class ObjectToolSystemExtraSnap : ExtraSnap<ObjectToolSystem, ObjectToolExtraSnap>
    {
        [Flags]
        public enum ObjectToolExtraSnap : uint
        {
            ObjectSurface,
            ObjectSide,
        }

        readonly Traverse traverse;

        ObjectToolSystemExtraSnap() : base()
        {
            traverse = Traverse.Create(_Tool);
        }

        protected override void InitializeRaycast()
        {
            PrefabBase m_Prefab = traverse.Field("m_Prefab").GetValue<PrefabBase>();
            Snap snap = GetActualToolSnap();

            if (m_Prefab != null)
            {
                // Same as snap to surface I guess for now
                if ((snap & Snap.ObjectSide) != Snap.None)
                {
                    _ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    if (_ToolSystem.actionMode.IsEditor())
                    {
                        _ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }
                }
            }
        }

        protected override JobHandle SnapControlPoint(JobHandle inputDeps)
        {
            NativeList<ControlPoint> controlPoints = traverse.Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>();
            Entity selected = ((_Tool.actualMode == Mode.Move) ? traverse.Field("m_MovingObject").GetValue<Entity>() : GetUpgradable(_ToolSystem.selected));

            JobHandle jobHandle = IJobExtensions.Schedule(new SnapJob
            {
                m_EditorMode = _ToolSystem.actionMode.IsEditor(),
                m_Snap = traverse.Method("GetActualSnap").GetValue<Snap>(),
                m_Mode = _Tool.actualMode,
                m_Prefab = _PrefabSystem.GetEntity(traverse.Field("m_Prefab").GetValue<PrefabBase>()),
                m_Selected = selected,
                m_LastRaycastPoint = traverse.Field("m_LastRaycastPoint").GetValue<ControlPoint>(),
                m_Rotation = GetRotation().m_Rotation,
                m_ControlPoints = controlPoints,

                m_OwnerData = _Tool.GetComponentLookup<Owner>(true),
                m_TransformData = _Tool.GetComponentLookup<Transform>(true),
                m_LocalTransformCacheData = _Tool.GetComponentLookup<LocalTransformCache>(true),
                m_ServiceUpgradeData = _Tool.GetComponentLookup<Game.Buildings.ServiceUpgrade>(true),
                m_ObjectGeometryData = _Tool.GetComponentLookup<ObjectGeometryData>(true),
                m_PrefabRefData = _Tool.GetComponentLookup<PrefabRef>(true),
            }, inputDeps);

            return jobHandle;
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct SnapJob : IJob
        {
            [ReadOnly]
            public bool m_EditorMode;

            [ReadOnly]
            public Snap m_Snap;

            [ReadOnly]
            public Mode m_Mode;

            [ReadOnly]
            public Entity m_Prefab;

            [ReadOnly]
            public Entity m_Selected;

            [ReadOnly]
            public ControlPoint m_LastRaycastPoint;

            public NativeList<ControlPoint> m_ControlPoints;

            // Component lookups
            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly]
            public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

            [ReadOnly]
            public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public quaternion m_Rotation;

            public void Execute()
            {
                ControlPoint controlPoint = m_LastRaycastPoint;
                ControlPoint bestSnapPosition = m_ControlPoints[m_ControlPoints.Length - 1];

                if ((m_Snap & Snap.ObjectSurface) != Snap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    int parentMesh = controlPoint.m_ElementIndex.x;
                    Entity entity2 = controlPoint.m_OriginalEntity;
                    while (m_OwnerData.HasComponent(entity2))
                    {
                        if (m_LocalTransformCacheData.HasComponent(entity2) && !m_ServiceUpgradeData.HasComponent(entity2))
                        {
                            parentMesh = m_LocalTransformCacheData[entity2].m_ParentMesh;
                            parentMesh += math.select(1000, -1000, parentMesh < 0);
                        }
                        entity2 = m_OwnerData[entity2].m_Owner;
                    }
                    if (m_TransformData.HasComponent(entity2))
                    {
                        SnapSurface(controlPoint, ref bestSnapPosition, entity2, parentMesh);
                    }
                }

                if ((m_Snap & Snap.ObjectSide) != Snap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                {
                    Entity targetEntity = controlPoint.m_OriginalEntity;
                    if (targetEntity == Entity.Null)
                        return;

                    // ===== Prefab & target bounds =====
                    Bounds3 placedBounds = ObjectUtils.GetBounds(m_ObjectGeometryData[m_Prefab]);

                    PrefabRef targetPrefabRef = m_PrefabRefData[targetEntity];
                    Entity targetPrefab = targetPrefabRef.m_Prefab;

                    Transform targetTransform = m_TransformData[targetEntity];
                    Bounds3 targetBounds = ObjectUtils.GetBounds(m_ObjectGeometryData[targetPrefab]);

                    float3 placedSize = placedBounds.max - placedBounds.min;
                    float3 targetSize = targetBounds.max - targetBounds.min;

                    // ===== Target base quad =====
                    Quad2 targetQuad = ObjectUtils.CalculateBaseCorners(
                        targetTransform.m_Position,
                        targetTransform.m_Rotation,
                        targetBounds
                    ).xz;


                    // ===== Rotation handling (relative to target object) =====
                    // Compute rotation of placed object relative to target
                    quaternion relativeRotation =
                        math.mul(
                            math.inverse(targetTransform.m_Rotation),
                            m_Rotation
                        );

                    // Clamp rotation in target local space
                    quaternion snappedRelativeRotation = ClampTo90(relativeRotation);

                    // Convert back to world space
                    quaternion snappedWorldRotation =
                        math.mul(
                            targetTransform.m_Rotation,
                            snappedRelativeRotation
                        );

                    // Extract final yaw used for snapping
                    float snappedYaw = math.Euler(snappedWorldRotation).y;


                    // ===== Snap validity test =====
                    Quad2 placedQuadAtTarget = ObjectUtils.CalculateBaseCorners(
                        targetTransform.m_Position,
                        targetTransform.m_Rotation,
                        new float2(placedSize.x, placedSize.z)
                    ).xz;

                    Quad2 placedQuadAtHit = ObjectUtils.CalculateBaseCorners(
                        controlPoint.m_HitPosition,
                        snappedWorldRotation,
                        placedSize.xz * 0.5f
                    ).xz;

                    bool allowSnap =
                        MathUtils.Intersect(placedQuadAtTarget, placedQuadAtHit) &&
                        MathUtils.Intersect(targetQuad, controlPoint.m_HitPosition.xz);

                    // ===== Iterate over all target edges =====
                    CheckSnapLineGeneric(
                        placedBounds,
                        targetTransform,
                        controlPoint,
                        ref bestSnapPosition,
                        new Line2(targetQuad.a, targetQuad.b),
                        snappedYaw,
                        allowSnap
                    );

                    CheckSnapLineGeneric(
                        placedBounds,
                        targetTransform,
                        controlPoint,
                        ref bestSnapPosition,
                        new Line2(targetQuad.b, targetQuad.c),
                        snappedYaw,
                        allowSnap
                    );

                    CheckSnapLineGeneric(
                        placedBounds,
                        targetTransform,
                        controlPoint,
                        ref bestSnapPosition,
                        new Line2(targetQuad.c, targetQuad.d),
                        snappedYaw,
                        allowSnap
                    );

                    CheckSnapLineGeneric(
                        placedBounds,
                        targetTransform,
                        controlPoint,
                        ref bestSnapPosition,
                        new Line2(targetQuad.d, targetQuad.a),
                        snappedYaw,
                        allowSnap
                    );
                }


                m_ControlPoints[m_ControlPoints.Length - 1] = bestSnapPosition;

            }

            private void SnapSurface(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, int parentMesh)
            {
                Transform transform = m_TransformData[entity];
                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = entity;
                snapPosition.m_ElementIndex.x = parentMesh;
                snapPosition.m_Position = controlPoint.m_HitPosition;
                snapPosition.m_Direction = math.forward(transform.m_Rotation).xz;
                snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
                AddSnapPosition(ref bestPosition, snapPosition);
            }

            private static void CheckSnapLineGeneric(
                Bounds3 bounds,                    // Bounds of the PLACED prefab
                Transform targetTransform,          // Transform of the TARGET object
                ControlPoint controlPoint,
                ref ControlPoint bestPosition,
                Line2 line,
                float angle,
                bool forceSnap)
            {
                // Rotation of the placed object (world)
                quaternion rotation = quaternion.RotateY(angle);

                // Edge direction & outward normal
                float2 edgeDir = math.normalize(line.b - line.a);
                float2 normal = new float2(-edgeDir.y, edgeDir.x);

                // Half-size of placed object (local space)
                float3 size = bounds.max - bounds.min;
                float2 halfSize = size.xz * 0.5f;

                // Local axes of the placed object in world space
                float2 axisX =
                    math.normalize(
                        math.mul(rotation, new float3(1f, 0f, 0f)).xz
                    );

                float2 axisZ =
                    math.normalize(
                        math.mul(rotation, new float3(0f, 0f, 1f)).xz
                    );

                // Project OBB onto edge normal
                float offset =
                    math.abs(math.dot(axisX, normal)) * halfSize.x +
                    math.abs(math.dot(axisZ, normal)) * halfSize.y;



                // This shit doesn't work, can't slide the prefab on the line.
                // Project OBB onto edge direction (PARALLEL side length)
                float halfLength =
                     math.abs(math.dot(axisX, edgeDir)) * halfSize.x +
                     math.abs(math.dot(axisZ, edgeDir)) * halfSize.y;

                // Project mouse position onto edge
                MathUtils.Distance(line, controlPoint.m_Position.xz, out float t);

                float lineLength = math.distance(line.a, line.b);
                t *= lineLength;
                //t = MathUtils.Snap(t, 0.5f);
                t = math.clamp(t, 0f - halfLength, lineLength + halfLength);

                // Position along the edge (THIS is what allows sliding)
                float2 pointOnLine = math.lerp(line.a, line.b, t / lineLength);



                // Final snapped position
                float2 snappedXZ = pointOnLine + normal * offset;

                ControlPoint snapPosition = controlPoint;
                snapPosition.m_OriginalEntity = Entity.Null;
                snapPosition.m_Position.xz = snappedXZ;
                snapPosition.m_Position.y = targetTransform.m_Position.y;

                // Rotation aligned to target edge
                snapPosition.m_Direction =
                    math.mul(
                        rotation,
                        new float3(0f, 0f, 1f)
                    ).xz;

                snapPosition.m_Rotation =
                    ToolUtils.CalculateRotation(snapPosition.m_Direction);

                float level = forceSnap ? 1f : 0f;
                snapPosition.m_SnapPriority =
                    ToolUtils.CalculateSnapPriority(
                        level,
                        1f,
                        0f,
                        controlPoint.m_HitPosition * 0.5f,
                        snapPosition.m_Position * 0.5f,
                        snapPosition.m_Direction
                    );

                AddSnapPosition(ref bestPosition, snapPosition);
            }


            private static void AddSnapPosition(ref ControlPoint bestSnapPosition, ControlPoint snapPosition)
            {
                if (ToolUtils.CompareSnapPriority(snapPosition.m_SnapPriority, bestSnapPosition.m_SnapPriority))
                {
                    bestSnapPosition = snapPosition;
                }
            }

            public static quaternion ClampTo90(UnityEngine.Quaternion q)
            {
                // Convert quaternion to Euler angles (in degrees)
                float3 euler = q.eulerAngles; // euler() retourne radians, on convertit en degrés

                // Clamp each angle to nearest multiple of 90
                euler.x = math.round(euler.x / 90f) * 90f;
                euler.y = math.round(euler.y / 90f) * 90f;
                euler.z = math.round(euler.z / 90f) * 90f;

                // Reconvert to quaternion
                return UnityEngine.Quaternion.Euler(euler); // On repasse en radians
            }

        }

        private static Entity GetUpgradable(Entity entity)
        {
            if (EL.m_EntityManager.TryGetComponent<Attached>(entity, out var component))
            {
                return component.m_Parent;
            }
            return entity;
        }

        private struct Rotation
        {
            public quaternion m_Rotation;
            public quaternion m_ParentRotation;
            public bool m_IsAligned;
            public bool m_IsSnapped;

            public Rotation(object privateRotation)
            {
                Type type = privateRotation.GetType();
                m_Rotation = (quaternion)type.GetField("m_Rotation", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_ParentRotation = (quaternion)type.GetField("m_ParentRotation", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_IsAligned = (bool)type.GetField("m_IsAligned", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_IsSnapped = (bool)type.GetField("m_IsSnapped", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
            }
        }

        private Rotation GetRotation()
        {
            var field = _Tool.GetType().GetField("m_Rotation", BindingFlags.NonPublic | BindingFlags.Instance);
            var nativeRef = field.GetValue(_Tool); // NativeReference<Rotation>
            object privateRotation = nativeRef.GetType().GetProperty("Value").GetValue(nativeRef);
            return new Rotation(privateRotation);
        }

    }
}
