using ExtraLib.Systems.UI.ExtraPanels;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools.Systems.UI.TransformPanel
{
    internal partial class TransformExtraPanel : ExtraPanelBase
    {
        public override GameMode gameMode => GameMode.Game | GameMode.Editor;

        public override string Icon => "coui://extradetailingtools/Icons/TransformGizmosTool/Icon.svg";

        protected override bool m_CanFullScreen => false;

        protected override void OnCreate()
        {
            base.OnCreate();
            EDT.Logger.Info("TransformPanel OnCreate");
        }

        protected override void OnProcess()
        {

        }
    }
}
