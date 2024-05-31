// <copyright file="EDT.cs" company="Triton Supreme">
// Copyright (c) Triton Supreme. All rights reserved.
// </copyright>



using Colossal.Logging;
using Extra.Lib.Debugger;
using Extra.Lib.Localization;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Tools;
using Game.UI.InGame;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ExtraDetailingTools.Systems;

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
            updateSystem.UpdateAt<EditTempEntitiesSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAfter<TransformObjectSystem>(SystemUpdatePhase.Rendering);

            SelectedInfoUISystem selectedInfoUISystem = updateSystem.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            selectedInfoUISystem.AddMiddleSection(updateSystem.World.GetOrCreateSystemManaged<TransformSection>());

            // toolRaycastSystem = updateSystem.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
            objectToolSystem = updateSystem.World.GetOrCreateSystemManaged<ObjectToolSystem>();

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