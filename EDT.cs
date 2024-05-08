﻿// <copyright file="EDT.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ExtraDetailingTools;

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

public class EDT : IMod
{
    private static readonly ILog log = LogManager.GetLogger($"{nameof(ExtraDetailingTools)}").SetShowsErrorsInUI(false);
#if DEBUG
    internal static Logger Logger = new (log, true);
#else
        internal static Logger Logger = new(log, false);
#endif

    // internal static EffectControlSystem effectControlSystem;

    internal static string ResourcesIcons { get; private set; }

    private Harmony Harmony { get; set; }

    // internal static ToolRaycastSystem toolRaycastSystem;
    internal static ObjectToolSystem objectToolSystem;

    public void OnLoad(UpdateSystem updateSystem)
    {
        Logger.Info(nameof(this.OnLoad));

        // effectControlSystem = updateSystem.World.GetOrCreateSystemManaged<EffectControlSystem>();
        if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset)) return;

        Logger.Info($"Current mod asset at {asset.path}");

        FileInfo fileInfo = new (asset.path);

        ResourcesIcons = Path.Combine(fileInfo.DirectoryName, "Icons");
        Icons.LoadIcons(fileInfo.DirectoryName);

        ExtraLocalization.LoadLocalization(Logger, Assembly.GetExecutingAssembly());
        EditEntities.SetupEditEntities();

        updateSystem.UpdateAt<UI>(SystemUpdatePhase.UIUpdate);

        Harmony = new ($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
        Harmony.PatchAll(typeof(EDT).Assembly);
        var patchedMethods = Harmony.GetPatchedMethods().ToArray();
        Logger.Info($"Plugin ExtraDetailingTools made patches! Patched methods: " + patchedMethods.Length);
        foreach (var patchedMethod in patchedMethods)
        {
            Logger.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
        }

        SelectedInfoUISystem selectedInfoUISystem = updateSystem.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
        selectedInfoUISystem.AddMiddleSection(updateSystem.World.GetOrCreateSystemManaged<TransformSection>());

        // toolRaycastSystem = updateSystem.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
        objectToolSystem = updateSystem.World.GetOrCreateSystemManaged<ObjectToolSystem>();

        // InputAction elevationStepHotKey = new($"ExtraDetailingTools.ElevationStep");
        // elevationStepHotKey.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/shift").With("Button", "<Keyboard>/page up");
        // elevationStepHotKey.performed += test;
        // elevationStepHotKey.Enable();

    }

    public void OnDispose()
    {
        Logger.Info(nameof(OnDispose));
        Harmony.UnpatchAll($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
    }
}
