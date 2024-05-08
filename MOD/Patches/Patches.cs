using HarmonyLib;
using System.Reflection;
using Game;
using System.Diagnostics;
using Game.Tools;

namespace ExtraDetailingTools;

[HarmonyPatch(typeof(GameModeExtensions), "IsEditor")]
public class GameModeExtensions_IsEditor
{
	public static void Postfix(ref bool __result) {

		MethodBase caller = new StackFrame(2, false).GetMethod();
		if(
			(caller.DeclaringType == typeof(NetToolSystem) && caller.Name == "GetNetPrefab") ||
			(caller.DeclaringType == typeof(ObjectToolSystem) && caller.Name == "GetObjectPrefab")
			//(caller.DeclaringType == typeof(DefaultToolSystem) && caller.Name == "InitializeRaycast")
			) {
					__result = true;
		}
	}
}
