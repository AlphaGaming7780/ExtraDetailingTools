using Extra.Lib;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Extra.Lib.UI;

namespace ExtraDetailingTools;

internal static class EditEntities
{
	internal static void SetupEditEntities()
	{
		EntityQueryDesc netLaneEntityQueryDesc = new()
		{
			All = [
				ComponentType.ReadOnly<NetLaneData>(),
                ],
			Any = [
				ComponentType.ReadOnly<LaneDeteriorationData>(),
				//ComponentType.ReadOnly<SpawnableObjectData>(),
				ComponentType.ReadOnly<SecondaryLaneData>(),
				ComponentType.ReadOnly<UtilityLaneData>(),
                    //ComponentType.ReadOnly<NetLaneGeometryData>(),
                ],
			None = [
				ComponentType.ReadOnly<PlaceholderObjectElement>(),
				ComponentType.ReadOnly<TrackLaneData>(),
			]
		};

		//EntityQueryDesc UIToolbarGroupQuery = new()
		//{
		//	All = [
   //                 ComponentType.ReadOnly<UIToolbarGroupData>(),
   //             ]
		//}; 

            //EntityQueryDesc EffectEntityQueryDesc = new()
            //{
            //	All = [ComponentType.ReadOnly<EffectData>()]
            //};

            //EntityQueryDesc ActivityLocationEntityQueryDesc = new()
            //{
            //	All = [ComponentType.ReadOnly<ActivityLocationData>()]
            //};


		ExtraLib.AddOnEditEnities(new(OnEditNetLaneEntities, netLaneEntityQueryDesc));

		//ExtraLib.AddOnEditEnities(new(OnEditUIToolbarGroupEntity, UIToolbarGroupQuery));

		//ExtraLib.AddOnEditEnities(new(OnEditEffectEntities, EffectEntityQueryDesc));
		//ExtraLib.AddOnEditEnities(new(OnEditActivityLocationEntities, ActivityLocationEntityQueryDesc));
	}

	//private static void OnEditEffectEntities(NativeArray<Entity> entities)
	//{
	//	foreach (Entity entity in entities)
	//	{
	//		if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out EffectPrefab prefab))
	//		{

	//			var prefabUI = prefab.GetComponent<UIObject>();
	//			if (prefabUI == null)
	//			{
	//				prefabUI = prefab.AddComponent<UIObject>();
	//				prefabUI.active = true;
	//				prefabUI.m_IsDebugObject = false;
	//				prefabUI.m_Icon = Icons.GetIcon(prefab);
	//				prefabUI.m_Priority = 1;
	//			}

	//			prefabUI.m_Group?.RemoveElement(entity);
	//			prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "Effect", Icons.GetIcon, "Decals");
	//			prefabUI.m_Group.AddElement(entity);

	//			EffectData effectData = ExtraLib.m_EntityManager.GetComponentData<EffectData>(entity);
	//			effectData.m_Flags.m_RequiredFlags = EffectConditionFlags.None;
	//			ExtraLib.m_EntityManager.SetComponentData(entity, effectData);

	//			ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
	//		}
	//	}
	//}

	//private static void OnEditActivityLocationEntities(NativeArray<Entity> entities)
	//{
	//	foreach (Entity entity in entities)
	//	{
	//		if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out ActivityLocationPrefab prefab))
	//		{

	//			var prefabUI = prefab.GetComponent<UIObject>();
	//			if (prefabUI == null)
	//			{
	//				prefabUI = prefab.AddComponent<UIObject>();
	//				prefabUI.active = true;
	//				prefabUI.m_IsDebugObject = false;
	//				prefabUI.m_Icon = Icons.GetIcon(prefab);
	//				prefabUI.m_Priority = 1;
	//			}

	//			prefabUI.m_Group?.RemoveElement(entity);
	//			prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "ActivityLocation", Icons.GetIcon, "Effect");
	//			prefabUI.m_Group.AddElement(entity);

	//			ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
	//		}
	//	}
	//}

	//private static void OnEditUIToolbarGroupEntity(NativeArray<Entity> entities)
	//{
  //          foreach (Entity entity in entities)
  //          {
  //              if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out UIToolbarGroupPrefab prefab))
  //              {
	//			EDT.Logger.Info(prefab.name);
  //              }
  //          }
  //      }
	private static void OnEditNetLaneEntities(NativeArray<Entity> entities)
	{
            if (entities.Length == 0) return;
            ExtraAssetsMenu.AssetCat assetCat = ExtraAssetsMenu.GetOrCreateNewAssetCat("NetLanes", $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/NetLanes.svg");

            foreach (Entity entity in entities)
		{
			if ( ExtraLib.m_EntityManager.HasComponent<UtilityLaneData>(entity) && ExtraLib.m_EntityManager.GetComponentData<UtilityLaneData>(entity).m_UtilityTypes != Game.Net.UtilityTypes.Fence) continue;
			if (!ExtraLib.m_EntityManager.HasComponent<NetLaneGeometryData>(entity)) continue;

			if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out NetLanePrefab prefab))
			{

				var prefabUI = prefab.GetComponent<UIObject>();
				if (prefabUI == null)
				{
					prefabUI = prefab.AddComponent<UIObject>();
					prefabUI.active = true;
					prefabUI.m_IsDebugObject = false;
					prefabUI.m_Icon = Icons.GetIcon(prefab);
					prefabUI.m_Priority = 1;
				}

				prefabUI.m_Group?.RemoveElement(entity);
				if (prefab.GetComponent<UtilityLane>()?.m_UtilityType == Game.Net.UtilityTypes.Fence) prefabUI.m_Group = ExtraAssetsMenu.GetOrCreateNewUIAssetCategoryPrefab("Fence", Icons.GetIcon, assetCat);
				else if (prefab.GetComponent<SecondaryLane>() != null && prefab.GetComponent<ThemeObject>() != null) prefabUI.m_Group = ExtraAssetsMenu.GetOrCreateNewUIAssetCategoryPrefab("RoadMarking", Icons.GetIcon, assetCat);
				else prefabUI.m_Group = ExtraAssetsMenu.GetOrCreateNewUIAssetCategoryPrefab("Misc", Icons.GetIcon, assetCat);
                    prefabUI.m_Icon = $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/{prefabUI.m_Group.name}.svg";
                    prefabUI.m_Group.AddElement(entity);

				ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
			}
		}
	}

}
