
using Game.UI.InGame;
using HarmonyLib;

namespace ExtraDetailingTools
{
	internal class ActionsSectionPatch
	{
		[HarmonyPatch(typeof(ActionsSection), "OnProcess")]
		public class OnProcess
		{
			public static void Postfix(ActionsSection __instance)
			{
				Traverse traverse = Traverse.Create(__instance);

				//Entity selectedEntity = traverse.Property("selectedEntity").GetValue<Entity>();
				//bool disableable = traverse.Property("disableable").GetValue<bool>();

				//if (ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out PrefabData prefabData) && ExtraLib.m_PrefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase))
				//{
				//	EDT.Logger.Info(prefabBase);
				//}

				//if (ExtraLib.m_EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef) && ExtraLib.m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase2))
				//{
				//	EDT.Logger.Info(prefabBase2);
				//}

				traverse.Property("deletable").SetValue(true);
				//if (!ExtraLib.m_EntityManager.HasComponent<Car>(selectedEntity) && !ExtraLib.m_EntityManager.HasComponent<Human>(selectedEntity)) traverse.Property("moveable").SetValue(true);
				//traverse.Property("disableable").SetValue(disableable || ExtraLib.m_EntityManager.HasBuffer<EnabledEffect>(selectedEntity));

				//if (ExtraLib.m_EntityManager.TryGetBuffer<EnabledEffect>(selectedEntity, false, out var enabledEffect)) 
				//{
				//    foreach(EnabledEffect enabledEffect1 in enabledEffect)
				//    {
				//        EDT.Logger.Info("m_EffectIndex " + enabledEffect1.m_EffectIndex);
				//        EDT.Logger.Info("m_EnabledIndex " + enabledEffect1.m_EnabledIndex);
				//    }
				//}
			}
		}
	}
}