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

        // Copy of the private ObjectToolSystem.AlignRotation: 45 degrees steps of the parent rotation around the normal.
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
    }
}
