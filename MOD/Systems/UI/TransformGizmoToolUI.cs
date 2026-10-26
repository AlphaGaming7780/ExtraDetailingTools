using Colossal.Entities;
using Colossal.UI.Binding;
using ExtraDetailingTools.Systems.Tools;
using ExtraDetailingTools.Systems.UI.TransformPanel;
using ExtraLib.Systems.UI.ExtraPanels;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        private ValueBinding<bool> m_IsCopyableValueBinding;
        private ValueBinding<bool> m_HasSubBuildingsValueBinding;
        private GetterValueBinding<bool> m_MoveSubBuildingsValueGetter;
        private GetterValueBinding<int> m_XZHandleModeValueGetter;
        private GetterValueBinding<bool> m_UprightValueGetter;
        private GetterValueBinding<int> m_RaycastFilterValueGetter;
        private GetterValueBinding<bool> m_AnarchyAvailableValueGetter;
        private GetterValueBinding<bool> m_AddPreventOverrideValueGetter;
        private GetterValueBinding<bool> m_AddTransformLockValueGetter;

        // Multi selection bindings
        private GetterValueBinding<int> m_PivotAggregationValueGetter;
        private GetterValueBinding<int> m_PivotSourceValueGetter;
        private ValueBinding<int> m_SelectionCountValueBinding;

        // Grid bindings
        private GetterValueBinding<bool> m_GridEnabledValueGetter;
        private GetterValueBinding<float3> m_PosOffsetValueGetter;
        private GetterValueBinding<float3> m_RotOffsetValueGetter;
        private GetterValueBinding<bool> m_PosOffsetLinkedValueGetter;
        private GetterValueBinding<bool> m_RotOffsetLinkedValueGetter;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_TransformGizmoTool.ModeChanged += OnToolModeChanged;
            m_TransformGizmoTool.SelectedEntityChanged += OnSelectedEntityChanged;
            m_TransformGizmoTool.SelectionChanged += OnSelectionChanged;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ExtraPanelsUISystem = World.GetOrCreateSystemManaged<ExtraPanelsUISystem>();
            m_TransformExtraPanel = m_ExtraPanelsUISystem.AddExtraPanel<TransformExtraPanel>();

            m_OpenTransformToolAction = EDT.m_Settings.GetAction(EDT.m_Settings.OpenTransformTool.actionName);
            m_OpenTransformToolAction.shouldBeEnabled = true;

            AddBinding(m_LocalAxisValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", () => m_TransformGizmoTool.m_UseLocalAxis));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", SetUseLocalAxis));

            AddBinding(m_IsCopyableValueBinding = new ValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.IsCopyable", m_TransformGizmoTool.CanDuplicateSelection()));
            AddBinding(m_HasSubBuildingsValueBinding = new ValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.HasSubBuildings", HasSubBuildings(m_TransformGizmoTool.SelectedEntities)));
            AddBinding(m_MoveSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", () => m_TransformGizmoTool.m_MoveSubBuildings));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", SetMoveSubBuildings));

            AddBinding(m_ToolModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", () => m_TransformGizmoTool.uiModeIndex));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", SetMode));

            AddBinding(m_XZHandleModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", () => (int)m_TransformGizmoTool.xzHandleMode));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.XZHandleMode", SetXZHandleMode));
            AddBinding(m_UprightValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.Upright", () => m_TransformGizmoTool.m_Upright));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.Upright", SetUpright));

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
            AddBinding(m_PosOffsetValueGetter = new GetterValueBinding<float3>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffset", () => m_TransformGizmoTool.m_PosOffset));
            AddBinding(m_RotOffsetValueGetter = new GetterValueBinding<float3>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffset", () => m_TransformGizmoTool.m_RotOffset));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.GridEnabled", SetGridEnabled));
            AddBinding(new TriggerBinding<float3>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffset", SetPosOffset));
            AddBinding(new TriggerBinding<float3>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffset", SetRotOffset));
            AddBinding(m_PosOffsetLinkedValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffsetLinked", () => m_TransformGizmoTool.m_PosOffsetLinked));
            AddBinding(m_RotOffsetLinkedValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffsetLinked", () => m_TransformGizmoTool.m_RotOffsetLinked));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.PosOffsetLinked", SetPosOffsetLinked));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.RotOffsetLinked", SetRotOffsetLinked));
            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.PosOffsetFromObject", SetPosOffsetFromObject));

            AddBinding(m_PivotAggregationValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.PivotAggregation", () => (int)m_TransformGizmoTool.pivotAggregation));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.PivotAggregation", SetPivotAggregation));
            AddBinding(m_PivotSourceValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.PivotSource", () => (int)m_TransformGizmoTool.pivotSource));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.PivotSource", SetPivotSource));
            AddBinding(m_SelectionCountValueBinding = new ValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.SelectionCount", m_TransformGizmoTool.SelectionCount));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(m_OpenTransformToolAction.WasPressedThisFrame())
            {
                EnableTransformGizmoTool();
            }
        }

        protected override void OnDestroy()
        {
            m_TransformGizmoTool.ModeChanged -= OnToolModeChanged;
            m_TransformGizmoTool.SelectionChanged -= OnSelectionChanged;
            base.OnDestroy();
        }

        private void OnToolModeChanged(TransformGizmoTool.Mode mode)
        {
            m_ToolModeValueGetter.Update();
        }

        private void OnSelectedEntityChanged(Entity entity)
        {
            m_IsCopyableValueBinding.Update(m_TransformGizmoTool.CanDuplicateSelection());
        }

        private void OnSelectionChanged(IReadOnlyList<Entity> selection)
        {
            m_SelectionCountValueBinding.Update(selection.Count);
            m_IsCopyableValueBinding.Update(m_TransformGizmoTool.CanDuplicateSelection());
            m_HasSubBuildingsValueBinding.Update(HasSubBuildings(selection));
        }

        // MoveSubBuildings only matters when an installed upgrade is a building.
        private bool HasSubBuildings(IReadOnlyList<Entity> selection)
        {
            if (selection == null) return false;
            foreach (Entity entity in selection)
            {
                if (!EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<Game.Buildings.InstalledUpgrade> upgrades)) continue;
                foreach (Game.Buildings.InstalledUpgrade upgrade in upgrades)
                {
                    if (EntityManager.HasComponent<Game.Buildings.Building>(upgrade.m_Upgrade)) return true;
                }
            }
            return false;
        }

        public void EnableTransformGizmoTool()
        {
            m_AnarchyAvailableValueGetter.Update();
            m_ToolSystem.activeTool = m_TransformGizmoTool;
        }

        public void SetMode(TransformGizmoTool.Mode mode)
        {
            m_TransformGizmoTool.SetMode(mode);
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

        public void SetUpright(bool enabled)
        {
            m_TransformGizmoTool.m_Upright = enabled;
            m_UprightValueGetter.Update();
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

        public void SetPosOffset(float3 value)
        {
            m_TransformGizmoTool.m_PosOffset = value;
            m_PosOffsetValueGetter.Update();
        }

        public void SetRotOffset(float3 value)
        {
            m_TransformGizmoTool.m_RotOffset = value;
            m_RotOffsetValueGetter.Update();
        }

        public void SetPosOffsetLinked(bool linked)
        {
            m_TransformGizmoTool.m_PosOffsetLinked = linked;
            m_PosOffsetLinkedValueGetter.Update();
        }

        public void SetRotOffsetLinked(bool linked)
        {
            m_TransformGizmoTool.m_RotOffsetLinked = linked;
            m_RotOffsetLinkedValueGetter.Update();
        }

        // Grid of the size of the primary object. Flat axes (decals...) keep their step.
        // Unlinks different steps, the next edit would make them equal again.
        public void SetPosOffsetFromObject()
        {
            if (!m_TransformGizmoTool.TryGetPrimaryObjectSize(out float3 size)) return;
            float3 step = math.select(m_TransformGizmoTool.m_PosOffset, math.round(size * 1000f) / 1000f, size > 0.001f);
            SetPosOffset(step);
            if (step.x != step.y || step.y != step.z) SetPosOffsetLinked(false);
        }

        public void SetPivotAggregation(int value)
        {
            m_TransformGizmoTool.pivotAggregation = (TransformGizmoTool.PivotAggregation)value;
            m_PivotAggregationValueGetter.Update();
        }

        public void SetPivotSource(int value)
        {
            m_TransformGizmoTool.pivotSource = (TransformGizmoTool.PivotSource)value;
            m_PivotSourceValueGetter.Update();
        }
    }
}
