
using Colossal.Entities;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.UI.InGame;
using HarmonyLib;
using Unity.Entities;

namespace ExtraDetailingTools.Patches
{
	internal class ActionsSectionPatch
	{
		[HarmonyPatch(typeof(ActionsSection), "OnProcess")]
		public class OnProcess
		{
			public static void Postfix(ActionsSection __instance)
			{
				Traverse traverse = Traverse.Create(__instance);

				Entity selectedEntity = traverse.Property("selectedEntity").GetValue<Entity>();

                traverse.Property("deletable").SetValue(!__instance.EntityManager.HasComponent<Aggregate>(selectedEntity));
            }
		}

        [HarmonyPatch(typeof(ActionsSection), "OnDelete")]
        public class OnDelete
        {
            public static void Postfix(ActionsSection __instance)
            {
                Traverse traverse = Traverse.Create(__instance);

				Entity selectedEntity = traverse.Property("selectedEntity").GetValue<Entity>();

                if (!__instance.EntityManager.Exists(selectedEntity))
					return;
                
                EndFrameBarrier m_EndFrameBarrier = traverse.Field("m_EndFrameBarrier").GetValue<EndFrameBarrier>();

                EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();

				if (__instance.EntityManager.TryGetBuffer<InstalledUpgrade>(selectedEntity, true, out var installedUpgradeBuffer))
				{
                    foreach (var upgrade in installedUpgradeBuffer)
                    {
                        if (!__instance.EntityManager.TryGetBuffer<SubArea>(upgrade.m_Upgrade, true, out var subAreaBuffer))
                            return;

                        foreach (SubArea subArea in subAreaBuffer)
                        {
                            entityCommandBuffer.AddComponent<Deleted>(subArea.m_Area);
                        }
                    }
                }

                // For roads name, but disabled because it cause visual glitchs with connected roads.
                if (__instance.EntityManager.TryGetBuffer<AggregateElement>(selectedEntity, true, out var aggregateElements))
                {
                    foreach (var aggregate in aggregateElements)
                    {
                        entityCommandBuffer.AddComponent<Deleted>(aggregate.m_Edge);
                    }
                }


                //if(__instance.EntityManager.TryGetBuffer<SubNet>(selectedEntity, true, out var subNets))
                //{
                //                foreach (var subNet in subNets)
                //                {
                //                    entityCommandBuffer.AddComponent<Deleted>(subNet.m_SubNet);
                //                }
                //            }
            }
        }
    }
}