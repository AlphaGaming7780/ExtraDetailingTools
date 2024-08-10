using Game.Areas;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;

namespace ExtraDetailingTools.Tools
{
    internal partial class AreaReplacerTool : ToolBaseSystem
    {
        public override string toolID => "AreaReplacerTool";

        private AreaPrefab _prefab;

        private ProxyAction m_ApplyAction;
        private ProxyAction m_SecondaryApplyAction;

        protected override void OnCreate()
        {
            base.OnCreate();

            Enabled = false;

            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");

            int index = 0;
            foreach (ToolBaseSystem tool in m_ToolSystem.tools)
            {
                if (tool is AreaToolSystem)
                {
                    m_ToolSystem.tools.Insert(index, this);
                    break;
                }
                index++;
            }
        }

        public override PrefabBase GetPrefab()
        {
            return _prefab;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            if (prefab is not AreaPrefab) return false;

            _prefab = (AreaPrefab)prefab;

            EDT.Logger.Info(_prefab.name);

            return true;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            
            //m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.Surfaces | AreaTypeMask.Spaces;
            //m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround;
            //m_ToolRaycastSystem.typeMask = TypeMask.Areas;

            AreaGeometryData componentData = this.m_PrefabSystem.GetComponentData<AreaGeometryData>(_prefab);
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
            m_ToolRaycastSystem.typeMask = TypeMask.Areas;
            m_ToolRaycastSystem.areaTypeMask = AreaUtils.GetTypeMask(componentData.m_Type);

        }

        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_FocusChanged)
            {
                return inputDeps;
            }
            UpdateActions();
            AreaGeometryData componentData = this.m_PrefabSystem.GetComponentData<AreaGeometryData>(_prefab);
            requireAreas = AreaUtils.GetTypeMask(componentData.m_Type);
            //requireAreas = AreaTypeMask.Spaces | AreaTypeMask.Surfaces;
            bool raycastFlag = GetRaycastResult(out Entity e, out RaycastHit hit);
            if (raycastFlag)
            {
                EDT.Logger.Info("Yeah");

                PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(e);
                prefabRef.m_Prefab = m_PrefabSystem.GetEntity(_prefab);
                EntityManager.SetComponentData(e, prefabRef);
                EntityManager.AddComponentData<Updated>(e, new());
                //EntityManager.AddComponentData<BatchesUpdated>(e, new());
            }

            return inputDeps;
        }

        private void UpdateActions()
        {
            m_ApplyAction.shouldBeEnabled = true;
            m_SecondaryApplyAction.shouldBeEnabled = true;
            //this.m_ApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
            //this.m_SecondaryApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
            //this.m_SecondaryApplyDisplayOverride.displayName = ((this.m_State == AreaToolSystem.State.Create && this.m_ControlPoints.Length > 1) ? "Undo Area Node" : "Remove Area Node");
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            UpdateActions();
        }

        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
            m_SecondaryApplyAction.shouldBeEnabled= false;
            base.OnStopRunning();
        }
    }
}
