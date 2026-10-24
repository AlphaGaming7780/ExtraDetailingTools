using Colossal.Collections;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Snapping
{
    // Everything the shared snap functions read, grouped so their signatures stay short.
    // Put it as a field of the SnapJob and pass it by ref. [ReadOnly] on the fields inside the struct is
    // honored by the job safety system.
    public struct SnapContext
    {
        [ReadOnly] public ComponentLookup<Owner> m_OwnerData;
        [ReadOnly] public ComponentLookup<Transform> m_TransformData;
        [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;
        [ReadOnly] public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;
        [ReadOnly] public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;
        [ReadOnly] public ComponentLookup<BuildingData> m_BuildingData;
        [ReadOnly] public ComponentLookup<MovingObjectData> m_MovingObjectData;
        [ReadOnly] public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;
        [ReadOnly] public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;
        [ReadOnly] public ComponentLookup<Game.Common.Terrain> m_TerrainEntityData;

        [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;
        [ReadOnly] public TerrainHeightData m_TerrainHeightData;
        [ReadOnly] public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

        // dependencies: to combine with the job's input deps. Once scheduled, call AddReaders with the job handle.
        public static SnapContext Create(SystemBase system, TerrainSystem terrainSystem, WaterSystem waterSystem, SearchSystem objectSearchSystem, out JobHandle dependencies)
        {
            SnapContext context = new SnapContext
            {
                m_OwnerData = system.GetComponentLookup<Owner>(true),
                m_TransformData = system.GetComponentLookup<Transform>(true),
                m_PrefabRefData = system.GetComponentLookup<PrefabRef>(true),
                m_ObjectGeometryData = system.GetComponentLookup<ObjectGeometryData>(true),
                m_PlaceableObjectData = system.GetComponentLookup<PlaceableObjectData>(true),
                m_BuildingData = system.GetComponentLookup<BuildingData>(true),
                m_MovingObjectData = system.GetComponentLookup<MovingObjectData>(true),
                m_LocalTransformCacheData = system.GetComponentLookup<LocalTransformCache>(true),
                m_ServiceUpgradeData = system.GetComponentLookup<Game.Buildings.ServiceUpgrade>(true),
                m_TerrainEntityData = system.GetComponentLookup<Game.Common.Terrain>(true),
                m_ObjectSearchTree = objectSearchSystem.GetStaticSearchTree(readOnly: true, out JobHandle searchTreeDeps),
                m_TerrainHeightData = terrainSystem.GetHeightData(),
                m_WaterSurfaceData = waterSystem.GetSurfaceData(out JobHandle waterDeps),
            };
            dependencies = JobHandle.CombineDependencies(searchTreeDeps, waterDeps);
            return context;
        }

        // Same readers as the vanilla ObjectToolSystem.SnapControlPoint (it doesn't register a terrain height reader).
        public static void AddReaders(WaterSystem waterSystem, SearchSystem objectSearchSystem, JobHandle jobHandle)
        {
            waterSystem.AddSurfaceReader(jobHandle);
            objectSearchSystem.AddStaticSearchTreeReader(jobHandle);
        }
    }
}
