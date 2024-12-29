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
				toolRaycastSystem.raycastFlags |= RaycastFlags.EditorContainers; // | RaycastFlags.Markers | RaycastFlags.SubElements;
				//toolRaycastSystem.typeMask |= TypeMask.Net;
				//toolRaycastSystem.netLayerMask |= Game.Net.Layer.Fence | Game.Net.Layer.LaneEditor;
				//toolRaycastSystem.netLayerMask &= ~Game.Net.Layer.Road;
				//toolRaycastSystem.utilityTypeMask |= Game.Net.UtilityTypes.Fence;
			}
		}
	}
}
