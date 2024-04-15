using Colossal.Entities;
using Extra.Lib;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace ExtraDetailingTools
{
	internal partial class EffectEnablerSystem : SystemBase
	{
		EntityQuery createdEffectEntityQuery;

		protected override void OnCreate()
		{
			base.OnCreate();
			EntityQueryDesc createdEffectEntityQueryDesc = new()
			{
				All = [ComponentType.ReadOnly<VFXData>(), ComponentType.ReadOnly<Temp>()]
            };

			createdEffectEntityQuery = GetEntityQuery(createdEffectEntityQueryDesc);

			RequireForUpdate(createdEffectEntityQuery);

		}

		protected override void OnUpdate()
		{
			EDT.Logger.Info("Updated");
			foreach (Entity createdEffect in createdEffectEntityQuery.ToEntityArray(AllocatorManager.Temp)) 
			{
				if(ExtraLib.m_EntityManager.TryGetComponent(createdEffect, out PrefabData prefabData) && ExtraLib.m_PrefabSystem.TryGetPrefab(prefabData, out PrefabBase effectPrefab))
				{
					EDT.Logger.Info(effectPrefab);
				}

				EffectData effectData = ExtraLib.m_EntityManager.GetComponentData<EffectData>(createdEffect);
				effectData.m_Flags.m_RequiredFlags = EffectConditionFlags.None;
				ExtraLib.m_EntityManager.SetComponentData(createdEffect, effectData);
				ExtraLib.m_EntityManager.AddComponent<Updated>(createdEffect);
			}
		}
	}
}
