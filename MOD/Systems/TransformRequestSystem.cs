using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Systems.Tools;
using Game;
using Game.Common;
using Game.Tools;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems
{
    // Transform changes asked by the UI (Transform panel of the gizmo tool and of the SIP). The UI runs after the
    // Modification phases, so it only queues them. They're applied here, in ToolUpdate whatever the active tool, through
    // the ToolOutputBarrier like a tool: the Modification phases handle the Updated entities in the same frame (bounds,
    // culling, search tree...).
    internal partial class TransformRequestSystem : GameSystemBase
    {
        private struct Request
        {
            public Entity m_Entity;
            public bool m_Group; // The whole selection of TransformGizmoTool follows m_Entity (its primary entity).
            public bool m_HasTransform;
            public Transform m_Transform;
            public bool m_HasScale;
            public float3 m_Scale;
        }

        private TransformGizmoTool m_TransformGizmoTool;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private TransformGizmoTool.UpdateObjectLookups m_UpdateObjectLookups;

        // One per entity. The targets are absolute, so a newer request replaces the older one.
        private readonly List<Request> m_Requests = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_UpdateObjectLookups = new TransformGizmoTool.UpdateObjectLookups(ref CheckedStateRef);
        }

        public void RequestTransform(Entity entity, bool group, Transform transform)
        {
            Request request = GetRequest(entity, out int index);
            request.m_Group = group;
            request.m_HasTransform = true;
            request.m_Transform = transform;
            SetRequest(index, request);
        }

        public void RequestScale(Entity entity, float3 scale)
        {
            Request request = GetRequest(entity, out int index);
            request.m_HasScale = true;
            request.m_Scale = scale;
            SetRequest(index, request);
        }

        private Request GetRequest(Entity entity, out int index)
        {
            index = m_Requests.FindIndex(r => r.m_Entity == entity);
            return index >= 0 ? m_Requests[index] : new Request { m_Entity = entity };
        }

        private void SetRequest(int index, Request request)
        {
            if (index >= 0) m_Requests[index] = request;
            else m_Requests.Add(request);
        }

        protected override void OnUpdate()
        {
            if (m_Requests.Count == 0) return;

            m_UpdateObjectLookups.Update(ref CheckedStateRef);

            foreach (Request request in m_Requests)
            {
                if (!EntityManager.Exists(request.m_Entity) || !EntityManager.HasComponent<Transform>(request.m_Entity)) continue;

                if (request.m_HasTransform)
                {
                    // The panel never adds a transform lock (Anarchy).
                    bool addTransformLock = m_TransformGizmoTool.m_AddTransformLock;
                    m_TransformGizmoTool.m_AddTransformLock = false;
                    Dependency = request.m_Group
                        ? m_TransformGizmoTool.TransformSelection(Dependency, request.m_Transform, m_ToolOutputBarrier, m_UpdateObjectLookups)
                        : m_TransformGizmoTool.UpdateObject(Dependency, request.m_Entity, request.m_Transform.m_Position, request.m_Transform.m_Rotation, m_ToolOutputBarrier, m_UpdateObjectLookups);
                    m_TransformGizmoTool.m_AddTransformLock = addTransformLock;
                }

                if (request.m_HasScale)
                {
                    ApplyScale(request.m_Entity, request.m_Scale);
                }
            }

            m_Requests.Clear();
        }

        // A scale of 1 removes TransformObject.
        private void ApplyScale(Entity entity, float3 scale)
        {
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            if (math.all(scale == 1f))
            {
                if (!EntityManager.HasComponent<TransformObject>(entity)) return;
                commandBuffer.RemoveComponent<TransformObject>(entity);
            }
            else
            {
                commandBuffer.AddComponent(entity, new TransformObject(scale));
            }
            commandBuffer.AddComponent<Updated>(entity);
        }
    }
}
