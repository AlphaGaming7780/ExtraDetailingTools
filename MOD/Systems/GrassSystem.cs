using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Structs;
using ExtraDetailingTools.Systems.Tools;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Systems
{
    partial class GrassSystem : GameSystemBase
    {
        private TerrainSystem m_TerrainSystem;
        private PrefabSystem m_PrefabSystem;
        private ModificationEndBarrier m_ModificationEndBarrier;

        private EntityQuery m_BrushQuery;
        private EntityQuery m_GrassDataQuery;

        private NativeHashMap<Entity, Entity> m_GrassPrefabToSavingEntity;

        public SerializableTextureStruct<ColorRG16> DefaultTexturStruct { get; private set; } = new(TerrainSystem.kDefaultHeightmapWidth, TerrainSystem.kDefaultHeightmapHeight);

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ModificationEndBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();

            m_GrassPrefabToSavingEntity = new(0, Allocator.Persistent);

            m_BrushQuery = GetEntityQuery(new ComponentType[]
{
                ComponentType.ReadOnly<Brush>(),
                ComponentType.Exclude<Hidden>(),
                ComponentType.Exclude<Deleted>()
            });

            m_GrassDataQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<GrassData>(),
                ComponentType.ReadOnly<PrefabRef>(),

            });

        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> grassDataEntities = m_GrassDataQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Entity> brushEntities = m_BrushQuery.ToEntityArray(Allocator.Temp);

            if (TryGetValideBrushEntity(brushEntities, out Entity brushEntity, out Brush brush, out GrassBrushData grassBrushData) && grassBrushData.m_State != GrassToolSystem.State.Default)
            {

                if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(brushEntity).m_Prefab, out PrefabBase prefab))
                {
                    EDT.Logger.Warn("Isn't a prefab");
                    return;
                }

                if (prefab is not BrushPrefab brushPrefab)
                {
                    EDT.Logger.Warn("Isn't a brush prefab");
                    return;
                }

                if (brush.m_Tool == null)
                {
                    EDT.Logger.Warn("brush.m_Tool is null");
                    return;
                }


                //if (!m_GrassPrefabToSavingEntity.ContainsKey(brush.m_Tool)) {
                //    SavingEntityCreationDefinition savingEntityCreationDefinition = new()
                //    {
                //        entityCommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer(),
                //        brush = brush,
                //        grassDataEntities = grassDataEntities,
                //        DefaultTextureStruct = DefaultTexturStruct,
                //        m_GrassPrefabToSavingEntity = m_GrassPrefabToSavingEntity,
                //        prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(),
                //    };

                //    JobHandle creationDefJob = savingEntityCreationDefinition.Schedule(Dependency);
                //    creationDefJob.Complete();
                //    //Dependency = creationDefJob;
                //}

                TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData();
                Texture2D texture = brushPrefab.m_Texture;
                NativeArray<Color32> brushTextureData = new NativeArray<Color32>(texture.GetPixels32(), Allocator.TempJob);

                TextureStruct<Color32> brushTexture = new()
                {
                    data = brushTextureData,
                    width = texture.width,
                    height = texture.height
                };

                UpdateGrassMapTextures updateGrassMapTextures = new()
                {
                    entityCommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer(),
                    grassDataLookup = SystemAPI.GetComponentLookup<GrassData>(),
                    prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(),
                    DefaultTextureStruct = DefaultTexturStruct,
                    grassBrushData = grassBrushData,
                    grassDataEntities = grassDataEntities,
                    terrainHeightData = terrainHeightData,
                    brush = brush,
                    //brushArea = brushArea,
                    brushTexture = brushTexture,
                    m_GrassPrefabToSavingEntity = m_GrassPrefabToSavingEntity,
                };

                JobHandle jobHandle = updateGrassMapTextures.Schedule(Dependency);
                this.m_ModificationEndBarrier.AddJobHandleForProducer(jobHandle);
                brushTextureData.Dispose(jobHandle);
                Dependency = jobHandle;
                //jobHandle.Complete();
            }
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            m_GrassPrefabToSavingEntity.Clear();

            NativeArray<Entity> grassDataEntities = m_GrassDataQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in grassDataEntities)
            {
                PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);

                m_GrassPrefabToSavingEntity.Add(prefabRef.m_Prefab, entity);

                EntityManager.AddComponent<Updated>(entity);
            }

        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_GrassPrefabToSavingEntity.Dispose();
        }

        private bool TryGetValideBrushEntity(NativeArray<Entity> entities, out Entity brushEntity, out Brush brush, out GrassBrushData grassData)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                Brush tBrush = EntityManager.GetComponentData<Brush>(entity);
                if (EntityManager.TryGetComponent<GrassBrushData>(tBrush.m_Tool, out grassData))
                {
                    brushEntity = entity;
                    brush = tBrush;
                    return true;
                }
            }

            brushEntity = Entity.Null;
            brush = default(Brush);
            grassData = default(GrassBrushData);
            return false;

        }

#if RELEASE
	[BurstCompile]
#endif
        //[BurstCompile]
        private struct UpdateGrassMapTextures : IJob
        {
            [ReadOnly]
            public EntityCommandBuffer entityCommandBuffer;

            [ReadOnly]
            public ComponentLookup<GrassData> grassDataLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> prefabRefLookup;

            [ReadOnly]
            public TextureStruct<Color32> brushTexture;

            [ReadOnly]
            public SerializableTextureStruct<ColorRG16> DefaultTextureStruct;

            //[ReadOnly]
            public NativeHashMap<Entity, Entity> m_GrassPrefabToSavingEntity;

            [ReadOnly]
            public Brush brush;

            [ReadOnly]
            public GrassBrushData grassBrushData;

            [ReadOnly]
            public TerrainHeightData terrainHeightData;

            [ReadOnly]
            public NativeArray<Entity> grassDataEntities;


            public void Execute()
            {
                if (!m_GrassPrefabToSavingEntity.TryGetValue(brush.m_Tool, out Entity savingEntity))
                {
                    savingEntity = CreateDefinition();

                    //EDT.Logger.Error("Brush.m_Tool isn't in m_GrassPrefabToSavingEntity, if you see that, that mean something went wrong in the Grass rendering system.");
                    //return;
                }

                //if (!prefabRefLookup.TryGetComponent(savingEntity, out PrefabRef prefabRef))
                //{
                //    EDT.Logger.Error("savingEntity doesn't have PrefabRef Component, if you see that, that mean something went wrong in the Grass rendering system.");
                //    return;
                //}

                //Entity prefabEntity = prefabRef.m_Prefab;

                //if (brush.m_Tool != prefabEntity)
                //{
                //    EDT.Logger.Error("Brush.m_Tool isn't the same as the prefab Entity, if you see that, that mean something went wrong in the Grass rendering system.");
                //    return;
                //}

                if (!grassDataLookup.TryGetComponent(savingEntity, out GrassData grassData) || grassData.textureStruct.data.Length == 0 || grassData.textureStruct.width == 0 || grassData.textureStruct.height == 0 )
                {
                    grassData = new(DefaultTextureStruct);

                    //EDT.Logger.Error("No Grass Data component on the saving entity, if you see that, that mean something went wrong in the Grass rendering system.");
                    //return;
                }

                SerializableTextureStruct<ColorRG16> savedTexture = grassData.textureStruct;

                brush.m_Angle = 0; //Disabling rotation for now

                Bounds2 brushArea = ToolUtils.GetBounds(brush);

                float3 brushAreaMinFloat3 = new float3(brushArea.min.x, 0, brushArea.min.y);
                float3 brushAreaMaxFloat3 = new float3(brushArea.max.x, 0, brushArea.max.y);

                float3 brushAreaMinToHeightMapFloat3 = TerrainUtils.ToHeightmapSpace(ref terrainHeightData, brushAreaMinFloat3);
                float3 brushAreaMaxToHeightMapFloat3 = TerrainUtils.ToHeightmapSpace(ref terrainHeightData, brushAreaMaxFloat3);

                Bounds2 brushHeightMapArea = new Bounds2(brushAreaMinToHeightMapFloat3.xz, brushAreaMaxToHeightMapFloat3.xz);

                float2 brushHeightMapAreaSize = brushHeightMapArea.Size();
                float2 areaToTextureScale = brushTexture.Size() / brushHeightMapAreaSize;

                for (int x = 0; x < brushHeightMapAreaSize.x; x++)
                {
                    for (int y = 0; y < brushHeightMapAreaSize.y; y++)
                    {
                        int2 brushTexturePos = GetBrushTexturePos(x, y, areaToTextureScale);
                        int2 userTexturePos = new int2((int)Math.Round(brushHeightMapArea.min.x) + x, (int)Math.Round(brushHeightMapArea.min.y) + y);

                        if (userTexturePos.x < 0 || userTexturePos.x > 4095 || userTexturePos.y < 0 || userTexturePos.y > 4095) continue;


                        ColorRG16 color = savedTexture.GetValue(userTexturePos);

                        int val;
                        switch (grassBrushData.m_State)
                        {
                            case GrassToolSystem.State.Adding:
                                //val = (int)Math.Round(color.g + brushTexture.GetValue(brushTexturePos).a / 255 * brush.m_Strength);
                                val = color.g + (int)Math.Round(brushTexture.GetValue(brushTexturePos).a * brush.m_Strength);
                                color.g = val > 255 ? (byte)255 : (byte)val;
                                //color.g = brushTexture.GetValue(brushTexturePos).a;
                                break;
                            case GrassToolSystem.State.Removing:
                                //color.g = color.g - brushTexture.GetValue(brushTexturePos).a * brush.m_Strength;
                                //val = (int)Math.Round(color.g - brushTexture.GetValue(brushTexturePos).a / 255 * brush.m_Strength);
                                val = color.g - (int)Math.Round(brushTexture.GetValue(brushTexturePos).a * brush.m_Strength);
                                color.g = val < 0 ? (byte)0 : (byte)val;
                                //color.g = brushTexture.GetValue(brushTexturePos).a > 200 ? (byte)0 : color.g;
                                break;
                        }

                        savedTexture.SetValue(userTexturePos, color);

                    }
                }

                entityCommandBuffer.SetComponent<GrassData>(savingEntity, grassData);
                entityCommandBuffer.AddComponent<Updated>(savingEntity, default);

            }

            int2 GetBrushTexturePos(int x, int y, float2 areaToTextureScale)
            {
                x = (int)Math.Round(x * areaToTextureScale.x);
                x = Math.Min(brushTexture.width-1, Math.Max(0, x));

                y = (int)Math.Round(y * areaToTextureScale.y);
                y = Math.Min(brushTexture.height-1, Math.Max(0, y));

                return new int2(x, y);
            }

            Entity CreateDefinition()
            {
                for (int i = 0; i < grassDataEntities.Length; i++)
                {
                    Entity entity = grassDataEntities[i];

                    if (!prefabRefLookup.TryGetComponent(entity, out PrefabRef prefabRef)) continue;

                    if (prefabRef.m_Prefab != brush.m_Tool) continue;

                    m_GrassPrefabToSavingEntity.Add(brush.m_Tool, entity);

                    entityCommandBuffer.AddComponent<Updated>(entity, default);

                    return entity;

                }

                //CreationDefinition creationDefinition = default(CreationDefinition);
                //creationDefinition.m_Prefab = brush.m_Tool;
                //creationDefinition.m_Flags = CreationFlags.Permanent;

                //ObjectDefinition objectDefinition = default;
                //objectDefinition.m_Position = float3.zero;

                PrefabRef prefabRef1 = default;
                prefabRef1.m_Prefab = brush.m_Tool;

                GrassData grassData = new(DefaultTextureStruct);

                Entity e = entityCommandBuffer.CreateEntity();
                //entityCommandBuffer.AddComponent<CreationDefinition>(e, creationDefinition);
                //entityCommandBuffer.AddComponent<ObjectDefinition>(e, objectDefinition);
                entityCommandBuffer.AddComponent<PrefabRef>(e, prefabRef1);
                entityCommandBuffer.AddComponent<GrassData>(e, grassData);
                entityCommandBuffer.AddComponent<Updated>(e, default);

                //m_GrassPrefabToSavingEntity.Add(brush.m_Tool, e);

                return e;
            }
        }
    }
}
