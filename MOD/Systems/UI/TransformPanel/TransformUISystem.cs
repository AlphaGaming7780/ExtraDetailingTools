using Colossal.Entities;
using Colossal.UI.Binding;
using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Systems.Tools;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Rendering;
using Game.UI;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.UI.TransformPanel
{
    internal partial class TransformUISystem : UISystemBase
    {
        private float3 _copiedPos;
        private float3 _copiedRot;
        private float3 _copiedScale;

        private bool canPastPos = false;
        private bool canPastRot = false;
        private bool canPastScale = false;

        private TransformGizmoTool m_TransformGizmoTool;
        private EndFrameBarrier m_EndFrameBarrier;

        private GetterValueBinding<float3> transformGetPos;
        private GetterValueBinding<float3> transformGetRot;
        private GetterValueBinding<float3> transformGetScale;

        private GetterValueBinding<double> transformGetIncPos;
        private GetterValueBinding<double> transformGetIncRot;
        private GetterValueBinding<double> transformGetIncScale;

        private GetterValueBinding<bool> transformCanPastPos;
        private GetterValueBinding<bool> transformCanPastRot;
        private GetterValueBinding<bool> transformCanPastScale;

        private GetterValueBinding<bool> m_AsSubBuildingBinding;
        private GetterValueBinding<bool> m_AllowScalingBinding;

        private double3 increment = new(1, 45, 1);
        private Transform transform;
        private TransformObject transformObject;
        private bool UseLocalAxis => m_TransformGizmoTool.m_UseLocalAxis;

        private float3 m_EulerAngles;
        private quaternion m_LastSetRotation;

        private Entity m_SelectedEntity;
        private bool m_AsSubBuilding = false;
        //private bool m_LastUseLocalAxis = false;

#if Extra4
        private bool m_AllowScaling = true;
#else
        private bool m_AllowScaling = false;
#endif

        public Entity SelectedEntity => m_SelectedEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;

            m_TransformGizmoTool = World.GetOrCreateSystemManaged<TransformGizmoTool>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            AddBinding(transformGetPos = new GetterValueBinding<float3>("EDT", "TransformPanel.pos", GetPosition));
            AddBinding(new TriggerBinding<float3>("EDT", "TransformPanel.pos", new Action<float3>(SetPosition)));
            AddBinding(new TriggerBinding<float3>("EDT", "TransformPanel.abspos", new Action<float3>(SetAbsolutePosition)));

            AddBinding(transformGetRot = new GetterValueBinding<float3>("EDT", "TransformPanel.rot", GetRotation));
            AddBinding(new TriggerBinding<float3>("EDT", "TransformPanel.rot", new Action<float3>(SetRotation)));
            AddBinding(new TriggerBinding<float3>("EDT", "TransformPanel.absrot", new Action<float3>(SetAbsoluteRotation)));

            AddBinding(transformGetScale = new GetterValueBinding<float3>("EDT", "TransformPanel.scale", GetScale));
            AddBinding(new TriggerBinding<float3>("EDT", "TransformPanel.scale", new Action<float3>(SetScale)));

            AddBinding(transformGetIncPos = new GetterValueBinding<double>("EDT", "TransformPanel.incpos", () => increment.x));
            AddBinding(new TriggerBinding<double>("EDT", "TransformPanel.incpos", (double inc) => { increment.x = inc; transformGetIncPos.Update(); }));

            AddBinding(transformGetIncRot = new GetterValueBinding<double>("EDT", "TransformPanel.incrot", () => increment.y));
            AddBinding(new TriggerBinding<double>("EDT", "TransformPanel.incrot", (double inc) => { increment.y = inc; transformGetIncRot.Update(); }));

            AddBinding(transformGetIncScale = new GetterValueBinding<double>("EDT", "TransformPanel.incscale", () => increment.z));
            AddBinding(new TriggerBinding<double>("EDT", "TransformPanel.incscale", (double inc) => { increment.z = inc; transformGetIncScale.Update(); }));

            AddBinding(new TriggerBinding<string>("EDT", "TransformPanel.copy", new Action<string>(Copy)));
            AddBinding(new TriggerBinding<string, string>("EDT", "TransformPanel.past", new Action<string, string>(Past)));

            AddBinding(transformCanPastPos = new GetterValueBinding<bool>("EDT", "TransformPanel.canpastpos", () => canPastPos));
            AddBinding(transformCanPastRot = new GetterValueBinding<bool>("EDT", "TransformPanel.canpastrot", () => canPastRot));
            AddBinding(transformCanPastScale = new GetterValueBinding<bool>("EDT", "TransformPanel.canpastscale", () => canPastScale));

            AddBinding(m_AsSubBuildingBinding = new GetterValueBinding<bool>("EDT", "TransformPanel.assubbuilding", () => m_AsSubBuilding));
            AddBinding(m_AllowScalingBinding = new GetterValueBinding<bool>("EDT", "TransformPanel.allowscaling", () => m_AllowScaling));
        }

        public void SetSelectedEntity(Entity entity)
        {
            m_SelectedEntity = entity;

            if (EntityManager.HasComponent<Transform>(m_SelectedEntity))
            {
                var t = EntityManager.GetComponentData<Transform>(m_SelectedEntity);
                m_EulerAngles = ((UnityEngine.Quaternion)t.m_Rotation).eulerAngles;
                m_LastSetRotation = t.m_Rotation;
            }

            m_AsSubBuilding = false;

            if (EntityManager.HasComponent<Building>(m_SelectedEntity) && EntityManager.TryGetBuffer<InstalledUpgrade>(m_SelectedEntity, true, out DynamicBuffer<InstalledUpgrade> installedUpgrades))
            {
                foreach (InstalledUpgrade installedUpgrade in installedUpgrades)
                {
                    if (EntityManager.HasComponent<Building>(installedUpgrade))
                    {
                        m_AsSubBuilding = true;
                        break;
                    }
                }
            }

            m_AsSubBuildingBinding.Update();
        }

        public bool NeedUpdate()
        {
            //if (UseLocalAxis != m_LastUseLocalAxis)
            //{
            //    m_LastUseLocalAxis = UseLocalAxis;
            //    return true;
            //}
            return EntityManager.HasComponent<InterpolatedTransform>(m_SelectedEntity) || EntityManager.HasComponent<Updated>(m_SelectedEntity);
        }

        public void Process()
        {
            transform = EntityManager.HasComponent<InterpolatedTransform>(m_SelectedEntity) ?
                        EntityManager.GetComponentData<InterpolatedTransform>(m_SelectedEntity).ToTransform() :
                        EntityManager.GetComponentData<Transform>(m_SelectedEntity);

            transformObject = EntityManager.HasComponent<TransformObject>(m_SelectedEntity) ?
                              EntityManager.GetComponentData<TransformObject>(m_SelectedEntity) :
                              default;

            if (math.abs(math.dot(transform.m_Rotation.value, m_LastSetRotation.value)) < 0.9999f)
            {
                m_EulerAngles = ((UnityEngine.Quaternion)transform.m_Rotation).eulerAngles;
                m_LastSetRotation = transform.m_Rotation;
            }

            transformGetPos.Update();
            transformGetRot.Update();
            transformGetScale.Update();
        }

        private void UpdateObject(float3 position, quaternion rotation)
        {
            m_TransformGizmoTool.UpdateObject(Dependency, m_SelectedEntity, position, rotation, m_EndFrameBarrier);
        }

        private float3 GetPosition()
        {
            if (UseLocalAxis)
            {
                return new(0, transform.m_Position.y, 0);
            }
            return transform.m_Position;
        }

        private float3 GetRotation()
        {
            return m_EulerAngles;
        }

        private void SetPosition(float3 positionOffset)
        {
            if (UseLocalAxis)
            {
                quaternion rot = transform.m_Rotation;
                positionOffset = math.mul(rot, positionOffset);
            }
            transform.m_Position += positionOffset;
            UpdateObject(transform.m_Position, transform.m_Rotation);
            transformGetPos.Update();
        }

        private void SetAbsolutePosition(float3 targetPosition)
        {
            float3 offset = targetPosition - GetPosition();
            SetPosition(offset);
        }

        private void SetRotation(float3 rotationOffset)
        {
            m_EulerAngles += rotationOffset;
            quaternion newRotation = Quaternion.Euler(m_EulerAngles);
            m_LastSetRotation = newRotation;
            UpdateObject(transform.m_Position, newRotation);
            transform.m_Rotation = newRotation;
            transformGetRot.Update();
        }

        private void SetAbsoluteRotation(float3 targetEuler)
        {
            m_EulerAngles = targetEuler;
            quaternion newRotation = Quaternion.Euler(m_EulerAngles);
            m_LastSetRotation = newRotation;
            UpdateObject(transform.m_Position, newRotation);
            transform.m_Rotation = newRotation;
            transformGetRot.Update();
        }

        private float3 GetScale()
        {
            if (!EntityManager.HasComponent<TransformObject>(m_SelectedEntity)) return new float3(1, 1, 1);
            return transformObject.m_Scale;
        }

        private void SetScale(float3 scale)
        {
            if (EntityManager.HasComponent<TransformObject>(m_SelectedEntity))
            {
                if (scale.x == 1 && scale.y == 1 && scale.z == 1)
                {
                    EntityManager.RemoveComponent<TransformObject>(m_SelectedEntity);
                    EntityManager.AddComponentData<Updated>(m_SelectedEntity, default);
                    transformGetScale.Update();
                    return;
                }
            }
            else
            {
                EntityManager.AddComponent<TransformObject>(m_SelectedEntity);
            }
            transformObject = EntityManager.GetComponentData<TransformObject>(m_SelectedEntity);
            transformObject.m_Scale = scale;
            EntityManager.SetComponentData(m_SelectedEntity, transformObject);
            EntityManager.AddComponentData<Updated>(m_SelectedEntity, default);
            transformGetScale.Update();
        }

        private void Copy(string id)
        {
            switch (id)
            {
                case "POS":
                    _copiedPos = transform.m_Position;
                    canPastPos = true;
                    transformCanPastPos.Update();
                    break;
                case "ROT":
                    _copiedRot = GetRotation();
                    canPastRot = true;
                    transformCanPastRot.Update();
                    break;
                case "SCALE":
                    _copiedScale = GetScale();
                    canPastScale = true;
                    transformCanPastScale.Update();
                    break;
            }
        }

        private void Past(string id, string axis = null)
        {
            switch (id)
            {
                case "POS":
                    float3 posOffset = new(0, 0, 0);
                    switch (axis)
                    {
                        case "X": posOffset.x = _copiedPos.x - transform.m_Position.x; break;
                        case "Y": posOffset.y = _copiedPos.y - transform.m_Position.y; break;
                        case "Z": posOffset.z = _copiedPos.z - transform.m_Position.z; break;
                        default: posOffset = _copiedPos - transform.m_Position; break;
                    }
                    transform.m_Position += posOffset;
                    UpdateObject(transform.m_Position, transform.m_Rotation);
                    transformGetPos.Update();
                    break;

                case "ROT":
                    float3 rotOffset = new(0, 0, 0);
                    switch (axis)
                    {
                        case "X": rotOffset.x = _copiedRot.x - GetRotation().x; break;
                        case "Y": rotOffset.y = _copiedRot.y - GetRotation().y; break;
                        case "Z": rotOffset.z = _copiedRot.z - GetRotation().z; break;
                        default: rotOffset = _copiedRot - GetRotation(); break;
                    }
                    quaternion newRotation = Quaternion.Euler(rotOffset + GetRotation());
                    UpdateObject(transform.m_Position, newRotation);
                    transform.m_Rotation = newRotation;
                    transformGetRot.Update();
                    break;

                case "SCALE":
                    float3 newScale = GetScale();
                    switch (axis)
                    {
                        case "X": newScale.x = _copiedScale.x; break;
                        case "Y": newScale.y = _copiedScale.y; break;
                        case "Z": newScale.z = _copiedScale.z; break;
                        default: newScale = _copiedScale; break;
                    }
                    SetScale(newScale);
                    break;
            }
        }
    }
}
