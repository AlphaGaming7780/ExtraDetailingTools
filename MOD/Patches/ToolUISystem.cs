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
using Game.UI.InGame;
using ExtraLib;
using ExtraDetailingTools.Systems.Tools;

namespace ExtraDetailingTools.Patches
{
    class ToolUISystemPatches
    {
        [HarmonyPatch(typeof(ToolUISystem), "AllowBrush")]
        public class AllowBrush
        {
            public static void Postfix(ref bool __result)
            {
                if( GrassToolSystem.s_Instance.toolSystem.activeTool == GrassToolSystem.s_Instance )
                {
                    __result = true;
                }
            }
        }

    }
}
