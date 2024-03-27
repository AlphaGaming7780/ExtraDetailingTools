using Game.Prefabs;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ExtraDetailingTools.Decals
{
    public struct DecalData : IComponentData, IQueryTypeParameter
    {
    }

    [HarmonyPatch(typeof(DecalProperties), "GetPrefabComponents")]
    class DecalProperties_GetPrefabComponents
    {
        static void Postfix(DecalProperties __instance, HashSet<ComponentType> components)
        {
            __instance.GetPrefabComponents(components);
            components.Add(ComponentType.ReadWrite<DecalData>());
        }
    }

}
