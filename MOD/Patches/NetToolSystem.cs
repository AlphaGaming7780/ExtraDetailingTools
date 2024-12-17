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

namespace ExtraDetailingTools;

class NetToolSystemPatch {

    //[HarmonyPatch(typeof(NetToolSystem), nameof(NetToolSystem.prefab), MethodType.Setter)]
    //class NetToolSystem_prefab
    //{
    //    static bool first = true;
    //    public static void Postfix(NetToolSystem __instance, NetPrefab value)
    //    {
    //        if (first)
    //        {
    //            __instance.selectedSnap &= ~(Snap.ObjectSurface & Snap.LotGrid);
    //            first = true;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(NetToolSystem), "SnapControlPoints")]
    //class NetToolSystem_SnapControlPoints
    //{
    //    static Entity oldEntity = Entity.Null;

    //    public static bool Prefix(NetToolSystem __instance)
    //    {
    //        if ((__instance.selectedSnap & Snap.ObjectSurface) == Snap.None) return true;

    //        ControlPoint controlPoint = Traverse.Create(__instance).Field("m_ControlPoints").GetValue<NativeList<ControlPoint>>()[0];

    //        if (controlPoint.m_OriginalEntity == Entity.Null || controlPoint.m_OriginalEntity == oldEntity) return true;

    //        if (ExtraLib.m_EntityManager.Exists(oldEntity) && ExtraLib.m_EntityManager.HasBuffer<SubNet>(oldEntity) && ExtraLib.m_EntityManager.GetBuffer<SubNet>(oldEntity).Length <= 0) ExtraLib.m_EntityManager.RemoveComponent<SubNet>(oldEntity);

    //        if (ExtraLib.m_EntityManager.HasBuffer<SubNet>(controlPoint.m_OriginalEntity) || ExtraLib.m_EntityManager.HasComponent<Owner>(controlPoint.m_OriginalEntity)) return true;

    //        oldEntity = controlPoint.m_OriginalEntity;

    //        ExtraLib.m_EntityManager.AddBuffer<SubNet>(controlPoint.m_OriginalEntity);

    //        return true;
    //    }
    //}

//    [HarmonyPatch(
//typeof(NetToolSystem), nameof(NetToolSystem.GetAvailableSnapMask),
//    new Type[] { typeof(NetGeometryData), typeof(PlaceableNetData), typeof(NetToolSystem.Mode), typeof(bool), typeof(bool), typeof(bool), typeof(Snap), typeof(Snap) },
//    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out }
//    )
//]
//    class NetToolSystem_GetAvailableSnapMask
//    {
//        private static void Postfix(NetGeometryData prefabGeometryData, PlaceableNetData placeableNetData, NetToolSystem.Mode mode, bool editorMode, bool laneContainer, bool underground, ref Snap onMask, ref Snap offMask)
//        {

//            if (mode == NetToolSystem.Mode.Replace) return;

//            //onMask |= (Snap.ObjectSurface | Snap.Upright | Snap.LotGrid | Snap.NetArea);
//            //offMask |= (Snap.ObjectSurface | Snap.Upright | Snap.LotGrid | Snap.NetArea);

//            onMask |= (Snap.NetMiddle | Snap.NetSide | Snap.NetNode | Snap.NetArea);
//            offMask |= (Snap.NetMiddle | Snap.NetSide | Snap.NetNode | Snap.NetArea);

//        }
//    }

    //[HarmonyPatch(typeof(NetToolSystem), "OnStartRunning")]
    //class NetToolSystem_OnStartRunning
    //{
    //    static bool first = true;
    //    public static void Postfix(NetToolSystem __instance)
    //    {
    //        if (first)
    //        {

    //            //Type[] privateClassType = typeof(NetToolSystem).Assembly.GetTypes();
    //            Type privateClassType = typeof(NetToolSystem).Assembly.GetType("Game.Tools.NetToolSystem+NetToolPreferences");

    //            //foreach (Type privateType in privateClassType) UnityEngine.Debug.Log(privateType.ToString());

    //            var instance = Activator.CreateInstance(privateClassType, true);
    //            var snapInfo = privateClassType.GetField("m_Snap", BindingFlags.Public | BindingFlags.Instance);

    //            Snap snap = (Snap)snapInfo.GetValue(instance);
    //            snap &= ~(Snap.ObjectSurface | Snap.LotGrid);
    //            snapInfo.SetValue(instance, snap);

    //            Traverse netToolSystemTravers = Traverse.Create(__instance);
    //            netToolSystemTravers.Field("m_DefaultToolPreferences").SetValue(instance);
    //            privateClassType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, [__instance]);
    //            //privateClassType.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, [__instance]);

    //            //var instance2 = netToolSystemTravers.Field("m_DefaultToolPreferences").GetValue();


    //            first = true;
    //        }

    //    }
    //}

}