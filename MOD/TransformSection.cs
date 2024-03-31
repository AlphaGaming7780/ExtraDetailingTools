using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Extra;
using Extra.Lib;
using Game.Buildings;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.UI.InGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools
{
	internal partial class TransformSection : InfoSectionBase
	{

        public struct Float3 : IJsonWritable, IJsonReadable
        {
            public Float3(float3 float3)
			{
				x = float3.x; y = float3.y; z = float3.z;
			}

            public Float3(float x = 0, float y = 0, float z = 0) { this.x = x; this.y = y; this.z = z; }

			public float x = 0, y = 0, z = 0;
            public readonly void Write(IJsonWriter writer)
            {
				writer.TypeBegin(GetType().FullName);
				writer.PropertyName("x");
				writer.Write(x);
                writer.PropertyName("y");
                writer.Write(y);
                writer.PropertyName("z");
                writer.Write(z);
				writer.TypeEnd();
            }

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty("x");
                reader.Read(out this.x);
                reader.ReadProperty("y");
                reader.Read(out this.y);
                reader.ReadProperty("z");
                reader.Read(out this.z);
                reader.ReadMapEnd();
            }
        }

        private static GetterValueBinding<Float3> transformSectionGetPos;
		private static GetterValueBinding<Float3> transformSectionGetRot;

		protected override string group => "TransformSection";

		protected override void OnCreate()
		{
			base.OnCreate();

			SelectedInfoUISystem selectedInfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
			selectedInfoUISystem.eventSelectionChanged += OnSelectionChanged;

			AddBinding(transformSectionGetPos = new GetterValueBinding<Float3>("edt", "transformsection_getpos", GetPosition));
			AddBinding(new TriggerBinding<Float3>("edt", "transformsection_getpos", new Action<Float3>(SetPosition)));

			AddBinding(transformSectionGetRot = new GetterValueBinding<Float3>("elt", "transformsection_getrot", GetRotation));
			AddBinding(new TriggerBinding<Float3>("edt", "transformsection_getrot", new Action<Float3>(SetRotattion)));

		}

		protected override void OnUpdate()
		{
			visible = EntityManager.HasComponent<Game.Objects.Transform>(selectedEntity);
		}

		public override void OnWriteProperties(IJsonWriter writer)
		{
			
		}

		protected override void OnProcess()
		{
			
		}

		protected override void Reset()
		{
			
		}

		private void SetPosition(Float3 position)
		{
			float3 float3 = new(position.x, position.y, position.z);
			UpdateObjectTransform(float3, null);
		}

		private void SetRotattion(Float3 rotation)
		{
			float3 float3 = new(rotation.x, rotation.y, rotation.z);
			UpdateObjectTransform(null, float3);
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
			if (!ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) || !ExtraLib.m_EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData) || !ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out CullingInfo cullingInfo))
			{
				EDT.Logger.Warn("Failed to get the CullingInfo on the Entity");
				transformSectionGetPos.Update();
				transformSectionGetRot.Update();
				return;
			}

			//Bounds3 bounds3 = new(transform.m_Position, transform.m_Position);
			Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
			cullingInfo.m_Bounds = bounds3;

			if (ExtraLib.m_EntityManager.TryGetBuffer(selectedEntity, false, out DynamicBuffer<Game.Buildings.InstalledUpgrade> installedUpgrades))
			{
				foreach (Game.Buildings.InstalledUpgrade installedUpgrade in installedUpgrades)
				{
					if (!ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out Game.Objects.Transform transform1))
					{
						continue;
					}
					if (!ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out PrefabRef prefabRef1) || !ExtraLib.m_EntityManager.TryGetComponent(prefabRef1.m_Prefab, out ObjectGeometryData geometryData1) || !ExtraLib.m_EntityManager.TryGetComponent(installedUpgrade, out CullingInfo cullingInfo1))
					{
						continue;
					}
					// UnityEngine.Quaternion quaternion = transform.m_Rotation;
					// float3 newAngle = quaternion.eulerAngles;
					transform1.m_Position += positionOffset;
					// float3 distance = transform1.m_Position-transform.m_Position;
					// float lenght = (float)Math.Sqrt(Math.Pow(distance.x, 2)+Math.Pow(distance.z, 2));
					// transform1.m_Position -= distance; // * (float)Math.Cos(newAngle.y) //  * (float)Math.Sin(newAngle.y)
					// transform1.m_Position = transform.m_Position + new float3(lenght * (float)Math.Cos(newAngle.y), distance.y, lenght * (float)Math.Sin(newAngle.y));
					transform1.m_Rotation = transform.m_Rotation;
					cullingInfo1.m_Bounds = ObjectUtils.CalculateBounds(transform1.m_Position, transform1.m_Rotation, geometryData1);
					ExtraLib.m_EntityManager.SetComponentData(installedUpgrade, transform1);
					ExtraLib.m_EntityManager.SetComponentData(installedUpgrade, cullingInfo1);
					ExtraLib.m_EntityManager.AddComponentData(installedUpgrade, new Game.Common.Updated());
				}
			}

			ExtraLib.m_EntityManager.SetComponentData(selectedEntity, transform);
			ExtraLib.m_EntityManager.SetComponentData(selectedEntity, cullingInfo);
			ExtraLib.m_EntityManager.AddComponentData(selectedEntity, new Game.Common.Updated());
			if (!position.Equals(float3.zero)) transformSectionGetPos.Update();
			if (!rotation.Equals(float3.zero)) transformSectionGetRot.Update();
		}

		private Float3 GetPosition()
		{
			ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform);
			return new(transform.m_Position);
		}

		private Float3 GetRotation()
		{
			ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform);
			UnityEngine.Quaternion quaternion = transform.m_Rotation;
			return new(quaternion.eulerAngles);
		}

		private void OnSelectionChanged(Entity entity, Entity prefab, float3 SelectedPosition)
		{
			if (entity == Entity.Null //||
				//ExtraLib.m_EntityManager.HasComponent<Building>(entity) ||
    //            ExtraLib.m_EntityManager.HasComponent<LabelExtents>(entity) ||
				//!ExtraLib.m_EntityManager.HasComponent<Game.Objects.Transform>(entity)
			) return;

			transformSectionGetPos.Update();
			transformSectionGetRot.Update();
		}
	}
}
