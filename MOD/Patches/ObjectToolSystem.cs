using Colossal.Entities;
using Colossal.Mathematics;
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
using static Game.Tools.ObjectToolSystem;

namespace ExtraDetailingTools.Patches
{
    class ObjectToolSystemPatch
    {

        //[HarmonyPatch(typeof(ObjectToolSystem), "OnStartRunning")]
        //class ObjectToolSystem_OnStartRunning
        //{
        //    static bool first = true;
        //    public static void Postfix(ObjectToolSystem __instance)
        //    {
        //        if (first)
        //        {
        //            __instance.selectedSnap &= ~(Snap.NetArea);
        //            first = false;
        //        }
        //    }
        //}

        //Patche 1.3.3 f1 rotation fixe
       [HarmonyPatch(typeof(ObjectToolSystem), "GetAllowRotation")]
        class ObjectToolSystem_GetAllowRotation
        {
            public static void Postfix(ObjectToolSystem __instance, ref bool __result)
            {
                // Only filter for props
                if (__instance.prefab is not StaticObjectPrefab || __instance.prefab is BuildingPrefab || __instance.prefab is BuildingExtensionPrefab) return;
                __result = __instance.allowRotation;
            }
        }

        [HarmonyPatch(typeof(ObjectToolSystem), "InitializeRaycast")]
        class ObjectToolSystem_InitializeRaycast
        {
            public static void Postfix(ObjectToolSystem __instance)
            {
                Traverse traverse = Traverse.Create(__instance);
                PrefabBase m_Prefab = traverse.Field("m_Prefab").GetValue<PrefabBase>();
                __instance.GetAvailableSnapMask(out var onMask, out var offMask);
                Snap snap = ToolBaseSystem.GetActualSnap(__instance.selectedSnap, onMask, offMask);
                ToolRaycastSystem m_ToolRaycastSystem = traverse.Field("m_ToolRaycastSystem").GetValue<ToolRaycastSystem>();
                ToolSystem m_ToolSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ToolSystem>();

                if (m_Prefab != null)
                {
                    // Same as snap to surface I guess for now
                    if ((snap & Snap.ObjectSide) != Snap.None)
                    {
                        m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                        if (m_ToolSystem.actionMode.IsEditor())
                        {
                            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ObjectToolSystem), "SnapControlPoint")]
        class ObjectToolSystem_SnapControlPoint
        {
            static Entity oldEntity = Entity.Null;

            public static bool Prefix(ObjectToolSystem __instance)
            {
                //ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ToolSystem>();
                //Traverse traverse = Traverse.Create(__instance);
                //NativeList<ControlPoint> controlPoints = traverse.Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>();
                //Entity m_Selected = ((__instance.actualMode == Mode.Move) ? traverse.Field("m_MovingObject").GetValue<Entity>() : GetUpgradable(toolSystem.selected));
                //Entity m_Prefab = EL.m_PrefabSystem.GetEntity(traverse.Field("m_Prefab").GetValue<PrefabBase>());
                //ControlPoint m_LastRaycastPoint = traverse.Field("m_LastRaycastPoint").GetValue<ControlPoint>();
                //ControlPoint controlPoint = controlPoints[controlPoints.Length - 1];

                //EDT.Logger.Info($"SnapControlPoint Prefix");
                //if (m_Selected != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(m_Selected, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"m_Selected: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(m_Selected, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"m_Selected: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"m_Selected: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"m_Selected: null");

                //if (m_Prefab != Entity.Null)
                //    EDT.Logger.Info($"m_Prefab: {EL.m_PrefabSystem.GetPrefab<PrefabBase>(m_Prefab).name}");
                //else
                //    EDT.Logger.Info($"m_Prefab: null");

                //if (m_LastRaycastPoint.m_OriginalEntity != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(m_LastRaycastPoint.m_OriginalEntity, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(m_LastRaycastPoint.m_OriginalEntity, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: null");

                //if (controlPoint.m_OriginalEntity != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(controlPoint.m_OriginalEntity, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(controlPoint.m_OriginalEntity, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"controlPoint.m_OriginalEntity: null");

                return true;

                //if ((__instance.selectedSnap & Snap.ObjectSurface) == Snap.None)
                //    return true;

                //ControlPoint controlPoint = Traverse.Create(__instance).Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>()[0];

                //if (controlPoint.m_OriginalEntity == Entity.Null || controlPoint.m_OriginalEntity == oldEntity)
                //    return true;

                //if (EL.m_EntityManager.HasBuffer<SubObject>(controlPoint.m_OriginalEntity) || EL.m_EntityManager.HasComponent<Owner>(controlPoint.m_OriginalEntity))
                //    return true;

                //if (EL.m_EntityManager.Exists(oldEntity) &&
                //    EL.m_EntityManager.HasBuffer<SubObject>(oldEntity) &&
                //    EL.m_EntityManager.GetBuffer<SubObject>(oldEntity).Length <= 0)
                //    EL.m_EntityManager.RemoveComponent<SubObject>(oldEntity);

                //oldEntity = controlPoint.m_OriginalEntity;

                //EL.m_EntityManager.AddBuffer<SubObject>(controlPoint.m_OriginalEntity);

                //return true;
            }


            private static Entity GetUpgradable(Entity entity)
            {
                if (EL.m_EntityManager.TryGetComponent<Attached>(entity, out var component))
                {
                    return component.m_Parent;
                }
                return entity;
            }

            public static void Postfix(ObjectToolSystem __instance, ref JobHandle __result, JobHandle inputDeps)
            {
                ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ToolSystem>();
                Traverse traverse = Traverse.Create(__instance);
                NativeList<ControlPoint> controlPoints = traverse.Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>();
                Entity selected = ((__instance.actualMode == Mode.Move) ? traverse.Field("m_MovingObject").GetValue<Entity>() : GetUpgradable(toolSystem.selected));

                __result = IJobExtensions.Schedule(new SnapJob
                {
                    m_EditorMode =              toolSystem.actionMode.IsEditor(),
                    m_Snap =                    traverse.Method("GetActualSnap").GetValue<Snap>(),
                    m_Mode =                    __instance.actualMode,
                    m_Prefab =                  EL.m_PrefabSystem.GetEntity(traverse.Field("m_Prefab").GetValue<PrefabBase>()),
                    m_Selected =                selected,
                    m_LastRaycastPoint =        traverse.Field("m_LastRaycastPoint").GetValue<ControlPoint>(),
                    m_Rotation =                GetRotation(__instance).m_Rotation,
                    m_ControlPoints =           controlPoints,

                    m_OwnerData =               __instance.GetComponentLookup<Owner>(true),
                    m_TransformData =           __instance.GetComponentLookup<Transform>(true),
                    m_LocalTransformCacheData = __instance.GetComponentLookup<LocalTransformCache>(true),
                    m_ServiceUpgradeData =      __instance.GetComponentLookup<Game.Buildings.ServiceUpgrade>(true),
                    m_ObjectGeometryData =      __instance.GetComponentLookup<ObjectGeometryData>(true),
                    m_PrefabRefData =           __instance.GetComponentLookup<PrefabRef>(true),
                }, __result);
            }
        }

        [HarmonyPatch(
            typeof(ObjectToolSystem), nameof(ObjectToolSystem.GetAvailableSnapMask),
                new Type[] { typeof(PlaceableObjectData), typeof(bool), typeof(bool), typeof(bool), typeof(ObjectToolSystem.Mode), typeof(Snap), typeof(Snap) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out }
            )
        ]
        class ObjectToolSystem_GetAvailableSnapMask
        {
            static bool first = true;
            private static void Postfix(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, ObjectToolSystem.Mode mode, ref Snap onMask, ref Snap offMask) //, object[] __args, 
            {
                if (EDT.objectToolSystem.actualMode != ObjectToolSystem.Mode.Create) return;

                if (!isBuilding && (prefabPlaceableData.m_Flags & (PlacementFlags.OwnerSide | PlacementFlags.RoadSide | PlacementFlags.Shoreline | PlacementFlags.Floating | PlacementFlags.Hovering | PlacementFlags.RoadNode | PlacementFlags.RoadEdge)) == PlacementFlags.None)
                {
                    onMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea | Snap.ObjectSide;
                    offMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea | Snap.ObjectSide;

                    if (first)
                    {
                        EDT.objectToolSystem.selectedSnap &= ~(Snap.NetArea | Snap.ObjectSide);
                        first = false;
                    }

                }
            }
        }

        public struct PublicRotation
        {
            public quaternion m_Rotation;
            public quaternion m_ParentRotation;
            public bool m_IsAligned;
            public bool m_IsSnapped;

            public PublicRotation(object privateRotation)
            {
                Type type = privateRotation.GetType();
                m_Rotation = (quaternion)type.GetField("m_Rotation", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_ParentRotation = (quaternion)type.GetField("m_ParentRotation", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_IsAligned = (bool)type.GetField("m_IsAligned", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
                m_IsSnapped = (bool)type.GetField("m_IsSnapped", BindingFlags.Public | BindingFlags.Instance).GetValue(privateRotation);
            }
        }

        // Fonction unique pour récupérer la struct publique
        public static PublicRotation GetRotation(ObjectToolSystem objectToolSystem)
        {
            var field = objectToolSystem.GetType().GetField("m_Rotation", BindingFlags.NonPublic | BindingFlags.Instance);
            var nativeRef = field.GetValue(objectToolSystem); // NativeReference<Rotation>
            object privateRotation = nativeRef.GetType().GetProperty("Value").GetValue(nativeRef);
            return new PublicRotation(privateRotation);
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

            public quaternion m_Rotation;

            public void Execute()
            {
                ControlPoint controlPoint = m_LastRaycastPoint;
                ControlPoint bestSnapPosition = m_ControlPoints[m_ControlPoints.Length - 1];

                //EDT.Logger.Info($"SnapJob Execute");

                //if (m_Selected != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(m_Selected, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"m_Selected: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(m_Selected, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"m_Selected: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"m_Selected: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"m_Selected: null");

                //if (m_Prefab != Entity.Null)
                //    EDT.Logger.Info($"m_Prefab: {EL.m_PrefabSystem.GetPrefab<PrefabBase>(m_Prefab).name}");
                //else
                //    EDT.Logger.Info($"m_Prefab: null");

                //if (m_LastRaycastPoint.m_OriginalEntity != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(m_LastRaycastPoint.m_OriginalEntity, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(m_LastRaycastPoint.m_OriginalEntity, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"m_LastRaycastPoint.m_OriginalEntity: null");

                //if (controlPoint.m_OriginalEntity != Entity.Null)
                //    if (EL.m_PrefabSystem.TryGetPrefab(controlPoint.m_OriginalEntity, out PrefabBase prefabbase))
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: {prefabbase.name}");
                //    else if (EL.m_EntityManager.TryGetComponent<PrefabRef>(controlPoint.m_OriginalEntity, out PrefabRef prefabRef) && EL.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: {prefabBase2.name}");
                //    else
                //        EDT.Logger.Info($"controlPoint.m_OriginalEntity: Failed to get Prefab, Entity: {m_Selected}");
                //else
                //    EDT.Logger.Info($"controlPoint.m_OriginalEntity: null");

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
                        //math.mul(targetTransform.m_Rotation, rotation),
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


    }

}
