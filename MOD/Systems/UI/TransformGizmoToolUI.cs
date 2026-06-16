using Colossal.UI.Binding;
using ExtraDetailingTools.Systems.Tools;
using ExtraDetailingTools.Systems.UI.TransformPanel;
using ExtraLib.Systems.UI.ExtraPanels;
using Game.Tools;
using Game.UI;
using System;

namespace ExtraDetailingTools.Systems.UI
{
    internal partial class TransformGizmoToolUI : UISystemBase
    {
        private ToolSystem m_ToolSystem;
        private TransformGizmoTool m_TransformGizmoTool;
        private ExtraPanelsUISystem m_ExtraPanelsUISystem;
        private TransformExtraPanel m_TransformExtraPanel;

        private GetterValueBinding<int> m_ToolModeValueGetter;
        private GetterValueBinding<bool> m_LocalAxisValueGetter;
        private GetterValueBinding<bool> m_HasSubBuildingsValueGetter;
        private GetterValueBinding<bool> m_MoveSubBuildingsValueGetter;
        private GetterValueBinding<int> m_XZHandleModeValueGetter;
        private GetterValueBinding<int> m_RaycastFilterValueGetter;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ExtraPanelsUISystem = World.GetOrCreateSystemManaged<ExtraPanelsUISystem>();
            m_TransformExtraPanel = m_ExtraPanelsUISystem.AddExtraPanel<TransformExtraPanel>();

            AddBinding(m_LocalAxisValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", () => m_TransformGizmoTool.m_UseLocalAxis));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", new Action<bool>(SetUseLocalAxis)));

            AddBinding(m_HasSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.HasSubBuildings", () => true));
            AddBinding(m_MoveSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", () => m_TransformGizmoTool.m_MoveSubBuildings));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", new Action<bool>(SetMoveSubBuildings)));

            AddBinding(m_ToolModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", () => m_TransformGizmoTool.uiModeIndex));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", new Action<int>(SetMode)));

            AddBinding(m_XZHandleModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", () => (int)m_TransformGizmoTool.xzHandleMode));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", new Action<int>(SetXZHandleMode)));

            AddBinding(m_RaycastFilterValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.RaycastFilter", () => (int)m_TransformGizmoTool.raycastFilter));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.RaycastFilter", new Action<int>(SetRaycastFilter)));

            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.SnapOnGround", new Action(SnapOnGround)));

            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.SelectTransformGizmosTool", EnableTransformGizmoTool));
        }

        public void EnableTransformGizmoTool()
        {
            m_ToolSystem.activeTool = m_TransformGizmoTool;
        }

        public void SetMode(TransformGizmoTool.Mode mode)
        {
            m_TransformGizmoTool.SetMode(mode);
            m_ToolModeValueGetter.Update();
        }

        public void SetMode(int mode)
        {
            SetMode((TransformGizmoTool.Mode)mode);
        }

        public void SetUseLocalAxis(bool enabled)
        {
            m_TransformGizmoTool.m_UseLocalAxis = enabled;
            m_TransformExtraPanel.RequestUpdate();
            m_LocalAxisValueGetter.Update();
        }

        public void SetMoveSubBuildings(bool enabled)
        {
            m_TransformGizmoTool.m_MoveSubBuildings = enabled;
            m_TransformExtraPanel.RequestUpdate();
            m_MoveSubBuildingsValueGetter.Update();
        }

        public void SetXZHandleMode(int value)
        {
            SetXZHandleMode((TransformGizmoTool.XZHandleMode)value);
        }

        public void SetXZHandleMode(TransformGizmoTool.XZHandleMode value)
        {
            m_TransformGizmoTool.xzHandleMode = value;
            m_XZHandleModeValueGetter.Update();
        }

        public void SetRaycastFilter(int value)
        {
            SetRaycastFilter((TransformGizmoTool.RaycastFilter)value);
        }

        public void SetRaycastFilter(TransformGizmoTool.RaycastFilter value)
        {
            m_TransformGizmoTool.raycastFilter = value;
            m_RaycastFilterValueGetter.Update();
        }

        public void SnapOnGround()
        {
            m_TransformGizmoTool.SnapOnGround();
        }

    }
}
