// <copyright file="EDT.cs" company="Triton Supreme">
// Copyright (c) Triton Supreme. All rights reserved.
// </copyright>

using Colossal.Core;
using Colossal.Logging;
using ExtraDetailingTools.ExtraSnap;
using ExtraDetailingTools.Gizmos;
using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Systems;
using ExtraDetailingTools.Systems.Tools;
using ExtraDetailingTools.Systems.Tooltip;
using ExtraDetailingTools.Systems.UI;
using ExtraDetailingTools.Systems.UI.BetterInfoPanel;
using ExtraLib;
using ExtraLib.Debugger;
using ExtraLib.Helpers;
using ExtraLib.Systems.UI.ExtraPanels;
using Game;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using Game.UI.InGame;
using Game.UI.Tooltip;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;

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

        internal static string ResourcesIcons { get; private set; }

        private Harmony harmony;
        // internal static ToolRaycastSystem toolRaycastSystem;
        internal static ObjectToolSystem objectToolSystem;

        public void OnLoad(UpdateSystem updateSystem)
        {
            try
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
                updateSystem.UpdateAt<GizmosRenderSystem>(SystemUpdatePhase.Rendering);
                updateSystem.UpdateAt<GizmosRaycastSystem>(SystemUpdatePhase.Raycast);
                updateSystem.UpdateAt<TransformGizmoTool>(SystemUpdatePhase.ToolUpdate);
                updateSystem.UpdateAt<TransformGizmoToolTooltip>(SystemUpdatePhase.UITooltip);
                updateSystem.UpdateAt<TransformGizmoToolUI>(SystemUpdatePhase.UIUpdate);
#if Extra4
                updateSystem.UpdateAfter<TransformObjectSystem>(SystemUpdatePhase.Rendering);
                updateSystem.UpdateAt<GrassToolSystem>(SystemUpdatePhase.ToolUpdate);
                updateSystem.UpdateAt<GrassSystem>(SystemUpdatePhase.ModificationEnd);
                updateSystem.UpdateAt<GrassRenderSystem>(SystemUpdatePhase.PreCulling);
                //ExtraPanelsUISystem extraPanelsUISystem = updateSystem.World.GetOrCreateSystemManaged<ExtraPanelsUISystem>();
                //extraPanelsUISystem.AddExtraPanel<BetterInfoPanelUISystem>();
#endif

                SelectedInfoUISystem selectedInfoUISystem = updateSystem.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
                selectedInfoUISystem.AddMiddleSection(updateSystem.World.GetOrCreateSystemManaged<TransformSection>());

                // toolRaycastSystem = updateSystem.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
                objectToolSystem = updateSystem.World.GetOrCreateSystemManaged<ObjectToolSystem>();
                ExtraSnapBase.RegisterInstance<ObjectToolSystemExtraSnap>();

                //PrefabsHelper.LoadPrefabsInDirectory(Path.Combine(fileInfo.Directory.FullName, "Prefabs"));

#if Extra4
                GameGrassPrefab gameGrassPrefab = UnityEngine.ScriptableObject.CreateInstance<GameGrassPrefab>();
                gameGrassPrefab.name = "GameGrassPrefab";
                UIObject uIObject = gameGrassPrefab.AddComponent<UIObject>();
                uIObject.m_Group = PrefabsHelper.GetUIAssetCategoryPrefab("Terraforming");
                uIObject.m_Icon = Icons.GetIcon(gameGrassPrefab);
                EL.m_PrefabSystem.AddPrefab(gameGrassPrefab);

                //GrassPrefabNew grassPrefabNew = UnityEngine.ScriptableObject.CreateInstance<GrassPrefabNew>();
                //grassPrefabNew.name = "GrassPrefabNew";
                //UIObject uIObject1 = grassPrefabNew.AddComponent<UIObject>();
                //uIObject1.m_Group = PrefabsHelper.GetUIAssetCategoryPrefab("Terraforming");
                //uIObject1.m_Icon = Icons.GetIcon(grassPrefabNew);
                //EL.m_PrefabSystem.AddPrefab(grassPrefabNew);
#endif
                harmony = new($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
                harmony.PatchAll(typeof(EDT).Assembly);
                var patchedMethods = harmony.GetPatchedMethods().ToArray();
                Logger.Info($"Plugin ExtraDetailingTools made patches! Patched methods: " + patchedMethods.Length);
                foreach (var patchedMethod in patchedMethods)
                {
                    Logger.Info($"Patched method: {patchedMethod.Module.ScopeName}:{patchedMethod.Name}");
                }

                MainThreadDispatcher.RegisterUpdater(RegisterToolsToAnarchy);

            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in {nameof(OnLoad)}: {ex}"); Logger.Error(ex.Message);
            }
        }

        public void OnDispose()
        {
            Logger.Info(nameof(OnDispose));
            harmony.UnpatchAll($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
        }

        private void RegisterToolsToAnarchy()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GetName().FullName.Contains("Anarchy,"))
                {
                    continue;
                }

                Type[] types = assembly.GetTypes();

                Type anarchyBridge = assembly.GetTypes().FirstOrDefault(x => x.FullName.Contains("Anarchy.Bridge.AnarchyBridge"));
                if (anarchyBridge is null)
                {
                    Logger.Warn($"Couldn't locate Anarchy Bridge.");
                    continue;
                }

                MethodInfo addToolMethod = anarchyBridge.GetMethod("TryAddToolSystem", BindingFlags.Public | BindingFlags.Static);
                if (addToolMethod is null)
                {
                    Logger.Warn($"Could not find method to add tool.");
                    break;
                }

                var results = addToolMethod.Invoke(null, new object[] { World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransformGizmoTool>() });
                if (results is bool v &&
                    v == true)
                {
                    Logger.Info($"Successfully registered with Anarchy!");
                }
                else
                {
                    Logger.Warn($"Failed to register with Anarchy.");
                }
            }
        }

    }
}