using Extra.Lib.Helper;
using Extra.Lib;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Colossal.Entities;
using Game.Objects;
using System.Linq;
using Colossal.Collections;

namespace ExtraDetailingTools
{
	internal static class EditEntities
	{
		internal static void SetupEditEntities()
		{
			EntityQueryDesc surfaceEntityQueryDesc = new()
			{
				All = [ComponentType.ReadOnly<SurfaceData>()],
				None = [ComponentType.ReadOnly<PlaceholderObjectElement>()]
				
			};

			EntityQueryDesc decalsEntityQueryDesc = new()
			{
				All = [
					ComponentType.ReadOnly<StaticObjectData>(),
				],
				Any = [
					ComponentType.ReadOnly<SpawnableObjectData>(),
					ComponentType.ReadOnly<PlaceableObjectData>(),
				],
				None = [ComponentType.ReadOnly<PlaceholderObjectElement>()]
			};

			EntityQueryDesc netLaneEntityQueryDesc = new()
			{
				All = [
					ComponentType.ReadOnly<NetLaneData>(),
				],
				Any = [
					ComponentType.ReadOnly<LaneDeteriorationData>(),
					ComponentType.ReadOnly<SpawnableObjectData>(),
					ComponentType.ReadOnly<SecondaryLaneData>(),
				],
				None = [
					ComponentType.ReadOnly<PlaceholderObjectElement>(),
					ComponentType.ReadOnly<TrackLaneData>(),
				]
			};

			EntityQueryDesc UIToolbarGroupQuery = new()
			{
				All = [
                    ComponentType.ReadOnly<UIToolbarGroupData>(),
                ]
			}; 

            //EntityQueryDesc EffectEntityQueryDesc = new()
            //{
            //	All = [ComponentType.ReadOnly<EffectData>()]
            //};

            //EntityQueryDesc ActivityLocationEntityQueryDesc = new()
            //{
            //	All = [ComponentType.ReadOnly<ActivityLocationData>()]
            //};


            ExtraLib.AddOnEditEnities(new(OnEditSurfacesEntities, surfaceEntityQueryDesc));
			ExtraLib.AddOnEditEnities(new(OnEditDecalsEntities, decalsEntityQueryDesc));
			ExtraLib.AddOnEditEnities(new(OnEditNetLaneEntities, netLaneEntityQueryDesc));

			ExtraLib.AddOnEditEnities(new(OnEditUIToolbarGroupEntity, UIToolbarGroupQuery));

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

		private static void OnEditUIToolbarGroupEntity(NativeArray<Entity> entities)
		{
            foreach (Entity entity in entities)
            {
                if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out UIToolbarGroupPrefab prefab))
                {
					EDT.Logger.Info(prefab.name);
                }
            }
        }


        private static void OnEditSurfacesEntities(NativeArray<Entity> entities)
		{

			if (entities.Length > 0) ExtraDetailingMenu.CreateNewAssetCat("Surfaces", $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/Surfaces.svg");

			foreach (Entity entity in entities)
			{
				if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out SurfacePrefab prefab))
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
					prefabUI.m_Group = ExtraDetailingMenu.CreateNewUIAssetCategoryPrefab(Surfaces.GetCatByRendererPriority(prefab.GetComponent<RenderedArea>() is null ? 0 : prefab.GetComponent<RenderedArea>().m_RendererPriority) + " Surfaces", Icons.GetIcon, "Surfaces");
                    prefabUI.m_Group.AddElement(entity);

					ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}

		private static void OnEditDecalsEntities(NativeArray<Entity> entities)
		{
            if (entities.Length > 0) ExtraDetailingMenu.CreateNewAssetCat("Decals", $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/Decals.svg");

            foreach (Entity entity in entities)
			{
				if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out StaticObjectPrefab prefab))
				{

					DynamicBuffer<SubMesh> subMeshes =  ExtraLib.m_EntityManager.GetBuffer<SubMesh>(entity);
					if (ExtraLib.m_EntityManager.TryGetComponent(subMeshes.ElementAt(0).m_SubMesh, out MeshData component))
					{
						if (component.m_State != MeshFlags.Decal) continue;
					}
					else continue;

                    if (ExtraLib.m_EntityManager.TryGetComponent(entity, out ObjectGeometryData objectGeometryData))
					{
						objectGeometryData.m_Flags &= ~GeometryFlags.Overridable;
						ExtraLib.m_EntityManager.SetComponentData(entity, objectGeometryData);
					}

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
					prefabUI.m_Group = ExtraDetailingMenu.CreateNewUIAssetCategoryPrefab(Decals.GetCatByDecalName(prefab.name) + " Decals", Icons.GetIcon, "Decals");
                    prefabUI.m_Group.AddElement(entity);

					ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}

		private static void OnEditNetLaneEntities(NativeArray<Entity> entities)
		{
			foreach (Entity entity in entities)
			{
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
					prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "NetLane", Icons.GetIcon, "Pathways");
					prefabUI.m_Group.AddElement(entity);

					ExtraLib.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}

	}
}
