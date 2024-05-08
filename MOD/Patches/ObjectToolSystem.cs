using System;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;

namespace ExtraDetailingTools;

class ObjectToolSystemPatch {

	[HarmonyPatch(typeof(ObjectToolSystem), nameof(ObjectToolSystem.GetAvailableSnapMask),
		new Type[] { typeof(PlaceableObjectData), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(Snap), typeof(Snap) },
		new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
	class ObjectToolSystem_GetAvailableSnapMask
	{
		private static bool Prefix(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, bool brushing, bool stamping, out Snap onMask, out Snap offMask)
		{
			onMask = Snap.Upright;
			offMask = Snap.None;
			if (EDT.objectToolSystem.actualMode != ObjectToolSystem.Mode.Create) return true;
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.OwnerSide) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.OwnerSide;
			}
			else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
			{
				if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
				{
					onMask |= Snap.NetSide;
					offMask |= Snap.NetSide;
				}
				if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
				{
					onMask |= Snap.Shoreline;
					offMask |= Snap.Shoreline;
				}
				if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
				{
					onMask |= Snap.ObjectSurface;
					offMask |= Snap.ObjectSurface;
				}
			}
			else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadNode | Game.Objects.PlacementFlags.RoadEdge)) != Game.Objects.PlacementFlags.None)
			{
				if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
				{
					onMask |= Snap.NetNode;
				}
				if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadEdge) != Game.Objects.PlacementFlags.None)
				{
					onMask |= Snap.NetArea;
				}
			}
			else if (!isBuilding)
			{
				onMask |= Snap.ObjectSurface | Snap.NetArea;
				offMask |= Snap.ObjectSurface | Snap.NetArea;
				offMask |= Snap.Upright;
			}
			if (editorMode && (!isAssetStamp || stamping))
			{
				onMask |= Snap.AutoParent;
				offMask |= Snap.AutoParent;
			}
			if (brushing)
			{
				onMask &= Snap.Upright;
				offMask &= Snap.Upright;
				onMask |= Snap.PrefabType;
				offMask |= Snap.PrefabType;
			}
			if (isBuilding || isAssetStamp)
			{
				onMask |= Snap.ContourLines;
				offMask |= Snap.ContourLines;
			}

			return false;
		}
	}

	//[HarmonyPatch(typeof(ObjectToolSystem), nameof(ObjectToolSystem.InitializeRaycast))]
 //   class ObjectToolSystem_InitializeRaycast
 //   {
 //       public static void Postfix(DefaultToolSystem __instance)
 //       {
	//		if((__instance.selectedSnap & Snap.ObjectSurface) == Snap.None)
	//		{
 //               ToolRaycastSystem toolRaycastSystem = Traverse.Create(__instance).Field("m_ToolRaycastSystem").GetValue<ToolRaycastSystem>();
 //               toolRaycastSystem.typeMask |= TypeMask.Net;
 //               toolRaycastSystem.netLayerMask |= Layer.All; // (Layer.Road | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad);
 //           }
 //       }
 //   }


}

