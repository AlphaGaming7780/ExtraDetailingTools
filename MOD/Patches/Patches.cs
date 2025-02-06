using HarmonyLib;
using System.Reflection;
using Game;
using System.Diagnostics;
using Game.Tools;
using Game.Prefabs;
using Colossal.Annotations;
using Game.UI.InGame;

namespace ExtraDetailingTools
{
	[HarmonyPatch(typeof(GameModeExtensions), "IsEditor")]
	public class GameModeExtensions_IsEditor
	{
		public static void Postfix(ref bool __result)
		{

			MethodBase caller = new StackFrame(2, false).GetMethod();
			if (
				(caller.DeclaringType == typeof(NetToolSystem) && caller.Name == "GetNetPrefab") // ||
																								 //(caller.DeclaringType == typeof(ObjectToolSystem) && caller.Name == "GetObjectPrefab") // STRANGE STUFF HAPPEN WHEN RELOACTE BUILDING EXTENSION SINCE 1.1.5f1 GAME VERSION.
																								 //(caller.DeclaringType == typeof(DefaultToolSystem) && caller.Name == "InitializeRaycast")
				)
			{
				__result = true;
			}
		}
	}
}
