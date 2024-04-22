using Game.Prefabs;
using System.IO;

namespace ExtraDetailingTools
{
    internal class Icons
    {
        internal const string IconsResourceKey = "extradetailingtools";
        internal static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

        internal static void LoadIcons(string path)
        {
            Extra.Lib.UI.Icons.LoadIconsFolder(IconsResourceKey, path);
        }

        public static string GetIcon(PrefabBase prefab)
        {

            if (prefab is null) return Extra.Lib.UI.Icons.Placeholder;

            if (File.Exists($"{EDT.ResourcesIcons}/{prefab.GetType().Name}/{prefab.name}.svg")) return $"{COUIBaseLocation}/Icons/{prefab.GetType().Name}/{prefab.name}.svg";

            if (prefab is SurfacePrefab)
            {
                return "Media/Game/Icons/LotTool.svg";
            }
            else if (prefab is UIAssetCategoryPrefab)
            {

                return Extra.Lib.UI.Icons.Placeholder;
            }
            else if (prefab is UIAssetMenuPrefab)
            {

                return Extra.Lib.UI.Icons.Placeholder;
            }

            return Extra.Lib.UI.Icons.Placeholder;
        }

    }
}
