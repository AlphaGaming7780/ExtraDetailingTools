using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Extra.Lib;
using Extra.Lib.Helper;
using ExtraDetailingTools.Patches;
using Game.Prefabs;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Entities;

namespace ExtraDetailingTools;

internal partial class ExtraDetailingMenu : UISystemBase
{
    
    public struct AssetCat : IJsonWritable//, IWriter<AssetCat>
    {
        public string name = "Not Good";
        public string icon = Icons.GetIcon(null);
        //internal bool selected = false;

        public AssetCat(string name, string icon)
        {
            this.name = name;
            this.icon = icon;
        }

        public AssetCat()
        {
        }

        public readonly void Write(IJsonWriter writer)
        {
            writer.TypeBegin("AssetCat");
            writer.PropertyName("name");
            writer.Write(name);
            writer.PropertyName("icon");
            writer.Write(icon);
            writer.TypeEnd();
        }

        //readonly void IWriter<AssetCat>.Write(IJsonWriter writer, AssetCat value)
        //{
        //    writer.TypeBegin("AssetCat");
        //    writer.PropertyName("name");
        //    writer.Write(value.name);
        //    writer.PropertyName("icon");
        //    writer.Write(value.icon);
        //    writer.TypeEnd();
        //}
    }

    internal const string CatTabName = "Extra Detailing Assets";

    //private static EntityQuery UIAssetCategoryQuery;
    private static readonly List<AssetCat> assetsCats = [
        //new("Surfaces", $"{Icons.COUIBaseLocation}/resources/Icons/UIAssetCategoryPrefab/Surfaces.svg"),
        //new("Decals", $"{Icons.COUIBaseLocation}/resources/Icons/UIAssetCategoryPrefab/Decals.svg")
    ];
    private static readonly Dictionary<string, List<UIAssetCategoryPrefab>> categories = [];
    private static string selectedCat = null;
    internal static bool showCatTab = false;

    static ValueBinding<AssetCat[]> VB_assetsCats;
    static GetterValueBinding<bool> GVB_ShowCatTab;
    static GetterValueBinding<string> GVB_SelectedCat;

    protected override void OnCreate()
    {

        base.OnCreate();
        AddBinding(VB_assetsCats = new ValueBinding<AssetCat[]>("edt", "assetscat", [..assetsCats], new ArrayWriter<AssetCat>(new ValueWriter<AssetCat>())));
        AddBinding(GVB_ShowCatTab = new GetterValueBinding<bool>("edt", "showcattab", () => showCatTab));
        AddBinding(GVB_SelectedCat = new GetterValueBinding<string>("edt", "selectedtab", () => selectedCat));
        AddBinding(new TriggerBinding<string>("edt", "selectassetcat", new Action<string>(OnAssetCatClick)));

        //UIAssetCategoryQuery = GetEntityQuery(new EntityQueryDesc
        //{
        //    All =
        //       [
        //            ComponentType.ReadOnly<UIAssetCategoryData>()
        //       ],
        //});

    }

    internal static void ShowCatsTab(bool value)
    {
        showCatTab = value;
        if(selectedCat == null) OnAssetCatClick(assetsCats.First().name);
        GVB_ShowCatTab.Update();
    }

    private static void OnAssetCatClick(string assetCatName)
    {
        bool first = true;
        foreach(string assetCat in categories.Keys)
        {
            foreach(UIAssetCategoryPrefab uIAssetCategoryPrefab in categories[assetCat])
            {
                Entity entity = ExtraLib.m_PrefabSystem.GetEntity(uIAssetCategoryPrefab);
                if (assetCat == assetCatName)
                {
                    uIAssetCategoryPrefab.m_Menu.AddElement(entity);
                    if (first)
                    {
                        ToolbarUISystemPatch.SelectCatUI(entity);
                        first = false;
                    }
                } else uIAssetCategoryPrefab.m_Menu.RemoveElement(entity);
            }
        }

        selectedCat = assetCatName;
        GVB_SelectedCat.Update();
        ToolbarUISystemPatch.UpdateMenuUI();
    }

    public static UIAssetCategoryPrefab CreateNewUIAssetCategoryPrefab(string name, Func<PrefabBase, string> getIcons, string assetCatName)
    {
        if (!TryGetAssetCatByName(assetCatName, out AssetCat assetCat)) return null;
        if (!assetsCats.Contains(assetCat)) return null;

        UIAssetMenuPrefab uIAssetMenuPrefab = PrefabsHelper.GetOrCreateNewUIAssetMenuPrefab(CatTabName, Icons.GetIcon, offset: 999);

        UIAssetCategoryPrefab uIAssetCategoryPrefab = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab(uIAssetMenuPrefab, name, getIcons);

        if (categories.ContainsKey(assetCatName)) categories[assetCatName].Add(uIAssetCategoryPrefab);
        else categories.Add(assetCatName, [uIAssetCategoryPrefab]);

        return uIAssetCategoryPrefab;
    }

    public static UIAssetCategoryPrefab CreateNewUIAssetCategoryPrefab(string name, string icon, string assetCatName)
    {
        if(!TryGetAssetCatByName(assetCatName, out AssetCat assetCat)) return null;
        if(!assetsCats.Contains(assetCat)) return null;

        UIAssetMenuPrefab uIAssetMenuPrefab =  PrefabsHelper.GetOrCreateNewUIAssetMenuPrefab(CatTabName, Icons.GetIcon, offset: 999);

        UIAssetCategoryPrefab uIAssetCategoryPrefab = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab(uIAssetMenuPrefab, name, icon);

        if (categories.ContainsKey(assetCatName)) categories[assetCatName].Add(uIAssetCategoryPrefab);
        else categories.Add(assetCatName, [uIAssetCategoryPrefab]);

        return uIAssetCategoryPrefab;
    }

    public static void CreateNewAssetCat(string name, string icon)
    {
        AssetCat assetCat = new(name, icon);
        if(assetsCats.Contains(assetCat)) return;
        //if (assetsCats.Count == 0) selectedCat = name;
        assetsCats.Add(assetCat);
        VB_assetsCats.Update([.. assetsCats]);
    }

    public static bool TryGetAssetCatByName(string name, out AssetCat assetCat)
    {
        assetCat = new();
        foreach(AssetCat a in assetsCats)
        {
            if(a.name != name) continue;
            assetCat = a;
            return true;
        }
        return false;
    }

}
