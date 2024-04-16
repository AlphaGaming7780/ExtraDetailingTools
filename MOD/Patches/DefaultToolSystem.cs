using Game.Common;
using Game.Tools;
using Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
