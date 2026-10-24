using ExtraDetailingTools.Systems.Tools;
using ExtraLib.Systems.UI.ExtraPanels;
using Game;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;

namespace ExtraDetailingTools.Systems.UI.TransformPanel
{
    internal partial class TransformExtraPanel : ExtraPanelBase
    {
        public override GameMode gameMode => GameMode.Game | GameMode.Editor;

        public override string Icon => "coui://extradetailingtools/Icons/TransformGizmosTool/Icon.svg";

        protected override bool m_ShowInSelector => true;
        protected override bool m_CanFullScreen => false;

        public override float2 PanelMinSize => new float2(400, 165);

        private ToolSystem m_ToolSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private TransformGizmoTool m_TransformGizmoTool;
        private TransformUISystem m_TransformUISystem;

        protected override void OnCreate()
        {   
            base.OnCreate();
            EDT.Logger.Info("TransformPanel OnCreate");
            m_ToolSystem = World.GetExistingSystemManaged<ToolSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_TransformUISystem = World.GetOrCreateSystemManaged<TransformUISystem>();
            m_TransformGizmoTool.SelectedEntityChanged += OnSelectedEntityChanged;
            SetPanelSize( new float2(400, 165+48) );
        }

        protected override void OnDestroy()
        {
            m_TransformGizmoTool.SelectedEntityChanged -= OnSelectedEntityChanged;
            base.OnDestroy();
        }

        private void OnSelectedEntityChanged(Entity entity)
        {
            m_TransformUISystem.SetSelectedEntity(entity);
            RequestUpdate();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            SetVisible(m_TransformGizmoTool == m_ToolSystem.activeTool);
        }

        public override void OnOpen()
        {
            m_ToolSystem.activeTool = m_TransformGizmoTool;
            base.OnOpen();
        }

        public override void OnClose()
        {
            if (m_TransformGizmoTool == m_ToolSystem.activeTool)
                m_ToolSystem.activeTool = m_DefaultToolSystem;
            base.OnClose();
        }

        protected override void OnPreProcess()
        {
            base.OnPreProcess();
            if (m_TransformUISystem.NeedUpdate())
                RequestUpdate();
        }

        protected override void OnProcess()
        {
            m_TransformUISystem.Process();
        }
    }
}
