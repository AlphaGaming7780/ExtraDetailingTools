using Unity.Mathematics;

namespace ExtraDetailingTools.Snapping
{
    // Grid snap shared by the Transform Gizmo tool and its snap job. Pure math, Burst compatible.
    // A step <= 0 disables the snap (the input is returned as is).
    public static class SnapGrid
    {
        // Local orientation: incremental along the axes of localRot. Global: X/Z absolute on the world grid,
        // Y incremental. movedAxes (world) is only used in global, so a single axis drag doesn't snap the other ones.
        public static float3 SnapPosition(float3 origin, float3 pos, float step, bool local, quaternion localRot, bool3 movedAxes)
        {
            if (step <= 0f)
                return pos;

            if (local)
                return SnapPositionLocal(origin, pos, localRot, step);

            float3 result = pos;
            if (movedAxes.x) result.x = math.round(pos.x / step) * step;
            if (movedAxes.y) result.y = origin.y + math.round((pos.y - origin.y) / step) * step;
            if (movedAxes.z) result.z = math.round(pos.z / step) * step;
            return result;
        }

        // Incremental: the offset from origin, expressed along the axes of rot, is rounded to step.
        public static float3 SnapPositionLocal(float3 origin, float3 pos, quaternion rot, float step)
        {
            if (step <= 0f)
                return pos;

            float3 right = math.rotate(rot, new float3(1, 0, 0));
            float3 up = math.rotate(rot, new float3(0, 1, 0));
            float3 forward = math.rotate(rot, new float3(0, 0, 1));

            float3 delta = pos - origin;
            float3 localDelta = new float3(
                math.dot(delta, right),
                math.dot(delta, up),
                math.dot(delta, forward)
            );

            localDelta = math.round(localDelta / step) * step;

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
