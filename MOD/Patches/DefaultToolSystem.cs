using Game.Common;
using Game.Tools;
using HarmonyLib;

namespace ExtraDetailingTools.Patches
{
	internal class DefaultToolSystemPatche
	{

		[HarmonyPatch(typeof(DefaultToolSystem), "InitializeRaycast")]
		public class DefaultToolSystem_InitializeRaycast
		{
			public static void Postfix(DefaultToolSystem __instance)
			{
				ToolRaycastSystem toolRaycastSystem = Traverse.Create(__instance).Field("m_ToolRaycastSystem").GetValue<ToolRaycastSystem>();
				toolRaycastSystem.raycastFlags |= RaycastFlags.EditorContainers;
			}

		}
	}
}
