using System;
using System.Reflection;
using Extra.Lib;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace ExtraDetailingTools
{
	class NetToolSystemPatch
	{

		//[HarmonyPatch(typeof(NetToolSystem), nameof(NetToolSystem.prefab), MethodType.Setter)]
		//class NetToolPreferences_OnStartRunning
		//{
		//	static bool first = true;
		//	public static void Postfix(NetToolSystem __instance, NetPrefab value)
		//	{
		//		if (first)
		//		{
		//			__instance.selectedSnap &= ~(Snap.ObjectSurface & Snap.LotGrid);
		//			first = true;
		//		}
		//	}
		//}

		//[HarmonyPatch(typeof(NetToolSystem), "OnStartRunning")]
		//class NetToolPreferences_OnStartRunning
		//{
		//	static bool first = true;
		//	public static void Postfix(NetToolSystem __instance)
		//	{
		//		if (first)
		//		{

		//			//Type[] privateClassType = typeof(NetToolSystem).Assembly.GetTypes();
		//			Type privateClassType = typeof(NetToolSystem).Assembly.GetType("Game.Tools.NetToolSystem+NetToolPreferences");

		//			//foreach (Type privateType in privateClassType) UnityEngine.Debug.Log(privateType.ToString());

		//			var instance = Activator.CreateInstance(privateClassType, true);
		//			var snapInfo = privateClassType.GetField("m_Snap", BindingFlags.Public | BindingFlags.Instance);

		//			Snap snap = (Snap)snapInfo.GetValue(instance);
		//			snap &= ~(Snap.ObjectSurface & Snap.LotGrid);
		//			snapInfo.SetValue(instance, snap);

		//			Traverse netToolSystemTravers = Traverse.Create(__instance);
		//			netToolSystemTravers.Field("m_DefaultToolPreferences").SetValue(instance);
		//			privateClassType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, [__instance]);
		//			//privateClassType.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, [__instance]);

		//			//var instance2 = netToolSystemTravers.Field("m_DefaultToolPreferences").GetValue();

    //        ExtraLib.m_EntityManager.AddBuffer<SubNet>(controlPoint.m_OriginalEntity);

		//			first = true;
		//		}
		//	}
		//}

		//[HarmonyPatch(typeof(NetToolSystem), nameof(NetToolSystem.GetAvailableSnapMask),
		//	new Type[] { typeof(NetGeometryData), typeof(PlaceableNetData), typeof(NetToolSystem.Mode), typeof(bool), typeof(bool), typeof(bool), typeof(Snap), typeof(Snap) },
		//	new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
		//class NetToolSystem_GetAvailableSnapMask
		//{
		//	public static bool Prefix(NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData, NetToolSystem.Mode mode, bool editorMode, bool laneContainer, bool underground, out Snap onMask, out Snap offMask)
		//	{

		//		if (mode == NetToolSystem.Mode.Replace)
		//		{
		//			onMask = Snap.ExistingGeometry;
		//			offMask = onMask;
		//			if ((placeableNetData.m_PlacementFlags & Game.Net.PlacementFlags.UpgradeOnly) == Game.Net.PlacementFlags.None)
		//			{
		//				onMask |= Snap.ContourLines;
		//				offMask |= Snap.ContourLines;
		//			}
		//			if (laneContainer)
		//			{
		//				onMask &= ~Snap.ExistingGeometry;
		//				offMask &= ~Snap.ExistingGeometry;
		//				onMask |= Snap.NearbyGeometry;
		//				return false;
		//			}
		//			if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != (Game.Net.GeometryFlags)0)
		//			{
		//				offMask &= ~Snap.ExistingGeometry;
		//			}
		//			if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.SnapCellSize) != (Game.Net.GeometryFlags)0)
		//			{
		//				onMask |= Snap.CellLength;
		//				offMask |= Snap.CellLength;
		//				return false;
		//			}

		//			return false;
		//		}

		//		onMask = (Snap.ExistingGeometry | Snap.CellLength | Snap.StraightDirection | Snap.ObjectSide | Snap.GuideLines | Snap.ZoneGrid | Snap.ContourLines);
		//		offMask = onMask;
		//		if (underground)
		//		{
		//			onMask &= ~(Snap.ObjectSide | Snap.ZoneGrid);
		//		}
		//		if (laneContainer)
		//		{
		//			onMask &= ~(Snap.CellLength | Snap.ObjectSide);
		//			offMask &= ~(Snap.CellLength | Snap.ObjectSide);
		//		}
		//		else if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.Marker) != (Game.Net.GeometryFlags)0)
		//		{
		//			onMask &= ~Snap.ObjectSide;
		//			offMask &= ~Snap.ObjectSide;
		//		}
		//		if (laneContainer)
		//		{
		//			onMask &= ~Snap.ExistingGeometry;
		//			offMask &= ~Snap.ExistingGeometry;
		//			onMask |= Snap.NearbyGeometry;
		//			offMask |= Snap.NearbyGeometry;
		//		}
		//		else if ((prefabGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != (Game.Net.GeometryFlags)0)
		//		{
		//			offMask &= ~Snap.ExistingGeometry;
		//			onMask |= Snap.NearbyGeometry;
		//			offMask |= Snap.NearbyGeometry;
		//		}

		//		onMask |= Snap.ObjectSurface | Snap.LotGrid;
		//		offMask |= Snap.ObjectSurface | Snap.LotGrid;

		//		if (editorMode)
		//		{
		//			onMask |= Snap.AutoParent;
		//			offMask |= Snap.AutoParent;
		//		}

		//		return false;
		//	}
	}
}