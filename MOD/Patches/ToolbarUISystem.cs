using Colossal.UI.Binding;
using Extra;
using Extra.Lib;
using Game.Prefabs;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ExtraDetailingTools.Patches;

public class ToolbarUISystemPatch
{

    [HarmonyPatch(typeof(ToolbarUISystem), "SelectAssetMenu")]
    class SelectAssetMenu
    {
        static void Postfix(Entity assetMenu)
        {

            if (assetMenu != Entity.Null && ExtraLib.m_EntityManager.HasComponent<UIAssetMenuData>(assetMenu))
            {
                ExtraLib.m_PrefabSystem.TryGetPrefab(assetMenu, out PrefabBase prefabBase);

                ExtraDetailingMenu.ShowCatsTab(prefabBase is UIAssetMenuPrefab && prefabBase.name == ExtraDetailingMenu.CatTabName);
            }
        }
    }

    //public static void UpdateCatUI()
    //{
    //    Traverse.Create(ExtraLib.m_ToolbarUISystem).Field("m_AssetMenuCategoriesBinding").GetValue<RawMapBinding<Entity>>().UpdateAll();
    //}

    public static void UpdateMenuUI()
    {
        Traverse.Create(ExtraLib.m_ToolbarUISystem).Field("m_AssetMenuCategoriesBinding").GetValue<RawMapBinding<Entity>>().UpdateAll();
    }

    internal static void SelectCatUI(Entity entity)
    {
        Traverse.Create(ExtraLib.m_ToolbarUISystem).Method("SelectAssetCategory", [typeof(Entity)]).GetValue(entity);
    }

    internal static void SelectMenuUI(Entity entity)
    {
        Traverse.Create(ExtraLib.m_ToolbarUISystem).Method("SelectAssetMenu", [typeof(Entity)]).GetValue(entity);
    }

}
