using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using ExtraDetailingTools.Prefabs;
using ExtraDetailingTools.Structs;
using ExtraLib.Helpers;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace ExtraDetailingTools.Systems
{
    public partial class GrassRenderSystem : GameSystemBase
    {
        private TerrainSystem m_TerrainSystem;
        private TerrainMaterialSystem m_TerrainMaterialSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private PrefabSystem m_PrefabSystem;

        private static VisualEffectAsset s_FoliageVFXAsset;

        private EntityQuery m_GrassDataQuery;

        private Dictionary<Entity, Texture2D> m_GrassTextureMap;
        private Dictionary<Entity, VisualEffect> m_GrassVisualEffectMap;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_TerrainMaterialSystem = World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GrassRenderSystem.s_FoliageVFXAsset = Resources.Load<VisualEffectAsset>("Vegetation/FoliageVFX");

            m_GrassTextureMap = new();
            m_GrassVisualEffectMap = new();

            m_GrassDataQuery = GetEntityQuery(new ComponentType[]
{
                ComponentType.ReadWrite<GrassData>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Updated>(),

            });
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            DestroyEverything();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            EDT.Logger.Info("OnStartRunning");
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            EDT.Logger.Info($"OnGamePreload : {purpose}, {mode} ");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            DestroyEverything();
            EDT.Logger.Info($"OnGameLoadingComplete : {purpose}, {mode} ");
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            EDT.Logger.Info("OnGameLoaded");
        }

        protected override void OnUpdate()
        {
            if (this.m_CameraUpdateSystem.activeViewer != null)
            {
                
                NativeArray<Entity> grassDataEntities = m_GrassDataQuery.ToEntityArray(Allocator.Temp);

                for (int i = 0; i < grassDataEntities.Length; i++)
                {
                    Entity entity = grassDataEntities[i];

                    GrassData grassData = EntityManager.GetComponentData<GrassData>(entity);
                    PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);

                    if (grassData.textureStruct.data == null && grassData.textureStruct.data.Length == 0)
                    {
                        EDT.Logger.Warn("Texture data is null on the saving entity.");
                        continue;
                    }

                    Texture2D grassMaptexture;

                    if(!m_GrassTextureMap.ContainsKey(entity))
                    {
                        grassMaptexture = new Texture2D(TerrainSystem.kDefaultHeightmapWidth, TerrainSystem.kDefaultHeightmapHeight, TextureFormat.RG16, false, true)
                        {
                            name = m_PrefabSystem.GetPrefabName(prefabRef.m_Prefab),
                            hideFlags = HideFlags.HideAndDontSave,
                        };

                        m_GrassTextureMap.Add(entity, grassMaptexture);
                    }
                    else
                    {
                        grassMaptexture = m_GrassTextureMap[entity];
                    }

                    if (!m_GrassVisualEffectMap.ContainsKey(entity))
                    {
                        VisualEffect visualEffect = CreateVFX(m_PrefabSystem.GetPrefabName(entity));

                        if(visualEffect != null)
                        {
                            m_GrassVisualEffectMap.Add(entity, visualEffect);
                        }
                    }

                    NativeArray<ColorRG16> texture = grassMaptexture.GetPixelData<ColorRG16>(0);

                    UpdateGrassVFXTextureMap updateGrassVFXTextureMap = default;
                    updateGrassVFXTextureMap.savedTexture = grassData.textureStruct.data;
                    updateGrassVFXTextureMap.vfxGrassTextureMap = texture;
                    JobHandle jobHandle = updateGrassVFXTextureMap.Schedule(Dependency);
                    IAwaiter awaiter = jobHandle.GetAwaiter();
                    awaiter.OnCompleted(() => grassMaptexture.Apply(false));
                    Dependency = jobHandle;
                }

                Entity[] entities = new Entity[m_GrassTextureMap.Count];
                m_GrassTextureMap.Keys.CopyTo(entities, 0);
                foreach (Entity entity in entities)
                {
                    if(!EntityManager.Exists(entity))
                    {
                        DestroyEntity(entity);
                        continue;
                    }
                    Texture2D grassMaptexture = m_GrassTextureMap[entity];

                    VisualEffect visualEffect;

                    if (!m_GrassVisualEffectMap.ContainsKey(entity))
                    {
                        PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
                        visualEffect = CreateVFX(m_PrefabSystem.GetPrefabName(prefabRef.m_Prefab));
                        if (visualEffect == null) continue;

                        m_GrassVisualEffectMap.Add(entity, visualEffect);
                    }
                    else
                    {
                        visualEffect = m_GrassVisualEffectMap[entity];
                    }

                    UpdateEffect(visualEffect, grassMaptexture);
                }
            }
        }

        private void UpdateEffect(VisualEffect visualEffect, Texture2D grassMap)
        {
            Bounds terrainBounds = this.m_TerrainSystem.GetTerrainBounds();
            visualEffect.SetVector3("TerrainBounds_center", terrainBounds.center);
            visualEffect.SetVector3("TerrainBounds_size", terrainBounds.size);
            visualEffect.SetTexture("Terrain HeightMap", this.m_TerrainSystem.heightmap);
            //visualEffect.SetTexture("Terrain SplatMap", this.m_TerrainMaterialSystem.splatmap);
            visualEffect.SetTexture("Terrain SplatMap", grassMap);

            Vector4 globalVector = Shader.GetGlobalVector("colossal_TerrainScale");
            Vector4 globalVector2 = Shader.GetGlobalVector("colossal_TerrainOffset");
            visualEffect.SetVector4("Terrain Offset Scale", new Vector4(globalVector.x, globalVector.z, globalVector2.x, globalVector2.z));
            visualEffect.SetVector3("CameraPosition", this.m_CameraUpdateSystem.position);
            visualEffect.SetVector3("CameraDirection", this.m_CameraUpdateSystem.direction);

            //visualEffect.SetVector2("Crop Size", grassPrefab.CropSize);
            //visualEffect.SetFloat("FoliageCoverage", grassPrefab.FoliageCoverage > 0 ? grassPrefab.FoliageCoverage : 1);
            //visualEffect.SetAnimationCurve("Scale Over Distance", grassPrefab.ScaleOverDistance);

            //EDT.Logger.Info( $"Texture format : {m_TerrainMaterialSystem.splatmap.graphicsFormat}");
            //EDT.Logger.Info( $"Center : {terrainBounds.center}, Size : {terrainBounds.size}");
            //EDT.Logger.Info($"Terrain Offset Scale : {new Vector4(globalVector.x, globalVector.z, globalVector2.x, globalVector2.z)}");
            //Texture2D texture2D = TextureHelper.GetTexture2DFromTexture(m_TerrainMaterialSystem.splatmap);
            //TextureHelper.SaveTextureAsPNG(texture2D, $"{EDT.ResourcesIcons}/Heightmap.png");
            //CoreUtils.Destroy(texture2D);

        }

        private VisualEffect CreateVFX(string name)
        {
            if (s_FoliageVFXAsset != null)
            {

                foreach (VisualEffect vE in m_GrassVisualEffectMap.Values)
                {
                    if (vE.name != name) continue;
                    return vE;
                }

                GameObject gameObject = GameObject.Find(name) ?? new GameObject(name);
                VisualEffect visualEffect = gameObject.GetComponent<VisualEffect>() ?? gameObject.AddComponent<VisualEffect>();
                visualEffect.name = name;

                //VisualEffect visualEffect = new GameObject(name).AddComponent<VisualEffect>();
                visualEffect.visualEffectAsset = s_FoliageVFXAsset;
                return visualEffect;
            }

            return null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DestroyEverything();

        }

        private void DestroyEverything()
        {
            EDT.Logger.Info("Destorying everything");
            foreach (Texture2D texture2D in m_GrassTextureMap.Values)
            {
                CoreUtils.Destroy(texture2D);
            }
            m_GrassTextureMap.Clear();

            foreach (VisualEffect visualEffect in m_GrassVisualEffectMap.Values)
            {
                visualEffect.Stop();
                GameObject gameObject = visualEffect.gameObject;
                CoreUtils.Destroy(visualEffect);
                CoreUtils.Destroy(gameObject);
            }
            m_GrassVisualEffectMap.Clear();
        }

        private void DestroyEntity(Entity entity)
        {
            EDT.Logger.Info("Destorying everything");

            Texture2D texture = m_GrassTextureMap[entity];
            m_GrassTextureMap.Remove(entity);
            CoreUtils.Destroy(texture);

            VisualEffect visualEffect = m_GrassVisualEffectMap[entity];
            m_GrassVisualEffectMap.Remove(entity);
            visualEffect.Stop();
            GameObject gameObject = visualEffect.gameObject;
            CoreUtils.Destroy(visualEffect);
            CoreUtils.Destroy(gameObject);

        }

#if RELEASE
	[BurstCompile]
#endif
        //[BurstCompile]
        private struct UpdateGrassVFXTextureMap : IJob
        {
            [ReadOnly]
            public NativeArray<ColorRG16> savedTexture;

            [ReadOnly]
            public NativeArray<ColorRG16> vfxGrassTextureMap;

            public void Execute()
            {

                if( savedTexture.Length != vfxGrassTextureMap.Length )
                {
                    //EDT.Logger.Warn("Saved texture lenght isn't the same as vfxGrassTextureMap lenght");
                    return;
                }

                for (int i = 0; i < savedTexture.Length; i++)
                {
                    vfxGrassTextureMap[i] = savedTexture[i];
                }

            }
        }
    }
}


//private void UpdateEffect()
//{
//    Bounds terrainBounds = this.m_TerrainSystem.GetTerrainBounds();
//    this.m_FoliageVFX.SetVector3("TerrainBounds_center", terrainBounds.center);
//    this.m_FoliageVFX.SetVector3("TerrainBounds_size", terrainBounds.size);
//    this.m_FoliageVFX.SetTexture("Terrain HeightMap", this.m_TerrainSystem.heightmap);
//    //this.m_FoliageVFX.SetTexture("Terrain SplatMap", this.m_TerrainMaterialSystem.splatmap);
//    this.m_FoliageVFX.SetTexture("Terrain SplatMap", m_UserPaintedGrassTexture);

//    Vector4 globalVector = Shader.GetGlobalVector("colossal_TerrainScale");
//    Vector4 globalVector2 = Shader.GetGlobalVector("colossal_TerrainOffset");
//    this.m_FoliageVFX.SetVector4("Terrain Offset Scale", new Vector4(globalVector.x, globalVector.z, globalVector2.x, globalVector2.z));
//    this.m_FoliageVFX.SetVector3("CameraPosition", this.m_CameraUpdateSystem.position);
//    this.m_FoliageVFX.SetVector3("CameraDirection", this.m_CameraUpdateSystem.direction);

//    //this.m_FoliageVFX.SetVector2("Crop Size", grassPrefab.CropSize);
//    //this.m_FoliageVFX.SetFloat("FoliageCoverage", grassPrefab.FoliageCoverage > 0 ? grassPrefab.FoliageCoverage : 1);
//    //this.m_FoliageVFX.SetAnimationCurve("Scale Over Distance", grassPrefab.ScaleOverDistance);
//}

//private void UpdateEffectNew()
//{
//    if (!m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(GrassPrefabNew), "GrassPrefabNew"), out PrefabBase prefabBase) || prefabBase is not GrassPrefabNew grassPrefab) return;

//    if(grassPrefab.Grass_Enabled != updated)
//    {
//        EDT.Logger.Info("GrassPrefab has been updated");
//        updated = grassPrefab.Grass_Enabled;
//    }


//    Bounds terrainBounds = this.m_TerrainSystem.GetTerrainBounds();

//    this.m_FoliageVFX.SetVector3    ("CameraPosition", this.m_CameraUpdateSystem.position);
//    this.m_FoliageVFX.SetVector3    ("CameraDirection", this.m_CameraUpdateSystem.direction);
//    this.m_FoliageVFX.SetBool       ("DebugDistantGrass", grassPrefab.DebugDistantGrass);
//    this.m_FoliageVFX.SetBool       ("DebugGrassLOD", grassPrefab.DebugGrassLOD);
//    this.m_FoliageVFX.SetTexture    ("Grass_BaseColorMap", m_GrassBaseColorMap);
//    this.m_FoliageVFX.SetVector4    ("Grass_ColorRandom1", grassPrefab.Grass_ColorRandom1);
//    this.m_FoliageVFX.SetVector4    ("Grass_ColorRandom2", grassPrefab.Grass_ColorRandom2);
//    this.m_FoliageVFX.SetFloat      ("Grass_Coverage", grassPrefab.Grass_Coverage);
//    this.m_FoliageVFX.SetBool       ("Grass_Enabled", grassPrefab.Grass_Enabled);
//    this.m_FoliageVFX.SetVector2    ("Grass_FlipBookSize", grassPrefab.Grass_FlipBookSize);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD0CullDistance", grassPrefab.Grass_LOD0CullDistance);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD0ParticleCount", grassPrefab.Grass_LOD0ParticleCount);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD1CullDistance", grassPrefab.Grass_LOD1CullDistance);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD1ParticleCount", grassPrefab.Grass_LOD1ParticleCount);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD2CullDistance", grassPrefab.Grass_LOD2CullDistance);
//    this.m_FoliageVFX.SetFloat      ("Grass_LOD2ParticleCount", grassPrefab.Grass_LOD2ParticleCount);
//    this.m_FoliageVFX.SetFloat      ("Grass_NoiseScale", grassPrefab.Grass_NoiseScale);
//    //this.m_FoliageVFX.SetTexture    ("Grass_NormalMap", grassPrefab.Grass_NormalMap);
//    this.m_FoliageVFX.SetFloat      ("Grass_NormalScale", grassPrefab.Grass_NormalScale);
//    this.m_FoliageVFX.SetVector2    ("Grass_QuadSize", grassPrefab.Grass_QuadSize);
//    this.m_FoliageVFX.SetFloat      ("Grass_ScaleMultiplier", grassPrefab.Grass_ScaleMultiplier);
//    this.m_FoliageVFX.SetFloat      ("Grass_ScaleRandom", grassPrefab.Grass_ScaleRandom);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex1", grassPrefab.Grass_SplatIndex1);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex2", grassPrefab.Grass_SplatIndex2);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex3", grassPrefab.Grass_SplatIndex3);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex4", grassPrefab.Grass_SplatIndex4);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex5", grassPrefab.Grass_SplatIndex5);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex6", grassPrefab.Grass_SplatIndex6);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex7", grassPrefab.Grass_SplatIndex7);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex8", grassPrefab.Grass_SplatIndex8);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex9", grassPrefab.Grass_SplatIndex9);
//    this.m_FoliageVFX.SetInt        ("Grass_SplatIndex10", grassPrefab.Grass_SplatIndex10);
//    this.m_FoliageVFX.SetVector2    ("Grass_YAngleRange", grassPrefab.Grass_YAngleRange);
//    this.m_FoliageVFX.SetTexture    ("Heightmap", this.m_TerrainSystem.heightmap);
//    this.m_FoliageVFX.SetFloat      ("Heightmap_SamplingScale", grassPrefab.Heightmap_SamplingScale);
//    this.m_FoliageVFX.SetFloat      ("HeightmapYScale", grassPrefab.HeightmapYScale);
//    this.m_FoliageVFX.SetBool       ("IndexOverride", grassPrefab.IndexOverride);

//    this.m_FoliageVFX.SetBool       ("Scatter1_Enabled", grassPrefab.Scatter_Enabled);

//    //this.m_FoliageVFX.SetTexture("Splatmap", m_UserPaintedGrassTexture);
//    this.m_FoliageVFX.SetTexture    ("Splatmap", this.m_TerrainMaterialSystem.splatmap);
//    this.m_FoliageVFX.SetFloat      ("Splatmap_SamplingScale", grassPrefab.Splatmap_SamplingScale);
//    this.m_FoliageVFX.SetFloat      ("Splatmap_WeightBlendStrength", grassPrefab.Splatmap_WeightBlendStrength);
//    this.m_FoliageVFX.SetVector3    ("TerrainBounds_center", terrainBounds.center);
//    this.m_FoliageVFX.SetVector3    ("TerrainBounds_size", terrainBounds.size);
//    this.m_FoliageVFX.SetVector3    ("VolumeScale", grassPrefab.VolumeScale);

//}

//private void CreateDynamicVFXIfNeeded()
//{
//    if (GrassRenderSystem.s_FoliageVFXAsset != null && this.m_FoliageVFX == null)
//    {
//        COSystemBase.baseLog.DebugFormat("Creating FoliageVFX", Array.Empty<object>());
//        this.m_FoliageVFX = new GameObject("FoliageVFX").AddComponent<VisualEffect>();
//        this.m_FoliageVFX.visualEffectAsset = GrassRenderSystem.s_FoliageVFXAsset;
//    }
//}