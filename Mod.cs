using Colossal.Logging;
using Extra.Lib.Debugger;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System.IO;

namespace ExtraDetailingTools
{
	public class Mod : IMod
	{
		private static readonly ILog log = LogManager.GetLogger($"{nameof(ExtraDetailingTools)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
		internal static Logger Logger { get; private set; } = new(log);

		internal static string ResourcesIcons { get; private set; }
		public void OnLoad(UpdateSystem updateSystem)
		{
			
			Logger.Info(nameof(OnLoad));

			if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset)) return;

			Logger.Info($"Current mod asset at {asset.path}");

			FileInfo fileInfo = new(asset.path);

			ResourcesIcons = Path.Combine(fileInfo.DirectoryName, "Icons");

			EditEntities.SetupEditEntities();
			Icons.LoadIcons(fileInfo.DirectoryName);

		}

		public void OnDispose()
		{
			Logger.Info(nameof(OnDispose));
		}
	}
}
