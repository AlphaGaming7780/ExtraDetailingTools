using Colossal.Mathematics;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Snapping
{
    // Surface snap shared by the tools, Burst compatible.
    public static class SnapObjectSurface
    {
        // Below a heightmap cell (~3.5 m), the 4 samples fall in the same cell and the slope gets noisy.
        public const float kMinTerrainSampleHalfSize = 1f;

        // Snaps on the hit object surface (top owner), mesh index in m_ElementIndex.x like the vanilla SnapJob.
        public static void SnapSurface(ref SnapContext context, ControlPoint controlPoint, ref ControlPoint bestSnapPosition)
        {
            if (!context.m_TransformData.HasComponent(controlPoint.m_OriginalEntity)) return;

            int parentMesh = controlPoint.m_ElementIndex.x;
            Entity entity = controlPoint.m_OriginalEntity;
            while (context.m_OwnerData.HasComponent(entity))
            {
                if (context.m_LocalTransformCacheData.HasComponent(entity) && !context.m_ServiceUpgradeData.HasComponent(entity))
                {
                    parentMesh = context.m_LocalTransformCacheData[entity].m_ParentMesh;
                    parentMesh += math.select(1000, -1000, parentMesh < 0);
                }
                entity = context.m_OwnerData[entity].m_Owner;
            }
            if (!context.m_TransformData.HasComponent(entity)) return;

            Transform transform = context.m_TransformData[entity];
            ControlPoint snapPosition = controlPoint;
            snapPosition.m_OriginalEntity = entity;
            snapPosition.m_ElementIndex.x = parentMesh;
            snapPosition.m_Position = controlPoint.m_HitPosition;
            snapPosition.m_Direction = math.forward(transform.m_Rotation).xz;
            snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
            ToolUtils.AddSnapPosition(ref bestSnapPosition, snapPosition);
        }

        // Placement offset on an object, terrain / water height otherwise (from vanilla SnapJob.CalculateHeight).
        public static void CalculateHeight(ref SnapContext context, Entity prefab, ref ControlPoint controlPoint)
        {
            if (!context.m_PlaceableObjectData.HasComponent(prefab))
            {
                return;
            }
            PlaceableObjectData placeableObjectData = context.m_PlaceableObjectData[prefab];

            if (context.m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
            {
                controlPoint.m_Position.y += placeableObjectData.m_PlacementOffset.y;
                return;
            }
            float height;
            if ((placeableObjectData.m_Flags & PlacementFlags.RoadSide) != PlacementFlags.None && context.m_BuildingData.HasComponent(prefab))
            {
                BuildingData buildingData = context.m_BuildingData[prefab];
                float3 worldPosition = Game.Buildings.BuildingUtils.CalculateFrontPosition(new Transform(controlPoint.m_Position, controlPoint.m_Rotation), buildingData.m_LotSize.y);
                height = TerrainUtils.SampleHeight(ref context.m_TerrainHeightData, worldPosition);
            }
            else
            {
                height = TerrainUtils.SampleHeight(ref context.m_TerrainHeightData, controlPoint.m_Position);
            }
            if ((placeableObjectData.m_Flags & PlacementFlags.Hovering) != PlacementFlags.None)
            {
                float waterHeight = WaterUtils.SampleHeight(ref context.m_WaterSurfaceData, ref context.m_TerrainHeightData, controlPoint.m_Position);
                waterHeight += placeableObjectData.m_PlacementOffset.y;
                controlPoint.m_Elevation = math.max(0f, waterHeight - height);
                height = math.max(height, waterHeight);
            }
            else if ((placeableObjectData.m_Flags & (PlacementFlags.Shoreline | PlacementFlags.Floating)) == 0)
            {
                height += placeableObjectData.m_PlacementOffset.y;
            }
            else
            {
                float waterHeight = WaterUtils.SampleHeight(ref context.m_WaterSurfaceData, ref context.m_TerrainHeightData, controlPoint.m_Position, out float waterDepth);
                if (waterDepth >= 0.2f)
                {
                    waterHeight += placeableObjectData.m_PlacementOffset.y;
                    if ((placeableObjectData.m_Flags & PlacementFlags.Floating) != PlacementFlags.None)
                    {
                        controlPoint.m_Elevation = math.max(0f, waterHeight - height);
                    }
                    height = math.max(height, waterHeight);
                }
            }
            controlPoint.m_Position.y = height;
        }

        // Tilts to the surface (or upright) and applies the Wall / Hanging offset, terrain read from raw (pre snap point).
        public static void AlignToSurface(ref SnapContext context, Entity prefab, bool objectSurface, bool upright, bool editorMode, ControlPoint raw, ref ControlPoint controlPoint, ref quaternion parentRotation, bool alignRotation)
        {
            context.m_PlaceableObjectData.TryGetComponent(prefab, out PlaceableObjectData placeableObjectData);
            bool hasGeometry = context.m_ObjectGeometryData.TryGetComponent(prefab, out ObjectGeometryData geometryData);

            // The terrain hit normal is unusable (heightmap space), replace it by the slope under the object's footprint.
            if (objectSurface && !upright && hasGeometry
                && context.m_TerrainEntityData.HasComponent(raw.m_OriginalEntity)
                && !context.m_TransformData.HasComponent(controlPoint.m_OriginalEntity)
                && (placeableObjectData.m_Flags & PlacementFlags.Wall) == PlacementFlags.None)
            {
                controlPoint.m_HitDirection = CalculateTerrainNormal(ref context.m_TerrainHeightData, controlPoint.m_Position, controlPoint.m_Rotation, geometryData.m_Bounds);
            }

            AlignObject(ref context, prefab, upright, ref controlPoint, ref parentRotation, alignRotation);

            if ((controlPoint.m_OriginalEntity == Entity.Null || controlPoint.m_ElementIndex.x == -1 || controlPoint.m_HitDirection.y > 0.99f)
                && hasGeometry
                && geometryData.m_Bounds.min.y <= -0.01f
                && (((placeableObjectData.m_Flags & (PlacementFlags.Wall | PlacementFlags.Hanging)) != PlacementFlags.None && upright)
                    || (editorMode && context.m_MovingObjectData.HasComponent(prefab))))
            {
                controlPoint.m_Elevation -= geometryData.m_Bounds.min.y;
                controlPoint.m_Position.y -= geometryData.m_Bounds.min.y;
            }
        }

        // The terrain raycast normal is in heightmap space: use the plane through 4 footprint heights (yaw only).
        public static float3 CalculateTerrainNormal(ref TerrainHeightData terrain, float3 position, quaternion rotation, Bounds3 localBounds, float minHalfSize = kMinTerrainSampleHalfSize)
        {
            float3 forward = math.forward(rotation);
            forward.y = 0f;
            if (!MathUtils.TryNormalize(ref forward)) forward = new float3(0f, 0f, 1f);
            float3 right = new float3(forward.z, 0f, -forward.x);

            float2 center = (localBounds.min.xz + localBounds.max.xz) * 0.5f;
            float2 half = math.max((localBounds.max.xz - localBounds.min.xz) * 0.5f, minHalfSize);
            float2 min = center - half;
            float2 max = center + half;

            float3 backLeft = SampleCorner(ref terrain, position, right, forward, min.x, min.y);
            float3 backRight = SampleCorner(ref terrain, position, right, forward, max.x, min.y);
            float3 frontRight = SampleCorner(ref terrain, position, right, forward, max.x, max.y);
            float3 frontLeft = SampleCorner(ref terrain, position, right, forward, min.x, max.y);

            // The 4 corners are rarely coplanar: the cross product of the diagonals gives the average plane.
            float3 normal = math.cross(frontRight - backLeft, frontLeft - backRight);
            normal = math.select(normal, -normal, normal.y < 0f);
            return math.normalizesafe(normal, math.up());
        }

        // From vanilla SnapJob.AlignObject: Wall objects face the normal, the others align their up axis (or stay upright).
        private static void AlignObject(ref SnapContext context, Entity prefab, bool upright, ref ControlPoint controlPoint, ref quaternion parentRotation, bool alignRotation)
        {
            PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
            if (context.m_PlaceableObjectData.HasComponent(prefab))
            {
                placeableObjectData = context.m_PlaceableObjectData[prefab];
            }
            if ((placeableObjectData.m_Flags & PlacementFlags.Hanging) != PlacementFlags.None)
            {
                ObjectGeometryData objectGeometryData = context.m_ObjectGeometryData[prefab];
                controlPoint.m_Position.y -= objectGeometryData.m_Bounds.max.y;
            }
            parentRotation = quaternion.identity;
            if (context.m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
            {
                Entity entity = controlPoint.m_OriginalEntity;
                PrefabRef prefabRef = context.m_PrefabRefData[entity];
                parentRotation = context.m_TransformData[entity].m_Rotation;
                while (context.m_OwnerData.HasComponent(entity) && !context.m_BuildingData.HasComponent(prefabRef.m_Prefab))
                {
                    entity = context.m_OwnerData[entity].m_Owner;
                    prefabRef = context.m_PrefabRefData[entity];
                    if (context.m_TransformData.HasComponent(entity))
                    {
                        parentRotation = context.m_TransformData[entity].m_Rotation;
                    }
                }
            }
            if ((placeableObjectData.m_Flags & PlacementFlags.Wall) != PlacementFlags.None)
            {
                float3 @float = math.forward(controlPoint.m_Rotation);
                float3 value = controlPoint.m_HitDirection;
                value.y = math.select(value.y, 0f, upright);
                if (!MathUtils.TryNormalize(ref value))
                {
                    value = @float;
                    value.y = math.select(value.y, 0f, upright);
                    if (!MathUtils.TryNormalize(ref value))
                    {
                        value = new float3(0f, 0f, 1f);
                    }
                }
                float3 value2 = math.cross(@float, value);
                if (MathUtils.TryNormalize(ref value2))
                {
                    float angle = math.acos(math.clamp(math.dot(@float, value), -1f, 1f));
                    controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value2, angle), controlPoint.m_Rotation), quaternion.identity);
                    if (alignRotation)
                    {
                        SnapMath.AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: true);
                    }
                }
                controlPoint.m_Position += math.forward(controlPoint.m_Rotation) * placeableObjectData.m_PlacementOffset.z;
                return;
            }
            float3 float2 = math.rotate(controlPoint.m_Rotation, new float3(0f, 1f, 0f));
            float3 hitDirection = controlPoint.m_HitDirection;
            hitDirection = math.select(hitDirection, new float3(0f, 1f, 0f), upright);
            if (!MathUtils.TryNormalize(ref hitDirection))
            {
                hitDirection = float2;
            }
            float3 value3 = math.cross(float2, hitDirection);
            if (MathUtils.TryNormalize(ref value3))
            {
                float angle2 = math.acos(math.clamp(math.dot(float2, hitDirection), -1f, 1f));
                controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value3, angle2), controlPoint.m_Rotation), quaternion.identity);
                if (alignRotation)
                {
                    SnapMath.AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: false);
                }
            }
        }

        private static float3 SampleCorner(ref TerrainHeightData terrain, float3 position, float3 right, float3 forward, float x, float z)
        {
            float3 corner = position + right * x + forward * z;
            corner.y = TerrainUtils.SampleHeight(ref terrain, corner);
            return corner;
        }
    }
}
