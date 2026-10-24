using Colossal.Mathematics;
using System;
using Unity.Mathematics;

namespace ExtraDetailingTools.Snapping
{
    // Rotation helpers shared by the snaps. Pure math, Burst compatible.
    public static class SnapMath
    {
        // Yaw of q rounded to the nearest multiple of 90 degrees, tilt dropped.
        public static quaternion ClampTo90(quaternion q)
        {
            float3 forward = math.mul(q, new float3(0, 0, 1));
            float yaw = math.atan2(forward.x, forward.z);
            float snappedYaw = math.round(yaw / (math.PI / 2f)) * (math.PI / 2f);
            return quaternion.RotateY(snappedYaw);
        }

        // Yaw (radians) of the forward axis of rot, around the world Y axis.
        public static float GetYaw(quaternion rot)
        {
            float3 forward = math.rotate(rot, new float3(0, 0, 1));
            return math.atan2(forward.x, forward.z);
        }

        // Copy of the vanilla ObjectToolSystem.AlignRotation (private): snaps the rotation around the surface
        // normal to the closest 45 degrees step of the parent rotation.
        public static void AlignRotation(ref quaternion rotation, quaternion parentRotation, bool zAxis)
        {
            if (zAxis)
            {
                float3 forward = math.rotate(rotation, new float3(0f, 0f, 1f));
                float3 up = math.rotate(parentRotation, new float3(0f, 1f, 0f));
                quaternion a = quaternion.LookRotationSafe(forward, up);
                quaternion q = rotation;
                float num = float.MaxValue;
                for (int i = 0; i < 8; i++)
                {
                    quaternion quaternion = math.mul(a, quaternion.RotateZ((float)i * (MathF.PI / 4f)));
                    float num2 = MathUtils.RotationAngle(rotation, quaternion);
                    if (num2 < num)
                    {
                        q = quaternion;
                        num = num2;
                    }
                }
                rotation = math.normalizesafe(q, quaternion.identity);
                return;
            }
            float3 forward2 = math.rotate(rotation, new float3(0f, 1f, 0f));
            float3 up2 = math.rotate(parentRotation, new float3(1f, 0f, 0f));
            quaternion a2 = math.mul(quaternion.LookRotationSafe(forward2, up2), quaternion.RotateX(MathF.PI / 2f));
            quaternion q2 = rotation;
            float num3 = float.MaxValue;
            for (int j = 0; j < 8; j++)
            {
                quaternion quaternion2 = math.mul(a2, quaternion.RotateY((float)j * (MathF.PI / 4f)));
                float num4 = MathUtils.RotationAngle(rotation, quaternion2);
                if (num4 < num3)
                {
                    q2 = quaternion2;
                    num3 = num4;
                }
            }
            rotation = math.normalizesafe(q2, quaternion.identity);
        }

        // Unity.Mathematics has no FromToRotation - shortest rotation that takes "from" onto "to", built
        // from the standard axis/angle-between-two-vectors construction (atan2(|cross|, dot) is the robust
        // way to get the angle, avoiding the precision loss acos(dot) has near 0/180 degrees).
        public static quaternion ShortestRotation(float3 from, float3 to)
        {
            // NaN comparisons are always false, so a degenerate input wouldn't be caught by the
            // axisLength < 1e-6f check below - it'd fall through to atan2/AxisAngle and silently produce a
            // NaN quaternion instead. Matches LookRotationSafe's own isfinite guard.
            if (!math.all(math.isfinite(from)) || !math.all(math.isfinite(to)))
            {
                return quaternion.identity;
            }

            float3 axis = math.cross(from, to);
            float axisLength = math.length(axis);
            float dot = math.dot(from, to);

            if (axisLength < 1e-6f)
            {
                if (dot > 0f)
                {
                    return quaternion.identity;
                }

                // from/to are anti-parallel - cross product can't give an axis, pick any perpendicular one.
                float3 reference = math.abs(from.x) < 0.99f ? new float3(1f, 0f, 0f) : new float3(0f, 1f, 0f);
                float3 perpendicular = math.normalize(math.cross(from, reference));
                return quaternion.AxisAngle(perpendicular, math.PI);
            }

            float angle = math.atan2(axisLength, dot);
            return quaternion.AxisAngle(axis / axisLength, angle);
        }
    }
}
