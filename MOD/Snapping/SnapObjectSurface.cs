using Colossal.Mathematics;
using Game.Simulation;
using Unity.Mathematics;

namespace ExtraDetailingTools.Snapping
{
    // Surface snapping shared by the tools (ExtraSnap ObjectTool, TransformGizmoTool FollowSurface, ...).
    // Static and unmanaged so it can run inside a Burst job.
    public static class SnapObjectSurface
    {
        // Below a heightmap cell (~3.5 m), the 4 samples fall in the same cell and the slope gets noisy.
        public const float kMinTerrainSampleHalfSize = 1f;

        // The terrain raycast normal (TerrainUtils.Raycast / SampleHeight(out normal)) is computed in heightmap
        // space (cells / raw height units) and never scaled back to world space, so it's unusable. Instead,
        // sample the world height at the 4 corners of the object's footprint and use the plane through them.
        //
        // localBounds: ObjectGeometryData.m_Bounds of the object, relative to its origin.
        // Only the yaw of rotation is used, so the result doesn't depend on the current tilt.
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

        // Tilts rotation so its up axis matches up, keeping its yaw.
        public static quaternion AlignUp(quaternion rotation, float3 up)
        {
            float3 forward = math.forward(rotation);
            forward.y = 0f;
            if (!MathUtils.TryNormalize(ref forward)) forward = new float3(0f, 0f, 1f);

            // Forward projected on the surface plane.
            float3 surfaceForward = forward - up * math.dot(forward, up);
            if (!MathUtils.TryNormalize(ref surfaceForward)) return rotation;
            return quaternion.LookRotationSafe(surfaceForward, up);
        }

        private static float3 SampleCorner(ref TerrainHeightData terrain, float3 position, float3 right, float3 forward, float x, float z)
        {
            float3 corner = position + right * x + forward * z;
            corner.y = TerrainUtils.SampleHeight(ref terrain, corner);
            return corner;
        }
    }
}
