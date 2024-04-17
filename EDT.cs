using Colossal.Logging;
using Extra.Lib.Debugger;
using Extra.Lib.Localization;
using Game;
using Game.Effects;
using Game.Modding;
using Game.SceneFlow;
using Game.Tools;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ExtraDetailingTools
{
    public class EDT : IMod
	{
		private static readonly ILog log = LogManager.GetLogger($"{nameof(ExtraDetailingTools)}").SetShowsErrorsInUI(false);
		internal static Logger Logger { get; private set; } = new(log, true);

		internal static EffectControlSystem effectControlSystem;

		internal static string ResourcesIcons { get; private set; }

		private Harmony harmony;
		internal static ToolRaycastSystem toolRaycastSystem;

		public void OnLoad(UpdateSystem updateSystem)
		{
			Logger.Info(nameof(OnLoad));

            effectControlSystem = updateSystem.World.GetOrCreateSystemManaged<EffectControlSystem>();

            if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset)) return;

			Logger.Info($"Current mod asset at {asset.path}");

			FileInfo fileInfo = new(asset.path);

			ResourcesIcons = Path.Combine(fileInfo.DirectoryName, "Icons");
            Icons.LoadIcons(fileInfo.DirectoryName);

			ExtraLocalization.LoadLocalization(Logger, Assembly.GetExecutingAssembly(), false);
            EditEntities.SetupEditEntities();
			
            updateSystem.UpdateAt<UI>(SystemUpdatePhase.UIUpdate);

			harmony = new($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
			harmony.PatchAll(typeof(EDT).Assembly);
			var patchedMethods = harmony.GetPatchedMethods().ToArray();
			Logger.Info($"Plugin ExtraDetailingTools made patches! Patched methods: " + patchedMethods.Length);
			foreach (var patchedMethod in patchedMethods)
			{
				Logger.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
			}

        }

		public void OnDispose()
		{
			Logger.Info(nameof(OnDispose));
			harmony.UnpatchAll($"{nameof(ExtraDetailingTools)}.{nameof(EDT)}");
		}
	}
}
