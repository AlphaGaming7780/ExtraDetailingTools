using Extra.Lib.Helper;
using Extra.Lib;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

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
					ComponentType.ReadOnly<SpawnableObjectData>(),
				],
				None = [ComponentType.ReadOnly<PlaceholderObjectElement>()]
			};

			ExtraLib.AddOnEditEnities(new(OnEditSurfacesEntities, surfaceEntityQueryDesc));
			ExtraLib.AddOnEditEnities(new(OnEditDecalsEntities, decalsEntityQueryDesc));
		}

		private static void OnEditSurfacesEntities(NativeArray<Entity> entities)
		{
			foreach (Entity entity in entities)
			{
				if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out SurfacePrefab prefab))
				{

					var SurfaceUI = prefab.GetComponent<UIObject>();
					if (SurfaceUI == null)
					{
						SurfaceUI = prefab.AddComponent<UIObject>();
						SurfaceUI.active = true;
						SurfaceUI.m_IsDebugObject = false;
						SurfaceUI.m_Icon = Icons.GetIcon(prefab);
						SurfaceUI.m_Priority = 1;
					}

					SurfaceUI.m_Group?.RemoveElement(entity);
					SurfaceUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "Surfaces", Icons.GetIcon, "Terraforming");
					SurfaceUI.m_Group.AddElement(entity);

					ExtraLib.m_EntityManager.AddOrSetComponentData(entity, SurfaceUI.ToComponentData());
				}
			}
		}

		private static void OnEditDecalsEntities(NativeArray<Entity> entities)
		{
			foreach (Entity entity in entities)
			{
				if (ExtraLib.m_PrefabSystem.TryGetPrefab(entity, out StaticObjectPrefab prefab))
				{

					if (!prefab.name.ToLower().Contains("decal") && !prefab.name.ToLower().Contains("roadarrow") && !prefab.name.ToLower().Contains("lanemarkings")) continue;

					var DecalUI = prefab.GetComponent<UIObject>();
					if (DecalUI == null)
					{
						DecalUI = prefab.AddComponent<UIObject>();
						DecalUI.active = true;
						DecalUI.m_IsDebugObject = false;
						DecalUI.m_Icon = Icons.GetIcon(prefab);
						DecalUI.m_Priority = 1;
					}

					DecalUI.m_Group?.RemoveElement(entity);
					DecalUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "Decals", Icons.GetIcon, "Pathways");
					DecalUI.m_Group.AddElement(entity);

					ExtraLib.m_EntityManager.AddOrSetComponentData(entity, DecalUI.ToComponentData());
				}
			}
		}
	}
}
