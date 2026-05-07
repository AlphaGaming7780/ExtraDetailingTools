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

        private GetterValueBinding<bool> m_LocalAxisValueGetter;
        private GetterValueBinding<bool> m_HasSubBuildingsValueGetter;
        private GetterValueBinding<bool> m_MoveSubBuildingsValueGetter;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            AddBinding(m_LocalAxisValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", () => m_TransformGizmoTool.m_UseLocalAxis));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.LocalAxis", new Action<bool>(SetUseLocalAxis)));

            AddBinding(m_HasSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.HasSubBuildings", () => true));
            AddBinding(m_MoveSubBuildingsValueGetter = new GetterValueBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", () => m_TransformGizmoTool.m_MoveSubBuildings));
            AddBinding(new TriggerBinding<bool>("EDT", $"{m_TransformGizmoTool.toolID}.MoveSubBuildings", new Action<bool>(SetMoveSubBuildings)));

            AddBinding(new TriggerBinding<int>("EDT", $"{m_TransformGizmoTool.toolID}.SelectMode", new Action<int>(SetMode)));

        }

        public void SetMode(int mode)
        {
            TransformGizmoTool.Mode tMode = (TransformGizmoTool.Mode)mode;
            EDT.Logger.Info($"Set mode: {tMode}");
            m_TransformGizmoTool.SetMode(tMode);
        }

        public void SetUseLocalAxis(bool enabled)
        {
            EDT.Logger.Info($"SetUseLocalAxis: {enabled}");
            m_TransformGizmoTool.m_UseLocalAxis = enabled;
            m_LocalAxisValueGetter.Update();
        }

        public void SetMoveSubBuildings(bool enabled)
        {
            EDT.Logger.Info($"SetMoveSubBuildings: {enabled}");
            m_TransformGizmoTool.m_MoveSubBuildings = enabled;
            m_MoveSubBuildingsValueGetter.Update();
        }

    }
}
