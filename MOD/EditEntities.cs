using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Colossal.Entities;
using Game.SceneFlow;
using System.Linq;
using ExtraLib;
using ExtraLib.Systems.UI;
using ExtraLib.ClassExtension;
using ExtraLib.Helpers;
using ExtraLib.Prefabs;

namespace ExtraDetailingTools
{
	internal static class EditEntities
	{
		private static bool? isAssetIconLibraryEnabled;
		public static bool IsAssetIconLibraryEnabled => isAssetIconLibraryEnabled ??= GameManager.instance.modManager.ListModsEnabled().Any(x => x.StartsWith("AssetIconLibrary,"));

		internal static void SetupEditEntities()
		{
			EntityQueryDesc surfaceEntityQueryDesc = new()
			{
				All = new[] { ComponentType.ReadOnly<SurfaceData>() },
				None = new[] { ComponentType.ReadOnly<PlaceholderObjectElement>() },
			};

			EntityQueryDesc decalsEntityQueryDesc = new()
			{
				All = new[] { ComponentType.ReadOnly<StaticObjectData>(), },
				Any = new[] { ComponentType.ReadOnly<SpawnableObjectData>(), ComponentType.ReadOnly<PlaceableObjectData>(), },
				None = new[] { ComponentType.ReadOnly<PlaceholderObjectElement>() },
			};

			EntityQueryDesc netLaneEntityQueryDesc = new()
			{
				All = new[] { ComponentType.ReadOnly<NetLaneData>(), },
				Any = new[] {
					ComponentType.ReadOnly<LaneDeteriorationData>(),
					//ComponentType.ReadOnly<SpawnableObjectData>(),
					ComponentType.ReadOnly<SecondaryLaneData>(),
					ComponentType.ReadOnly<UtilityLaneData>(),
					//ComponentType.ReadOnly<NetLaneGeometryData>(),
				},
				None = new[] {
					ComponentType.ReadOnly<PlaceholderObjectElement>(),
					ComponentType.ReadOnly<TrackLaneData>(),
				},
			};

		//EntityQueryDesc brandEntityQueryDesc = new()
		//{
		//	All = [
		//		ComponentType.ReadOnly<BrandData>()
		//	]
		//};

		EL.AddOnEditEnities(new (OnEditSurfacesEntities, surfaceEntityQueryDesc));
		EL.AddOnEditEnities(new (OnEditDecalsEntities, decalsEntityQueryDesc));
		EL.AddOnEditEnities(new (OnEditNetLaneEntities, netLaneEntityQueryDesc));

		//ExtraLib.AddOnEditEnities(new(OnEditBrandEntity, brandEntityQueryDesc));
	}

	//private static void OnEditBrandEntity(NativeArray<Entity> entities)
	//{
	//	string list = "Possible values :";
	//	foreach (Entity entity in entities)
	//	{
	//		if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out BrandPrefab prefab))
	//		{
	//			list += $" `{prefab.name}`,";
	//		}
	//	}
	//	EDT.Logger.Info(list);
	//}


		private static void OnEditSurfacesEntities(NativeArray<Entity> entities)
		{
			if (entities.Length == 0) return;

			UIAssetParentCategoryPrefab assetCat = PrefabsHelper.GetOrCreateUIAssetParentCategoryPrefab("Surfaces");

			foreach (Entity entity in entities)
			{
				if (EL.m_PrefabSystem.TryGetPrefab(entity, out SurfacePrefab prefab))
				{
					if (!prefab.builtin || prefab.name == "Surface Area") continue;

					var prefabUI = prefab.GetComponent<UIObject>();
					if (prefabUI == null)
					{
						prefabUI = prefab.AddComponent<UIObject>();
						prefabUI.active = true;
						prefabUI.m_IsDebugObject = false;
						prefabUI.m_Icon = Icons.GetIcon(prefab);
						prefabUI.m_Priority = 1;
					}

					string catName = Surfaces.GetCatByRendererPriority(prefab.GetComponent<RenderedArea>() is null ? 0 : prefab.GetComponent<RenderedArea>().m_RendererPriority);
					prefabUI.m_Group?.RemoveElement(entity);
					prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetChildCategoryPrefab(assetCat, $"{catName} {assetCat.name}");
					prefabUI.m_Group.AddElement(entity);

					EL.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}

		private static void OnEditDecalsEntities(NativeArray<Entity> entities)
		{
			if (entities.Length == 0) return;
			UIAssetParentCategoryPrefab assetCat = PrefabsHelper.GetOrCreateUIAssetParentCategoryPrefab("Decals");

			foreach (Entity entity in entities)
			{
				if (EL.m_PrefabSystem.TryGetPrefab(entity, out StaticObjectPrefab prefab))
				{
					if (!prefab.builtin) continue;

					if (!EL.m_EntityManager.HasBuffer<SubMesh>(entity)) continue;

					DynamicBuffer<SubMesh> subMeshes = EL.m_EntityManager.GetBuffer<SubMesh>(entity);

					if (subMeshes.Length < 1) continue;

					if (!EL.m_EntityManager.TryGetComponent(subMeshes.ElementAt(0).m_SubMesh, out MeshData component)) continue;
					else if (component.m_State != MeshFlags.Decal) continue;

					var prefabUI = prefab.GetComponent<UIObject>();
					if (prefabUI == null)
					{
						prefabUI = prefab.AddComponent<UIObject>();
						prefabUI.active = true;
						prefabUI.m_IsDebugObject = false;
						prefabUI.m_Icon = IsAssetIconLibraryEnabled ? "" : Icons.GetIcon(prefab);
						prefabUI.m_Priority = 1;
					}

					string catName = Decals.GetCatByDecalName(prefab.name);
					prefabUI.m_Group?.RemoveElement(entity);
					prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetChildCategoryPrefab(assetCat, $"{catName} {assetCat.name}");
					prefabUI.m_Group.AddElement(entity);

					EL.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}

		private static void OnEditNetLaneEntities(NativeArray<Entity> entities)
		{
			if (entities.Length == 0) return;
			UIAssetParentCategoryPrefab assetCat = PrefabsHelper.GetOrCreateUIAssetParentCategoryPrefab("NetLanes");

			foreach (Entity entity in entities)
			{
				if (EL.m_EntityManager.HasComponent<UtilityLaneData>(entity) && EL.m_EntityManager.GetComponentData<UtilityLaneData>(entity).m_UtilityTypes != Game.Net.UtilityTypes.Fence) continue;
				if (!EL.m_EntityManager.HasComponent<NetLaneGeometryData>(entity)) continue;

				if (EL.m_PrefabSystem.TryGetPrefab(entity, out NetLanePrefab prefab))
				{

					var prefabUI = prefab.GetComponent<UIObject>();
					if (prefabUI == null)
					{
						prefabUI = prefab.AddComponent<UIObject>();
						prefabUI.active = true;
						prefabUI.m_IsDebugObject = false;
						prefabUI.m_Icon = IsAssetIconLibraryEnabled ? "" : Icons.GetIcon(prefab);
						prefabUI.m_Priority = 1;
					}

					prefabUI.m_Group?.RemoveElement(entity);
					if (prefab.GetComponent<UtilityLane>()?.m_UtilityType == Game.Net.UtilityTypes.Fence) prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetChildCategoryPrefab(assetCat, $"Fence {assetCat.name}");
					else if (prefab.GetComponent<SecondaryLane>() != null && prefab.GetComponent<ThemeObject>() != null) prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetChildCategoryPrefab(assetCat, $"RoadMarking {assetCat.name}");
					else prefabUI.m_Group = PrefabsHelper.GetOrCreateUIAssetChildCategoryPrefab(assetCat, $"Misc {assetCat.name}");
					if (!IsAssetIconLibraryEnabled) prefabUI.m_Icon = $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/{prefabUI.m_Group.name}.svg";
					prefabUI.m_Group.AddElement(entity);

					EL.m_EntityManager.AddOrSetComponentData(entity, prefabUI.ToComponentData());
				}
			}
		}
	}
}