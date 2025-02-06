using System;
using System.Collections.Generic;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Game.Common;

namespace ExtraDetailingTools.Patches
{
	class NetToolSystemPatch
	{

		//[HarmonyPatch(typeof(NetToolSystem), nameof(NetToolSystem.lane), MethodType.Setter)]
		//class NetToolPreferences_set_lane
		//{
		//	public static void Postfix(NetToolSystem __instance, NetPrefab value)
		//	{
		//		if (value == null) return;

		//		Traverse traverse = Traverse.Create(__instance);
		//		traverse.Property("allowReplace").SetValue(true);
		//		EDT.Logger.Info("AllowReplace for NetLane ok");
		//	}
		//}

		//[HarmonyPatch(typeof(NetToolSystem), "UpdateActions")]
		//class NetToolSystem_UpdateActions
		//{
		//	public static bool Prefix(NetToolSystem __instance)
		//	{
		//		PrefabBase prefabBase = __instance.GetPrefab();

		//		if (prefabBase is not NetLanePrefab lanePrefab || __instance.actualMode != NetToolSystem.Mode.Replace) return true;

		//		Traverse traverse = Traverse.Create(__instance);

		//		Traverse<IProxyAction> applyAction = traverse.Property<IProxyAction>("applyAction");
		//		Traverse<IProxyAction> applyActionOverride = traverse.Property<IProxyAction>("applyActionOverride");
		//		Traverse<IProxyAction> secondaryApplyAction = traverse.Property<IProxyAction>("secondaryApplyAction");
		//		Traverse<IProxyAction> secondaryApplyActionOverride = traverse.Property<IProxyAction>("secondaryApplyActionOverride");
		//		Traverse<IProxyAction> cancelAction = traverse.Property<IProxyAction>("cancelAction");
		//		Traverse<IProxyAction> cancelActionOverride = traverse.Property<IProxyAction>("cancelActionOverride");

		//		IProxyAction m_UpgradeNetEdge = traverse.Property<IProxyAction>("m_UpgradeNetEdge").Value;
		//		IProxyAction m_DowngradeNetEdge = traverse.Property<IProxyAction>("m_DowngradeNetEdge").Value;
		//		IProxyAction m_DiscardUpgrade = traverse.Property<IProxyAction>("m_DiscardUpgrade").Value;
		//		IProxyAction m_DiscardDowngrade = traverse.Property<IProxyAction>("m_DiscardDowngrade").Value;
		//		IProxyAction m_ReplaceNetEdge = traverse.Property<IProxyAction>("m_ReplaceNetEdge").Value;
		//		IProxyAction m_DiscardReplace = traverse.Property<IProxyAction>("m_DiscardReplace").Value;

		//		bool actionsEnabled = traverse.Property<bool>("actionsEnabled").Value;
		//		NativeList<ControlPoint> m_ControlPoints = traverse.Field<NativeList<ControlPoint>>("m_ControlPoints").Value;

		//		int m_State = traverse.Property<int>("m_State").Value;

		//		bool flag = m_ControlPoints.Length > 4;
		//		if (lanePrefab.Has<NetUpgrade>())
		//		{
		//			if (!flag)
		//			{
		//				applyAction.Value.enabled = actionsEnabled;
		//				applyActionOverride.Value = m_UpgradeNetEdge;
		//				secondaryApplyAction.Value.enabled = actionsEnabled;
		//				secondaryApplyActionOverride.Value = m_DowngradeNetEdge;
		//				cancelAction.Value.enabled = false;
		//				cancelActionOverride.Value = null;
		//			}
		//			else if (m_State == 1) //NetToolSystem.State.Applying
		//			{
		//				applyAction.Value.enabled = actionsEnabled;
		//				applyActionOverride.Value = m_UpgradeNetEdge;
		//				secondaryApplyAction.Value.enabled = false;
		//				secondaryApplyActionOverride.Value = null;
		//				cancelAction.Value.enabled = actionsEnabled;
		//				cancelActionOverride.Value = m_DiscardUpgrade;
		//			}
		//			else if (m_State == 2) //NetToolSystem.State.Cancelling
		//			{
		//				applyAction.Value.enabled = false;
		//				applyActionOverride.Value = null;
		//				secondaryApplyAction.Value.enabled = actionsEnabled;
		//				secondaryApplyActionOverride.Value = m_DowngradeNetEdge;
		//				cancelAction.Value.enabled = actionsEnabled;
		//				cancelActionOverride.Value = m_DiscardDowngrade;
		//			}
		//		}
		//		else
		//		{
		//			applyAction.Value.enabled = actionsEnabled;
		//			applyActionOverride.Value = m_ReplaceNetEdge;
		//			secondaryApplyAction.Value.enabled = false;
		//			secondaryApplyActionOverride.Value = null;
		//			if (flag)
		//			{
		//				cancelAction.Value.enabled = actionsEnabled;
		//				cancelActionOverride.Value = m_DiscardReplace;
		//			}
		//			else
		//			{
		//				cancelAction.Value.enabled = false;
		//				cancelActionOverride.Value = null;
		//			}
		//		}

		//		return false;

		//	}
		//}

		//[ HarmonyPatch(
		//	typeof(NetToolSystem), "GetRaycastResult", 
		//	new Type[] { typeof(ControlPoint), typeof(bool) },
		//	new ArgumentType[] { ArgumentType.Out, ArgumentType.Out } 
		//)]
		//class NetToolSystem_GetRaycastResult
		//{
		//	public static bool Prefix(NetToolSystem __instance, out ControlPoint controlPoint, out bool forceUpdate)
		//	{
		//		Traverse traverse = Traverse.Create(__instance);

		//		Entity hitEntity = default;
		//		RaycastHit raycastHit = default;
		//		forceUpdate = default;

		//		//Traverse GetRaycastResult = traverse.Method("GetRaycastResult", typeof(Entity), typeof(RaycastHit), typeof(bool) );

		//		bool test = (bool)MethodInvoker.GetHandler(AccessTools.Method(typeof(ToolBaseSystem), "GetRaycastResult", new Type[] { typeof(Entity), typeof(RaycastHit), typeof(bool) })).Invoke(__instance, hitEntity, raycastHit, forceUpdate) ;

		//		//if (GetRaycastResult.GetValue<bool>(new object[] { hitEntity, raycastHit, forceUpdate })) 
		//		if (test) 
		//		{
		//			PlaceableNetData placeableNetData;
		//			if (__instance.actualMode == NetToolSystem.Mode.Replace && ExtraLib.m_EntityManager.HasComponent<Node>(hitEntity) && ExtraLib.m_EntityManager.HasComponent<Edge>(raycastHit.m_HitEntity) && ExtraLib.m_PrefabSystem.TryGetComponentData<PlaceableNetData>(__instance.GetPrefab(), out placeableNetData) && (placeableNetData.m_PlacementFlags & PlacementFlags.NodeUpgrade) == PlacementFlags.None)
		//			{
		//				hitEntity = raycastHit.m_HitEntity;
		//			}
		//			controlPoint = new ControlPoint(hitEntity, raycastHit);
		//			return true;
		//		}
		//		controlPoint = default(ControlPoint);
		//		return false;

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