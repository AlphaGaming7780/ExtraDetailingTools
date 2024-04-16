using Extra.Lib;
using Game;
using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ExtraDetailingTools.Patches;

internal class ActionsSectionPatch
{
    [HarmonyPatch(typeof(ActionsSection), "OnProcess")]
    class OnProcess
    {
        public static void Postfix(ActionsSection __instance)
        {
            Traverse traverse = Traverse.Create(__instance);
            Entity selectedEntity = traverse.Property("selectedEntity").GetValue<Entity>();
            bool disableable = traverse.Property("disableable").GetValue<bool>();

            EDT.Logger.Info(selectedEntity);

            traverse.Property("deletable").SetValue(true);
            //traverse.Property("moveable").SetValue(true);
            traverse.Property("disableable").SetValue(disableable || ExtraLib.m_EntityManager.HasComponent<EffectData>(selectedEntity)); // the entity doesn't have this compoenent.
            if(ExtraLib.m_PrefabSystem.TryGetPrefab(selectedEntity, out PrefabBase prefab))
            {
                EDT.Logger.Info(prefab);
            }
        }
    }
}
