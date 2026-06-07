using Colossal.UI.Binding;
using ExtraDetailingTools.Systems.Tools;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools.Systems.UI
{
    internal partial class TransformGizmoToolUI : UISystemBase
    {
        private TransformGizmoTool m_TransformGizmoTool;

        private GetterValueBinding<int> m_ToolModeValueGetter;
        private GetterValueBinding<bool> m_LocalAxisValueGetter;
        private GetterValueBinding<bool> m_HasSubBuildingsValueGetter;
        private GetterValueBinding<bool> m_MoveSubBuildingsValueGetter;
        private GetterValueBinding<bool> m_FollowGroundValueGetter;
        private GetterValueBinding<bool> m_SnapToSurfaceValueGetter;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            AddBinding(m_LocalAxisValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", () => m_TransformGizmoTool.m_UseLocalAxis));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", new Action<bool>(SetUseLocalAxis)));

            AddBinding(m_HasSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.HasSubBuildings", () => true));
            AddBinding(m_MoveSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", () => m_TransformGizmoTool.m_MoveSubBuildings));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", new Action<bool>(SetMoveSubBuildings)));

            AddBinding(m_ToolModeValueGetter = new GetterValueBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", () => m_TransformGizmoTool.uiModeIndex));
            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.ToolMode", new Action<int>(SetMode)));

            AddBinding(m_FollowGroundValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.FollowGround", () => m_TransformGizmoTool.m_FollowGround));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.FollowGround", new Action<bool>(SetFollowGround)));
            AddBinding(new TriggerBinding("EDT", $"{m_TransformGizmoTool.toolID}.SnapOnGround", new Action(SnapOnGround)));

            AddBinding(m_SnapToSurfaceValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.SnapToSurface", () => m_TransformGizmoTool.m_SnapToSurface));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.SnapToSurface", new Action<bool>(SetSnapToSurface)));

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
            m_LocalAxisValueGetter.Update();
        }

        public void SetMoveSubBuildings(bool enabled)
        {
            m_TransformGizmoTool.m_MoveSubBuildings = enabled;
            m_MoveSubBuildingsValueGetter.Update();
        }

        public void SetFollowGround(bool enabled)
        {
            m_TransformGizmoTool.m_FollowGround = enabled;
            m_FollowGroundValueGetter.Update();
        }

        public void SetSnapToSurface(bool enabled)
        {
            m_TransformGizmoTool.m_SnapToSurface = enabled;
            m_SnapToSurfaceValueGetter.Update();
        }

        public void SnapOnGround()
        {
            m_TransformGizmoTool.SnapOnGround();
        }

    }
}
