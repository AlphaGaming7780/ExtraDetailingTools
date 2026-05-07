using Colossal;
using Colossal.Entities;
using Colossal.Internal.Gizmos;
using ExtraDetailingTools.Systems.Tools;
using Game.Areas;
using Game.Objects;
using Game.Tools;
using Game.UI.Tooltip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tooltip
{
    public partial class TransformGizmoToolTooltip : TooltipSystemBase
    {
        private ToolSystem m_ToolSystems;
        private TransformGizmoTool m_TransformGizmoTool;

        private GizmosSystem m_GizmosSystem;

        private FloatTooltip m_Distance;

        private FloatTooltip m_Angle;

        private FloatTooltip m_Scale;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystems = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TransformGizmoTool = World.GetExistingSystemManaged<TransformGizmoTool>();

            m_Distance = new FloatTooltip
            {
                icon = "Media/Glyphs/Length.svg",
                unit = "length",
                signed = false,
            };
            m_Angle = new FloatTooltip
            {
                icon = "Media/Glyphs/Angle.svg",
                //unit = "degree",
                signed = false
            };
            m_Scale = new FloatTooltip
            {
                icon = "Media/Glyphs/Length.svg",
                unit = "percentageSingleFraction",
                signed = false,
            };
            Enabled = true;
        }

        protected override void OnUpdate()
        {
            if (m_ToolSystems.activeTool != m_TransformGizmoTool || !(Camera.main != null))
                return;

            if (m_TransformGizmoTool.SelectedTempEntity == Entity.Null || m_TransformGizmoTool.SelectedEntity == Entity.Null)
                return;

            if (!EntityManager.TryGetComponent<Transform>(m_TransformGizmoTool.SelectedEntity, out Transform ogTransform) || !EntityManager.TryGetComponent<Transform>(m_TransformGizmoTool.SelectedTempEntity, out Transform tempTransform))
                return;

            //CompleteDependency();

            if (m_TransformGizmoTool.mode == TransformGizmoTool.Mode.Move)
            {
                m_Distance.value = math.distance(tempTransform.m_Position, ogTransform.m_Position);
                AddMouseTooltip(m_Distance);
            }

            else if (m_TransformGizmoTool.mode == TransformGizmoTool.Mode.Rotate)
            {
                m_Angle.value = math.degrees(math.angle(tempTransform.m_Rotation, ogTransform.m_Rotation));
                AddMouseTooltip(m_Angle);
            }

            else if(m_TransformGizmoTool.mode == TransformGizmoTool.Mode.Scale)
            {
                AddMouseTooltip(m_Scale);
            }
        }
    }
}
