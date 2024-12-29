using System;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;

namespace ExtraDetailingTools;

class AreaToolSystemPatch
{
	[HarmonyPatch(typeof(AreaToolSystem), nameof(AreaToolSystem.GetAvailableSnapMask),
		new Type[] { typeof(AreaGeometryData), typeof(bool), typeof(Snap), typeof(Snap) },
		new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
	class AreaToolSystem_GetAvailableSnapMask
	{
        private static void Postfix(AreaGeometryData prefabAreaData, bool editorMode, ref Snap onMask, ref Snap offMask)
		{

			onMask |= Snap.ExistingGeometry | Snap.StraightDirection | Snap.ContourLines;
			offMask |= Snap.ExistingGeometry | Snap.StraightDirection | Snap.ContourLines;
        }
	}
}