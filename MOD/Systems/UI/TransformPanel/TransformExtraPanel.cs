using ExtraDetailingTools.Systems.Tools;
using ExtraLib.Systems.UI.ExtraPanels;
using Game;
using Game.Tools;
using Unity.Mathematics;

namespace ExtraDetailingTools.Systems.UI.TransformPanel
{
    internal partial class TransformExtraPanel : ExtraPanelBase
    {
        public override GameMode gameMode => GameMode.Game | GameMode.Editor;

        public override string Icon => "coui://extradetailingtools/Icons/TransformGizmosTool/Icon.svg";

        protected override bool m_ShowInSelector => false;
        protected override bool m_CanFullScreen => false;

        public override float2 PanelMinSize => new float2(400, 165+48);

        private ToolSystem m_ToolSystem;
        private TransformGizmoTool m_TransformGizmoTool;
        private TransformGizmoToolUI m_TransformGizmoToolUI;
        private TransformUISystem m_TransformUISystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            //SetPanelLocation(new float2(0.01f, 0.95f));
            EDT.Logger.Info("TransformPanel OnCreate");
            m_ToolSystem = World.GetExistingSystemManaged<ToolSystem>();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_TransformGizmoToolUI = World.GetOrCreateSystemManaged<TransformGizmoToolUI>();
            m_TransformUISystem = World.GetOrCreateSystemManaged<TransformUISystem>();
            SetPanelSize( new float2(400, 165+48) );
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            SetVisible(m_TransformGizmoTool == m_ToolSystem.activeTool);
        }

        protected override void OnPreProcess()
        {
            base.OnPreProcess();
            if (m_TransformGizmoTool.SelectedEntity != m_TransformUISystem.SelectedEntity)
            {
                m_TransformUISystem.SetSelectedEntity(m_TransformGizmoTool.SelectedEntity);
                RequestUpdate();
            }
            else if (m_TransformUISystem.NeedUpdate()) 
                RequestUpdate();
        }

        protected override void OnProcess()
        {
            m_TransformUISystem.Process();
        }
    }
}
