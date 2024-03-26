using Colossal.Logging;
using Extra.Lib.Debugger;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace ExtraDetailingTools
{
    public class Mod : IMod
    {
        private static readonly ILog log = LogManager.GetLogger($"{nameof(ExtraDetailingTools)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        internal static Logger Logger { get; private set; } = new(log);
        public void OnLoad(UpdateSystem updateSystem)
        {
            
            Logger.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                Logger.Info($"Current mod asset at {asset.path}");

            EditEntities.SetupEditEntities();

        }

        public void OnDispose()
        {
            Logger.Info(nameof(OnDispose));
        }
    }
}
