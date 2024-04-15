using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Extra;
using Extra.Lib;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools
{
	internal partial class TransformSection : InfoSectionBase
	{

		private GetterValueBinding<float3> transformSectionGetPos;
		private GetterValueBinding<float3> transformSectionGetRot;

		protected override string group => "Transform Tool";

		protected override void OnCreate()
		{
			base.OnCreate();

			AddBinding(transformSectionGetPos = new GetterValueBinding<float3>("edt", "transformsection_getpos", GetPosition));
			AddBinding(new TriggerBinding<float3>("edt", "transformsection_getpos", new Action<float3>(SetPosition)));

			AddBinding(transformSectionGetRot = new GetterValueBinding<float3>("edt", "transformsection_getrot", GetRotation));
			AddBinding(new TriggerBinding<float3>("edt", "transformsection_getrot", new Action<float3>(SetRotattion)));

			AddBinding(new TriggerBinding<bool>("edt", "showhighlight", new Action<bool>(ShowHighlight)));

		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			visible = EntityManager.HasComponent<Game.Objects.Transform>(selectedEntity);
			if (visible)
			{
				transformSectionGetPos.Update();
				transformSectionGetRot.Update();
				RequestUpdate();
			}
		}

		public override void OnWriteProperties(IJsonWriter writer) {}

		protected override void OnProcess() {}

		protected override void Reset() {}

		private void ShowHighlight(bool b)
		{
            if (b && !EntityManager.HasComponent<Highlighted>(selectedEntity))
            {
                EntityManager.AddComponentData(selectedEntity, new Highlighted());
                EntityManager.AddComponentData(selectedEntity, new BatchesUpdated());
            }
            else if (EntityManager.HasComponent<Highlighted>(selectedEntity))
            {
                EntityManager.RemoveComponent<Highlighted>(selectedEntity);
                EntityManager.AddComponentData(selectedEntity, new BatchesUpdated());
            }
        }

		private void SetPosition(float3 position)
		{
			UpdateObjectTransform(position, null);
		}

		private void SetRotattion(float3 rotation)
		{
			UpdateObjectTransform(null, rotation);
		}

		private void UpdateObjectTransform(float3? position, float3? rotation)
		{
			if (!ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform))
			{
				EDT.Logger.Warn("Can't get the transform object.");
				transformSectionGetPos.Update();
				transformSectionGetRot.Update();
				return;
			}

			float3 positionOffset = float3.zero;

			if (position is float3 positionFloat3)
			{
				positionOffset = positionFloat3 - transform.m_Position;
				transform.m_Position = positionFloat3;
			}
			if (rotation is float3 rotationFloat3)
			{
				transform.m_Rotation = Quaternion.Euler(rotationFloat3);
			}
			if (ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) && ExtraLib.m_EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData) && ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out CullingInfo cullingInfo))
			{
                Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
                cullingInfo.m_Bounds = bounds3;
                ExtraLib.m_EntityManager.SetComponentData(selectedEntity, cullingInfo);
            }

			if (ExtraLib.m_EntityManager.TryGetBuffer(selectedEntity, false, out DynamicBuffer<Game.Buildings.InstalledUpgrade> installedUpgrades))
			{
				foreach (Game.Buildings.InstalledUpgrade installedUpgrade in installedUpgrades)
				{
					if (!ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out Game.Objects.Transform transform1))
					{
						continue;
					}
					if (ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out PrefabRef prefabRef1) && ExtraLib.m_EntityManager.TryGetComponent(prefabRef1.m_Prefab, out ObjectGeometryData geometryData1) && ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out CullingInfo cullingInfo1))
					{
                        cullingInfo1.m_Bounds = ObjectUtils.CalculateBounds(transform1.m_Position, transform1.m_Rotation, geometryData1);
                        ExtraLib.m_EntityManager.SetComponentData(installedUpgrade, cullingInfo1);
                    }
					// UnityEngine.Quaternion quaternion = transform.m_Rotation;
					// float3 newAngle = quaternion.eulerAngles;
					transform1.m_Position += positionOffset;
					// float3 distance = transform1.m_Position-transform.m_Position;
					// float lenght = (float)Math.Sqrt(Math.Pow(distance.x, 2)+Math.Pow(distance.z, 2));
					// transform1.m_Position -= distance; // * (float)Math.Cos(newAngle.y) //  * (float)Math.Sin(newAngle.y)
					// transform1.m_Position = transform.m_Position + new float3(lenght * (float)Math.Cos(newAngle.y), distance.y, lenght * (float)Math.Sin(newAngle.y));
					transform1.m_Rotation = transform.m_Rotation;
					
					ExtraLib.m_EntityManager.SetComponentData(installedUpgrade, transform1);
					ExtraLib.m_EntityManager.AddComponentData(installedUpgrade, new Game.Common.Updated());
				}
			}

			ExtraLib.m_EntityManager.SetComponentData(selectedEntity, transform);
			ExtraLib.m_EntityManager.AddComponentData(selectedEntity, new Game.Common.Updated());
			transformSectionGetPos.Update();
			transformSectionGetRot.Update();
		}

		private float3 GetPosition()
		{
			ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform);
			return transform.m_Position;
		}

		private float3 GetRotation()
		{
			ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform);
			UnityEngine.Quaternion quaternion = transform.m_Rotation;
			return quaternion.eulerAngles;
		}
	}
}
