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

#if RELEASE
using Unity.Burst;
#endif

namespace ExtraDetailingTools.Patches
{
    class ObjectToolSystemPatch
    {

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

        [HarmonyPatch(
            typeof(ObjectToolSystem), nameof(ObjectToolSystem.GetAvailableSnapMask),
                new Type[] { typeof(PlaceableObjectData), typeof(bool), typeof(bool), typeof(bool), typeof(ObjectToolSystem.Mode), typeof(Snap), typeof(Snap) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out }
            )
        ]
        class ObjectToolSystem_GetAvailableSnapMask
        {
            static bool first = true;
            private static void Postfix(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, ObjectToolSystem.Mode mode, ref Snap onMask, ref Snap offMask)
            {
                if (EDT.objectToolSystem.actualMode != ObjectToolSystem.Mode.Create) return;

                if((prefabPlaceableData.m_Flags & PlacementFlags.OwnerSide) == PlacementFlags.None)
                {
                    onMask |= Snap.ObjectSide;
                    offMask |= Snap.ObjectSide;
                }

                if (!isBuilding && (prefabPlaceableData.m_Flags & (PlacementFlags.OwnerSide | PlacementFlags.RoadSide | PlacementFlags.Shoreline | PlacementFlags.Floating | PlacementFlags.Hovering | PlacementFlags.RoadNode | PlacementFlags.RoadEdge)) == PlacementFlags.None)
                {
                    onMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea;
                    offMask |= Snap.ObjectSurface | Snap.Upright | Snap.NetArea;
                }

                if (first)
                {
                    EDT.objectToolSystem.selectedSnap &= ~(Snap.NetArea | Snap.ObjectSide);
                    first = false;
                }

            }
        }
    }
}
