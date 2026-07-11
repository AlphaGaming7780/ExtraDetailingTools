using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool
    {
        // Builds a full, permanent duplicate of a single object through the game's own CreationDefinition
        // pipeline (GenerateObjectsSystem), using CreationFlags.Permanent so the result comes out as a
        // finished entity directly, skipping the Temp/preview stage entirely.
        //
        // Deliberately does NOT walk installed upgrades, sub-objects, sub-areas or sub-nets: those buffers
        // are populated by the game itself (SubObjectSystem, area/net generation, ...), never by the player,
        // so duplicating their contents wouldn't preserve anything player-made — and doing so for a brand
        // new (not-yet-existing) parent hits a real engine limitation: FindOwnersSystem only resolves
        // OwnerDefinition against candidates that already have Updated + an existing SubObject/SubArea/SubNet
        // buffer, which a freshly created duplicate doesn't have yet, causing SubObjectReferencesSystem to
        // crash on entities left with a null Owner. Building upgrades are intentionally excluded too.
        //
        // The ObjectDefinition construction mirrors what Game.Tools.ObjectToolBaseSystem's own
        // CreateDefinitionsJob.BrushIterator.Iterate does for a single placed item. This is a separate job
        // from TransformGizmoTool's own CreateDefinitionsJob (which only builds Temp preview/highlight
        // definitions and must not be touched by this feature).
#if RELEASE
        [BurstCompile]
#endif
        private struct DuplicateEntityJob : IJob
        {
            [ReadOnly] public Entity m_Entity;

            [ReadOnly] public ComponentLookup<Transform> m_TransformData;
            [ReadOnly] public ComponentLookup<Game.Objects.Elevation> m_ElevationData;
            [ReadOnly] public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;
            [ReadOnly] public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

            public EntityCommandBuffer m_CommandBuffer;

            public void Execute()
            {
                if (!m_TransformData.TryGetComponent(m_Entity, out Transform transform))
                    return;

                Entity e = m_CommandBuffer.CreateEntity();

                CreationDefinition creationDefinition = new CreationDefinition
                {
                    m_Original = m_Entity,
                    m_Flags = CreationFlags.Permanent | CreationFlags.Duplicate
                };

                m_CommandBuffer.AddComponent(e, default(Updated));

                ObjectDefinition objectDefinition = new ObjectDefinition
                {
                    m_Position = transform.m_Position,
                    m_Rotation = transform.m_Rotation,
                    m_Probability = 100,
                    m_PrefabSubIndex = -1
                };

                if (m_ElevationData.TryGetComponent(m_Entity, out var elevation))
                {
                    objectDefinition.m_Elevation = elevation.m_Elevation;
                    objectDefinition.m_ParentMesh = ObjectUtils.GetSubParentMesh(elevation.m_Flags);
                    if ((elevation.m_Flags & ElevationFlags.Lowered) != 0)
                    {
                        creationDefinition.m_Flags |= CreationFlags.Lowered;
                    }
                }
                else
                {
                    objectDefinition.m_ParentMesh = -1;
                }

                if (m_LocalTransformCacheData.TryGetComponent(m_Entity, out var localTransformCache))
                {
                    objectDefinition.m_LocalPosition = localTransformCache.m_Position;
                    objectDefinition.m_LocalRotation = localTransformCache.m_Rotation;
                    objectDefinition.m_ParentMesh = localTransformCache.m_ParentMesh;
                    objectDefinition.m_GroupIndex = localTransformCache.m_GroupIndex;
                    objectDefinition.m_Probability = localTransformCache.m_Probability;
                    objectDefinition.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
                }
                else
                {
                    objectDefinition.m_LocalPosition = transform.m_Position;
                    objectDefinition.m_LocalRotation = transform.m_Rotation;
                }

                if (m_EditorContainerData.TryGetComponent(m_Entity, out var editorContainer))
                {
                    creationDefinition.m_SubPrefab = editorContainer.m_Prefab;
                    objectDefinition.m_Scale = editorContainer.m_Scale;
                    objectDefinition.m_Intensity = editorContainer.m_Intensity;
                    objectDefinition.m_GroupIndex = editorContainer.m_GroupIndex;
                }

                m_CommandBuffer.AddComponent(e, objectDefinition);
                m_CommandBuffer.AddComponent(e, creationDefinition);
            }
        }
    }
}
