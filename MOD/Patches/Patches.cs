using HarmonyLib;
using System.Reflection;
using Game;
using System.Diagnostics;
using Game.Tools;
using Game.Prefabs;
using Colossal.Annotations;

namespace ExtraDetailingTools
{
	[HarmonyPatch(typeof(GameModeExtensions), "IsEditor")]
	public class GameModeExtensions_IsEditor
	{
		public static void Postfix(ref bool __result)
		{

		MethodBase caller = new StackFrame(2, false).GetMethod();
		if(
			(caller.DeclaringType == typeof(NetToolSystem) && caller.Name == "GetNetPrefab") // ||
			//(caller.DeclaringType == typeof(ObjectToolSystem) && caller.Name == "GetObjectPrefab")
			//(caller.DeclaringType == typeof(DefaultToolSystem) && caller.Name == "InitializeRaycast")
			) {
                    __result = true;
			}
		}
	}

    //[HarmonyPatch(typeof(ToolSystem), "ActivatePrefabTool")]
    //public class ToolSystem_ActivatePrefabTool
    //{
    //    public static bool Prefix(ToolSystem __instance, ref bool __result, [CanBeNull] PrefabBase prefab)
    //    {
    //        EDT.Logger.Info("ActivatePrefabTool");
    //        if (prefab != null)
    //        {
    //            foreach (ToolBaseSystem toolBaseSystem in __instance.tools)
    //            {
    //                EDT.Logger.Info(toolBaseSystem.GetType().Name);
    //                if (toolBaseSystem.TrySetPrefab(prefab))
    //                {
    //                    EDT.Logger.Info("TRUE");
    //                    __instance.activeTool = toolBaseSystem;
    //                    __result = true;
    //                    return true;
    //                }
    //            }
    //        }
    //        __instance.activeTool = Traverse.Create(__instance).Field("m_DefaultToolSystem").GetValue<DefaultToolSystem>();
    //        __result = false;
    //        return false;
    //    }
    //}

}
