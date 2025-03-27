// <copyright file="EDT.cs" company="Triton Supreme">
// Copyright (c) Triton Supreme. All rights reserved.
// </copyright>



using Colossal.Logging;
using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Systems;
using ExtraDetailingTools.Systems.Tools;
using ExtraDetailingTools.Systems.UI;
using ExtraLib;
using ExtraLib.Debugger;
using ExtraLib.Helpers;
using Game;
using Game.Modding;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Game.UI.InGame;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ExtraDetailingTools
{
    public class EDT : IMod
    {
        private static readonly ILog log = LogManager.GetLogger($"{nameof(ExtraDetailingTools)}").SetShowsErrorsInUI(false);
#if DEBUG
        internal static Logger Logger = new(log, true);
#else
        internal static Logger Logger = new(log, false);
#endif

        // internal static EffectControlSystem effectControlSystem;

        internal static string ResourcesIcons { get; private set; }

        private Harmony harmony;
        // internal static ToolRaycastSystem toolRaycastSystem;
        internal static ObjectToolSystem objectToolSystem;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logger.Info(nameof(this.OnLoad));

            // effectControlSystem = updateSystem.World.GetOrCreateSystemManaged<EffectControlSystem>();
            if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset)) return;

            Logger.Info($"Current mod asset at {asset.path}");

            FileInfo fileInfo = new(asset.path);

            ResourcesIcons = Path.Combine(fileInfo.DirectoryName, "Icons");
            Icons.LoadIcons(fileInfo.DirectoryName);

            ExtraLocalization.LoadLocalization(Logger, Assembly.GetExecutingAssembly());
            EditEntities.SetupEditEntities();

            updateSystem.UpdateAt<UI>(SystemUpdatePhase.UIUpdate);
            //updateSystem.UpdateAt<BOTSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<EditTempEntitiesSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<TransformObjectSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<GrassToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<GrassSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<GrassRenderSystem>(SystemUpdatePhase.PreCulling);

            SelectedInfoUISystem selectedInfoUISystem = updateSystem.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            selectedInfoUISystem.AddMiddleSection(updateSystem.World.GetOrCreateSystemManaged<TransformSection>());

            // toolRaycastSystem = updateSystem.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
             objectToolSystem = updateSystem.World.GetOrCreateSystemManaged<ObjectToolSystem>();

            //PrefabsHelper.LoadPrefabsInDirectory(Path.Combine(fileInfo.Directory.FullName, "Prefabs"));

            GameGrassPrefab gameGrassPrefab = UnityEngine.ScriptableObject.CreateInstance<GameGrassPrefab>();
            gameGrassPrefab.name = "GameGrassPrefab";
            UIObject uIObject = gameGrassPrefab.AddComponent<UIObject>();
            uIObject.m_Group = PrefabsHelper.GetUIAssetCategoryPrefab("Terraforming");
            uIObject.m_Icon = Icons.GetIcon(gameGrassPrefab);
            EL.m_PrefabSystem.AddPrefab(gameGrassPrefab);

            harmony = new($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
            harmony.PatchAll(typeof(EDT).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            Logger.Info($"Plugin ExtraDetailingTools made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                Logger.Info($"Patched method: {patchedMethod.Module.ScopeName}:{patchedMethod.Name}");
            }
        }

        public void OnDispose()
        {
            Logger.Info(nameof(OnDispose));
            harmony.UnpatchAll($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
        }
    }
}