using Colossal.UI.Binding;
using ExtraDetailingTools.Systems.Tools;
using ExtraDetailingTools.Systems.UI.TransformPanel;
using ExtraLib.Systems.UI.ExtraPanels;
using Game.Input;
using Game.Tools;
using Game.UI;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace ExtraDetailingTools.Systems.UI
{
    internal partial class TransformGizmoToolUI : UISystemBase
    {
        private ToolSystem m_ToolSystem;
        private TransformGizmoTool m_TransformGizmoTool;
        private ExtraPanelsUISystem m_ExtraPanelsUISystem;
        private TransformExtraPanel m_TransformExtraPanel;

        private ProxyAction m_OpenTransformToolAction;

        private GetterValueBinding<int> m_ToolModeValueGetter;
        private GetterValueBinding<bool> m_LocalAxisValueGetter;
        private GetterValueBinding<bool> m_HasSubBuildingsValueGetter;
        private GetterValueBinding<bool> m_MoveSubBuildingsValueGetter;
        private GetterValueBinding<int> m_XZHandleModeValueGetter;
        private GetterValueBinding<int> m_RaycastFilterValueGetter;
        private GetterValueBinding<bool> m_AnarchyAvailableValueGetter;
        private GetterValueBinding<bool> m_AddPreventOverrideValueGetter;
        private GetterValueBinding<bool> m_AddTransformLockValueGetter;

        // Grid bindings
        private GetterValueBinding<bool> m_GridEnabledValueGetter;
        private GetterValueBinding<double> m_PosOffsetValueGetter;
        private GetterValueBinding<double> m_RotOffsetValueGetter;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ExtraPanelsUISystem = World.GetOrCreateSystemManaged<ExtraPanelsUISystem>();
            m_TransformExtraPanel = m_ExtraPanelsUISystem.AddExtraPanel<TransformExtraPanel>();

            m_OpenTransformToolAction = EDT.m_Settings.GetAction(EDT.m_Settings.OpenTransformTool.actionName);
            m_OpenTransformToolAction.shouldBeEnabled = true;

            AddBinding(m_LocalAxisValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", () => m_TransformGizmoTool.m_UseLocalAxis));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", SetUseLocalAxis));

            AddBinding(m_HasSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.HasSubBuildings", () => true));
            AddBinding(m_MoveSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", () => m_TransformGizmoTool.m_MoveSubBuildings));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", SetMoveSubBuildings));

            AddBinding(m_ToolModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", () => m_TransformGizmoTool.uiModeIndex));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", SetMode));

            AddBinding(m_XZHandleModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", () => (int)m_TransformGizmoTool.xzHandleMode));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", SetXZHandleMode));

            AddBinding(m_RaycastFilterValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.RaycastFilter", () => (int)m_TransformGizmoTool.raycastFilter));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.RaycastFilter", SetRaycastFilter));

            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.SnapOnGround", SnapOnGround));
            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.Duplicate", Duplicate));

            AddBinding(m_AnarchyAvailableValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.AnarchyAvailable", () => AnarchyBridge.IsAvailable));
            AddBinding(m_AddPreventOverrideValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.AddPreventOverride", () => m_TransformGizmoTool.m_AddPreventOverride));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.AddPreventOverride", SetAddPreventOverride));
            AddBinding(m_AddTransformLockValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.AddTransformLock", () => m_TransformGizmoTool.m_AddTransformLock));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.AddTransformLock", SetAddTransformLock));

            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.SelectTransformGizmosTool", EnableTransformGizmoTool));

            AddBinding(m_GridEnabledValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.GridEnabled", () => m_TransformGizmoTool.m_GridEnabled));
            AddBinding(m_PosOffsetValueGetter = new GetterValueBinding<double>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffset", () => m_TransformGizmoTool.m_PosOffset));
            AddBinding(m_RotOffsetValueGetter = new GetterValueBinding<double>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffset", () => m_TransformGizmoTool.m_RotOffset));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.GridEnabled", SetGridEnabled));
            AddBinding(new TriggerBinding<double>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffset", SetPosOffset));
            AddBinding(new TriggerBinding<double>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffset", SetRotOffset));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(m_OpenTransformToolAction.WasPressedThisFrame())
            {
                EnableTransformGizmoTool();
            }
        }

        public void EnableTransformGizmoTool()
        {
            m_AnarchyAvailableValueGetter.Update();
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

        public void Duplicate()
        {
            m_TransformGizmoTool.Duplicate();
        }

        public void SetAddPreventOverride(bool enabled)
        {
            m_TransformGizmoTool.m_AddPreventOverride = enabled;
            m_AddPreventOverrideValueGetter.Update();
        }

        public void SetAddTransformLock(bool enabled)
        {
            m_TransformGizmoTool.m_AddTransformLock = enabled;
            m_AddTransformLockValueGetter.Update();
        }

        public void SetGridEnabled(bool enabled)
        {
            m_TransformGizmoTool.m_GridEnabled = enabled;
            m_GridEnabledValueGetter.Update();
        }

        public void SetPosOffset(double value)
        {
            m_TransformGizmoTool.m_PosOffset = value;
            m_PosOffsetValueGetter.Update();
        }

        public void SetRotOffset(double value)
        {
            m_TransformGizmoTool.m_RotOffset = value;
            m_RotOffsetValueGetter.Update();
        }
    }
}
