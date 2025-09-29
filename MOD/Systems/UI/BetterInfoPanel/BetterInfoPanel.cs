using ExtraLib.Systems.UI.ExtraPanels;
using Game;

namespace ExtraDetailingTools.Systems.UI.BetterInfoPanel
{
    public partial class BetterInfoPanelUISystem : ExtraPanelBase
    {
        public override GameMode gameMode => GameMode.Game | GameMode.Editor;

        public override string Icon => base.Icon;

        protected override bool m_CanFullScreen => false;

        protected override void OnCreate()
        {
            base.OnCreate();
            EDT.Logger.Info("BetterInfoPanelUISystem OnCreate");
        }

        protected override void OnProcess()
        {

        }
    }
}
