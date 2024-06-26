﻿using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Extra.Lib;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools;

internal partial class TransformSection : InfoSectionBase
{
	private string Clipboard
	{
		get { return GUIUtility.systemCopyBuffer; }
		set { GUIUtility.systemCopyBuffer = value; }
	}

	private OverlayRenderSystem m_OverlayRenderSystem;

	private GetterValueBinding<float3> transformSectionGetPos;
	private GetterValueBinding<float3> transformSectionGetRot;
	private GetterValueBinding<double> transformSectionGetIncPos;
	private GetterValueBinding<double> transformSectionGetIncRot;
	private GetterValueBinding<bool> transformSectionGetLocalPos;

	private double2 increment = new(1, 45);
	private Transform transform;
	private bool useLocalAxis = false;
	private bool showAxis = false;

	protected override string group => "Transform Tool";

	protected override void OnCreate()
	{
		base.OnCreate();

		m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();

		AddBinding(transformSectionGetPos = new GetterValueBinding<float3>("edt", "transformsection_pos", GetPosition));
		AddBinding(new TriggerBinding<float3>("edt", "transformsection_pos", new Action<float3>(SetPosition)));

		AddBinding(transformSectionGetRot = new GetterValueBinding<float3>("edt", "transformsection_rot", GetRotation));
		AddBinding(new TriggerBinding<float3>("edt", "transformsection_rot", new Action<float3>(SetRotattion)));

		AddBinding(transformSectionGetIncPos = new GetterValueBinding<double>("edt", "transformsection_incpos", () => { return increment.x; })) ;
		AddBinding(new TriggerBinding<double>("edt", "transformsection_incpos", (double inc) => { increment.x = inc; transformSectionGetIncPos.Update(); } ));

		AddBinding(transformSectionGetIncRot = new GetterValueBinding<double>("edt", "transformsection_incrot", () => { return increment.y; }));
		AddBinding(new TriggerBinding<double>("edt", "transformsection_incrot", (double inc) => { increment.y = inc; transformSectionGetIncRot.Update(); }));

		AddBinding(new TriggerBinding("edt", "transformsection_copypos", new Action(CopyPosition) ));
		AddBinding(new TriggerBinding("edt", "transformsection_pastpos", new Action(PastPosition) ));

		AddBinding(new TriggerBinding("edt", "transformsection_copyrot", new Action(CopyRotation)));
		AddBinding(new TriggerBinding("edt", "transformsection_pastrot", new Action(PastRotation)));

		AddBinding(transformSectionGetLocalPos = new GetterValueBinding<bool>("edt", "transformsection_localaxis", () => useLocalAxis));
		AddBinding(new TriggerBinding("edt", "transformsection_localaxis", new Action(UseLocalAxis)));

		AddBinding(new TriggerBinding<bool>("edt", "showhighlight", new Action<bool>(ShowHighlight)));

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		visible = EntityManager.HasComponent<Game.Objects.Transform>(selectedEntity) && !EntityManager.HasComponent<Building>(selectedEntity); ;
		if (visible)
		{
			transform = EntityManager.GetComponentData<Transform>(selectedEntity);
			transformSectionGetPos.Update();
			transformSectionGetRot.Update();
			RequestUpdate();
			if(showAxis)
			{
				Bounds3 bounds3 = new (new(0,0,0), new(10,10,10));

				if (EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) && EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData))
				{
					bounds3 = useLocalAxis ? geometryData.m_Bounds : ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
				}

                float3 linesLenght = new(bounds3.x.max - bounds3.x.min, bounds3.y.max - bounds3.y.min, bounds3.z.max - bounds3.z.min);
				//linesLenght = new(linesLenght.x + (linesLenght.x / 10f), linesLenght.y + (linesLenght.y / 10f), linesLenght.z + (linesLenght.z / 10f));

                RenderAxisJob renderAxisJob = new()
				{
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
					pos = transform.m_Position,
					linesLenght = linesLenght,
                    rot = GetRotation(),
					useLocalAxis = useLocalAxis,
                };
				JobHandle jobHandle = renderAxisJob.Schedule(Dependency);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle);
				Dependency = jobHandle;
			}
		}	
	}

#if RELEASE
	[BurstCompile]
#endif
	private struct RenderAxisJob : IJob
	{
		public OverlayRenderSystem.Buffer m_OverlayBuffer;
		public float3 pos;
		public float3 rot;
		public float3 linesLenght;
		public bool useLocalAxis;

		public void Execute()
		{
			float3 xAxis = new(linesLenght.x, 0f, 0f);
			float3 yAxis = new(0f, linesLenght.y, 0f);
			float3 zAxis = new(0f, 0f, linesLenght.z);
            if (useLocalAxis)
            {
                float sinX = linesLenght.z * Mathf.Sin(rot.y * Mathf.PI / 180);
                float cosX = linesLenght.z * Mathf.Cos(rot.y * Mathf.PI / 180);

                float sinZ = linesLenght.x * Mathf.Sin((rot.y + 90) * Mathf.PI / 180);
                float cosZ = linesLenght.x * Mathf.Cos((rot.y + 90) * Mathf.PI / 180);

				xAxis = new(sinX, 0, cosX);
				zAxis = new(sinZ, 0, cosZ);
            }
            m_OverlayBuffer.DrawLine( UnityEngine.Color.red, UnityEngine.Color.red, 0, OverlayRenderSystem.StyleFlags.Grid, new ( pos, pos + xAxis ), 0.1f, 0.5f );
			m_OverlayBuffer.DrawLine( UnityEngine.Color.green, UnityEngine.Color.green, 0, OverlayRenderSystem.StyleFlags.Grid, new ( pos, pos + yAxis ), 0.1f, 0.5f );
			m_OverlayBuffer.DrawLine( UnityEngine.Color.blue, UnityEngine.Color.blue, 0, OverlayRenderSystem.StyleFlags.Grid, new ( pos, pos + zAxis ), 0.1f, 0.5f );
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
			showAxis = true;
		}
		else if (EntityManager.HasComponent<Highlighted>(selectedEntity))
		{
			EntityManager.RemoveComponent<Highlighted>(selectedEntity);
			EntityManager.AddComponentData(selectedEntity, new BatchesUpdated());
			showAxis = false;
		}
	}

	private void UseLocalAxis()
	{
		useLocalAxis = !useLocalAxis;
		transformSectionGetLocalPos.Update();
		transformSectionGetPos.Update();
	}

	private void CopyPosition()
	{
		float3 vector3 = transform.m_Position;
		Clipboard = $"{vector3.x} {vector3.y} {vector3.z}";
	}

	private void PastPosition()
	{
		UpdateSelectedEntity(StringToFloat3(Clipboard, transform.m_Position) - transform.m_Position, float3.zero);
	}

	private void CopyRotation()
	{
		float3 vector3 = GetRotation();
		Clipboard = $"{vector3.x} {vector3.y} {vector3.z}";
	}

	private void PastRotation()
	{
		UpdateSelectedEntity(float3.zero, StringToFloat3(Clipboard, GetRotation()) - GetRotation());
	}

	private float3 GetPosition()
	{
		if (useLocalAxis)
		{
			return new (0, transform.m_Position.y, 0);
		}
		return transform.m_Position;
	}

	private float3 GetRotation()
	{	
		UnityEngine.Quaternion q = transform.m_Rotation;
		return q.eulerAngles;
	}

    private float3 GetRotation(Transform transform)
    {
        UnityEngine.Quaternion q = transform.m_Rotation;
        return q.eulerAngles;
    }

    private void SetPosition(float3 positionOffset)
	{
		if (useLocalAxis)
		{
			float3 rot = GetRotation();
			float sinX = positionOffset.x * Mathf.Sin(rot.y * Mathf.PI / 180);
			float cosX = positionOffset.x * Mathf.Cos(rot.y * Mathf.PI / 180);

			float sinZ = positionOffset.z * Mathf.Sin((rot.y + 90) * Mathf.PI / 180);
			float cosZ = positionOffset.z * Mathf.Cos((rot.y + 90) * Mathf.PI / 180);

			positionOffset = new(sinX + sinZ, positionOffset.y, cosX + cosZ);
		}
		UpdateSelectedEntity(positionOffset, float3.zero);
	}

	private void SetRotattion(float3 rotationOffset)
	{
			UpdateSelectedEntity(float3.zero, rotationOffset);
	}

	private void UpdateSelectedEntity(float3 positionOffset, float3 rotationOffset)
	{

		transform.m_Position += positionOffset;
		transform.m_Rotation = Quaternion.Euler(rotationOffset + GetRotation());

		if (EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) && EntityManager.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData geometryData) && EntityManager.TryGetComponent(selectedEntity, out CullingInfo cullingInfo))
		{
			Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
			cullingInfo.m_Bounds = bounds3;
			EntityManager.SetComponentData(selectedEntity, cullingInfo);
		}

		UpdateSubElement(selectedEntity, positionOffset, rotationOffset);

		EntityManager.SetComponentData(selectedEntity, transform);
		EntityManager.AddComponentData(selectedEntity, new Game.Common.Updated());
		transformSectionGetPos.Update();
		transformSectionGetRot.Update();
	}

	private void UpdateInstalledUpgrade(Entity entity, float3 positionOffset, float3 rotationOffset)
	{
		if (EntityManager.TryGetBuffer(entity, false, out DynamicBuffer<Game.Buildings.InstalledUpgrade> installedUpgrades))
		{
			foreach (Game.Buildings.InstalledUpgrade installedUpgrade in installedUpgrades)
			{
				if (!EntityManager.TryGetComponent(installedUpgrade, out Game.Objects.Transform transform1))
				{
					continue;
				}
				if (EntityManager.TryGetComponent(installedUpgrade, out PrefabRef prefabRef1) && EntityManager.TryGetComponent(prefabRef1.m_Prefab, out ObjectGeometryData geometryData1) && EntityManager.TryGetComponent(installedUpgrade, out CullingInfo cullingInfo1))
				{
					cullingInfo1.m_Bounds = ObjectUtils.CalculateBounds(transform1.m_Position, transform1.m_Rotation, geometryData1);
					EntityManager.SetComponentData(installedUpgrade, cullingInfo1);
				}
				// UnityEngine.Quaternion quaternion = transform.m_Rotation;
				// float3 newAngle = quaternion.eulerAngles;
				transform1.m_Position += positionOffset;
				// float3 distance = transform1.m_Position-transform.m_Position;
				// float lenght = (float)Math.Sqrt(Math.Pow(distance.x, 2)+Math.Pow(distance.z, 2));
				// transform1.m_Position -= distance; // * (float)Math.Cos(newAngle.y) //  * (float)Math.Sin(newAngle.y)
				// transform1.m_Position = transform.m_Position + new float3(lenght * (float)Math.Cos(newAngle.y), distance.y, lenght * (float)Math.Sin(newAngle.y));
				//UnityEngine.Quaternion quaternion = transform1.m_Rotation;
				transform1.m_Rotation = Quaternion.Euler(rotationOffset + GetRotation());

				UpdateSubElement(installedUpgrade, positionOffset, rotationOffset);

				EntityManager.SetComponentData(installedUpgrade, transform1);
				EntityManager.AddComponentData(installedUpgrade, new Game.Common.Updated());
			}
		}
	}

	private void UpdateSubElement(Entity entity, float3 positionOffset, float3 rotationOffset)
	{
		UpdateInstalledUpgrade(entity, positionOffset, rotationOffset);
		UpdateSubArea(entity, positionOffset, rotationOffset);
        UpdateSubObject(entity, positionOffset, rotationOffset);
        //UpdateSubNet(entity, positionOffset, rotationOffset);
    }

	private void UpdateSubArea(Entity entity, float3 positionOffset, float3 rotationOffset)
	{
		if (EntityManager.TryGetBuffer(entity, false, out DynamicBuffer<Game.Areas.SubArea> subAreas))
		{
			foreach (Game.Areas.SubArea subArea in subAreas)
			{
				if (EntityManager.TryGetBuffer(subArea.m_Area, false, out DynamicBuffer<Game.Areas.Node> nodes))
				{
					for(int i = 0; i< nodes.Length; i++)
					{
						nodes.ElementAt(i).m_Position += positionOffset;
					}
				}
				EntityManager.AddComponentData(subArea.m_Area, new Game.Common.Updated());
			}
		}
	}

    private void UpdateSubObject(Entity entity, float3 positionOffset, float3 rotationOffset)
    {
        if (EntityManager.TryGetBuffer(entity, false, out DynamicBuffer<Game.Objects.SubObject> subObjects))
        {
            foreach (Game.Objects.SubObject subObject in subObjects)
            {
                if (EntityManager.TryGetComponent(subObject.m_SubObject, out Transform transform))
                {
                    transform.m_Position += positionOffset;
                    transform.m_Rotation = Quaternion.Euler(rotationOffset + GetRotation(transform));
                }
                EntityManager.AddComponentData(subObject.m_SubObject, new Game.Common.Updated());
            }
        }
    }

    private void UpdateSubNet(Entity entity, float3 positionOffset, float3 rotationOffset)
	{
		if (EntityManager.TryGetBuffer(entity, false, out DynamicBuffer<Game.Net.SubNet> subNets))
		{
			foreach (Game.Net.SubNet subNet in subNets)
			{
				if (EntityManager.TryGetComponent(subNet.m_SubNet, out Game.Net.Node node))
				{
				node.m_Position += positionOffset;
				EntityManager.SetComponentData(subNet.m_SubNet, node);
				}

				EntityManager.AddComponentData(subNet.m_SubNet, new Game.Common.Updated());
			}
		}
	}

	private float3 StringToFloat3(string sVector, float3 defaultResult)
	{

		try
		{
			string[] sArray = sVector.Split(' ');

			defaultResult = new(
				float.Parse(sArray[0]),
				float.Parse(sArray[1]),
				float.Parse(sArray[2]));
		}
		catch (Exception e) { EDT.Logger.Warn(e); }


		return defaultResult;
	}
}
