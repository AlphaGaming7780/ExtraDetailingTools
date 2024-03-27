using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools
{
    internal class Icons
    {
        internal const string IconsResourceKey = "extradetailingtools";
        internal static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

        public static readonly string Placeholder = $"{COUIBaseLocation}/Icons/Misc/placeholder.svg";
        public static readonly string GameCrashWarning = $"{COUIBaseLocation}/Icons/Misc/GameCrashWarning.svg";

        internal static void LoadIcons(string path)
        {
            Extra.Lib.UI.Icons.LoadIconsFolder(IconsResourceKey, path);
        }

        public static string GetIcon(PrefabBase prefab)
        {

            if (prefab is null) return $"{COUIBaseLocation}/Icons/Misc/placeholder.svg";

            if (File.Exists($"{EDT.ResourcesIcons}/{prefab.GetType().Name}/{prefab.name}.svg")) return $"{COUIBaseLocation}/Icons/{prefab.GetType().Name}/{prefab.name}.svg";

            if (prefab is SurfacePrefab)
            {


                return "Media/Game/Icons/LotTool.svg";

            }
            else if (prefab is UIAssetCategoryPrefab)
            {

                return $"{COUIBaseLocation}/Icons/Misc/placeholder.svg";
            }
            else if (prefab is UIAssetMenuPrefab)
            {

                return $"{COUIBaseLocation}/Icons/Misc/placeholder.svg";
            }
            else if (prefab.name.ToLower().Contains("decal") || prefab.name.ToLower().Contains("roadarrow"))
            {

            }

            return Placeholder;
        }

    }
}
