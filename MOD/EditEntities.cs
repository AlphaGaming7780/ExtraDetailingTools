using Extra.Lib.Helper;
using Extra.Lib;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Game.Objects;

namespace ExtraDetailingTools
{
    internal static class EditEntities
    {
        internal static void SetupEditEntities()
        {
            EntityQueryDesc entityQueryDesc = new()
            {
                All = [ComponentType.ReadOnly<SurfaceData>()],
                None = [ComponentType.ReadOnly<PlaceholderObjectElement>()],
                
            };

            ExtraLib.AddOnEditEnities(new(OnEditEntities, entityQueryDesc));
        }

        private static void OnEditEntities(NativeArray<Entity> entities)
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
                        SurfaceUI.m_Icon = "Media/Game/Icons/LotTool.svg";
                        SurfaceUI.m_Priority = 1;
                    }

                    SurfaceUI.m_Group?.RemoveElement(entity);
                    SurfaceUI.m_Group = PrefabsHelper.GetOrCreateUIAssetCategoryPrefab("Landscaping", "Surfaces", "Media/Game/Icons/LotTool.svg", "Terraforming");
                    SurfaceUI.m_Group.AddElement(entity);

                    ExtraLib.m_EntityManager.AddOrSetComponentData(entity, SurfaceUI.ToComponentData());
                }
            }
        }
    }
}
