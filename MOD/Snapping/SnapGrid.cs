using Unity.Mathematics;

namespace ExtraDetailingTools.Snapping
{
    // Grid snap, Burst compatible. A step <= 0 disables it on that axis.
    public static class SnapGrid
    {
        // Local: incremental along localRot. Global: X/Z on the world grid (movedAxes only), Y incremental.
        // step: one per axis (X, Y, Z of the grid).
        public static float3 SnapPosition(float3 origin, float3 pos, float3 step, bool local, quaternion localRot, bool3 movedAxes)
        {
            if (local)
                return SnapPositionLocal(origin, pos, localRot, step);

            float3 result = pos;
            if (movedAxes.x && step.x > 0f) result.x = math.round(pos.x / step.x) * step.x;
            if (movedAxes.y && step.y > 0f) result.y = origin.y + math.round((pos.y - origin.y) / step.y) * step.y;
            if (movedAxes.z && step.z > 0f) result.z = math.round(pos.z / step.z) * step.z;
            return result;
        }

        // Incremental: the offset from origin, expressed along the axes of rot, is rounded to step.
        public static float3 SnapPositionLocal(float3 origin, float3 pos, quaternion rot, float3 step)
        {
            float3 right = math.rotate(rot, new float3(1, 0, 0));
            float3 up = math.rotate(rot, new float3(0, 1, 0));
            float3 forward = math.rotate(rot, new float3(0, 0, 1));

            float3 delta = pos - origin;
            float3 localDelta = new float3(
                math.dot(delta, right),
                math.dot(delta, up),
                math.dot(delta, forward)
            );

            localDelta = math.select(localDelta, math.round(localDelta / step) * step, step > 0f);

            return origin + right * localDelta.x + up * localDelta.y + forward * localDelta.z;
        }

        // absoluteYaw (global + Y handle): the final yaw of refRot lands on the grid. Otherwise the angle is incremental.
        public static float SnapAngle(float angle, float stepDegrees, bool absoluteYaw, quaternion refRot)
        {
            if (absoluteYaw)
            {
                float yaw = SnapMath.GetYaw(refRot);
                return SnapAngleIncremental(yaw + angle, stepDegrees) - yaw;
            }

            return SnapAngleIncremental(angle, stepDegrees);
        }

        // angle in radians, rounded to a multiple of stepDegrees.
        public static float SnapAngleIncremental(float angle, float stepDegrees)
        {
            float stepRad = math.radians(stepDegrees);
            if (stepRad <= 0f)
                return angle;

            return math.round(angle / stepRad) * stepRad;
        }
    }
}
