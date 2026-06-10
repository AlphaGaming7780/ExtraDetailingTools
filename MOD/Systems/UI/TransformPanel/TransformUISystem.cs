using ExtraDetailingTools.Systems.Tools;
using Game;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ExtraDetailingTools.Systems.UI.TransformPanel
{
    internal partial class TransformUISystem : UISystemBase
    {
        private TransformGizmoTool m_TransformGizmoTool;
        private EndFrameBarrier m_EndFrameBarrier;
        private Entity m_SelectedEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }

        public bool NeedUpdate()
        {
            return EntityManager.HasComponent<InterpolatedTransform>(m_SelectedEntity);
        }

        private void UpdateObject(float3 position, quaternion rotation)
        {
            m_TransformGizmoTool.UpdateObject(Dependency, m_SelectedEntity, position, rotation, m_EndFrameBarrier);
        }
    }
}
