using System;
using Colossal;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using ExtraDetailingTools.Systems.Tools;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
#if RELEASE
using Unity.Burst;
#endif
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.UI
{
    internal partial class TransformSection : InfoSectionBase
    {
        private TransformPanel.TransformUISystem _transformUISystem;

        private OverlayRenderSystem _overlayRenderSystem;
        private GizmosSystem _gizmosSystem;
        private ToolSystem _toolSystem;
        private TransformGizmoTool _transformGizmoTool;

        private bool showAxis = false;

        protected override string group => "Transform Tool";
        protected override bool displayForUpgrades => true;
        protected override bool displayForDestroyedObjects => true;
        protected override bool displayForOutsideConnections => true;
        protected override bool displayForUnderConstruction => true;

        protected override void OnCreate()
        {
            base.OnCreate();

            _transformUISystem = World.GetOrCreateSystemManaged<TransformPanel.TransformUISystem>();
            _overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            _gizmosSystem = World.GetOrCreateSystemManaged<GizmosSystem>();
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _transformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();

            AddBinding(new TriggerBinding<bool>("EDT", "TransformSection.ShowHighlight", new Action<bool>(ShowHighlight)));
            AddBinding(new TriggerBinding<bool>("EDT", "TransformSection.Opened", new Action<bool>(OnPanelOpened)));
        }

        protected override void OnPreUpdate()
        {
            base.OnPreUpdate();

            if(selectedEntity != _transformUISystem.SelectedEntity)
            {
                _transformUISystem.SetSelectedEntity(selectedEntity);
                RequestUpdate();
            }
            else if (_transformUISystem.NeedUpdate())
            {
                RequestUpdate();
            }
            else
            {
                RenderAxis();
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            visible = EntityManager.HasComponent<Transform>(selectedEntity);
        }

        protected override void OnProcess()
        {
            _transformUISystem.Process();
            RenderAxis();
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
        }

        protected override void Reset() { }

        private void ShowHighlight(bool b)
        {
            if (b && !EntityManager.HasComponent<Highlighted>(selectedEntity))
            {
                EntityManager.AddComponentData(selectedEntity, new Highlighted());
                EntityManager.AddComponentData(selectedEntity, new BatchesUpdated());
                showAxis = true;
            }
            else if (EntityManager.HasComponent<Highlighted>(selectedEntity))
            {
                EntityManager.RemoveComponent<Highlighted>(selectedEntity);
                EntityManager.AddComponentData(selectedEntity, new BatchesUpdated());
                showAxis = false;
            }
        }

        private void OnPanelOpened(bool opened)
        {

        }

        private void RenderAxis()
        {
            if (!showAxis) return;

            var transform = EntityManager.HasComponent<InterpolatedTransform>(selectedEntity) ?
                            EntityManager.GetComponentData<InterpolatedTransform>(selectedEntity).ToTransform() :
                            EntityManager.GetComponentData<Transform>(selectedEntity);

            Bounds3 bounds3 = new(new(0, 0, 0), new(10, 10, 10));

            if (EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) && EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData))
            {
                bounds3 = geometryData.m_Bounds;
            }

            float3 linesLenght = new(bounds3.x.max - bounds3.x.min, bounds3.y.max - bounds3.y.min, bounds3.z.max - bounds3.z.min);

            RenderAxisJob job = new RenderAxisJob()
            {
                m_GizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dep),
                linesLenght = linesLenght,
                transform = transform,
            };
            JobHandle jobHandle = job.Schedule(JobHandle.CombineDependencies(Dependency, dep));
            _gizmosSystem.AddGizmosBatcherWriter(jobHandle);
            Dependency = jobHandle;
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct RenderAxisJob : IJob
        {
            public GizmoBatcher m_GizmoBatcher;
            public Transform transform;
            public float3 linesLenght;

            public void Execute()
            {
                quaternion rot = transform.m_Rotation;
                float3 pos = transform.m_Position;

                float3 xAxis = new float3(1, 0, 0);
                float3 yAxis = new float3(0, 1, 0);
                float3 zAxis = new float3(0, 0, 1);

                xAxis = math.rotate(rot, xAxis);
                yAxis = math.rotate(rot, yAxis);
                zAxis = math.rotate(rot, zAxis);

                xAxis *= linesLenght.x;
                yAxis *= linesLenght.y;
                zAxis *= linesLenght.z;

                m_GizmoBatcher.DrawArrow(pos, pos + xAxis, UnityEngine.Color.red, 0.4f * linesLenght.x);
                m_GizmoBatcher.DrawArrow(pos, pos + yAxis, UnityEngine.Color.green, 0.4f * linesLenght.y);
                m_GizmoBatcher.DrawArrow(pos, pos + zAxis, UnityEngine.Color.blue, 0.4f * linesLenght.z);
            }
        }
    }
}
