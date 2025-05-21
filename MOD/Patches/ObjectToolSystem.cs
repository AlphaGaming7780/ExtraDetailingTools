using System;
using ExtraLib;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using SubObject = Game.Objects.SubObject;

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


        [HarmonyPatch(typeof(ObjectToolSystem), "SnapControlPoint")]
        class ObjectToolSystem_SnapControlPoint
        {
            static Entity oldEntity = Entity.Null;

            public static bool Prefix(ObjectToolSystem __instance)
            {
                if ((__instance.selectedSnap & Snap.ObjectSurface) == Snap.None) return true;

                ControlPoint controlPoint = Traverse.Create(__instance).Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>()[0];

                if (controlPoint.m_OriginalEntity == Entity.Null || controlPoint.m_OriginalEntity == oldEntity) return true;

                if (EL.m_EntityManager.HasBuffer<SubObject>(controlPoint.m_OriginalEntity) || EL.m_EntityManager.HasComponent<Owner>(controlPoint.m_OriginalEntity)) return true;

                if (EL.m_EntityManager.Exists(oldEntity) && EL.m_EntityManager.HasBuffer<SubObject>(oldEntity) && EL.m_EntityManager.GetBuffer<SubObject>(oldEntity).Length <= 0) EL.m_EntityManager.RemoveComponent<SubObject>(oldEntity);

                oldEntity = controlPoint.m_OriginalEntity;

                EL.m_EntityManager.AddBuffer<SubObject>(controlPoint.m_OriginalEntity);

                return true;
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
                    onMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea;
                    offMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea;

                    if (first)
                    {
                        EDT.objectToolSystem.selectedSnap &= ~(Snap.NetArea);
                        first = false;
                    }

                }
            }
        }

    }

}
