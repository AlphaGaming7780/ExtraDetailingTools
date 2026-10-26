using Colossal;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Snapping
{
    // Grid snap and its markings, Burst compatible. A step <= 0 disables it on that axis.
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

        // Markings: lines / ticks drawn on each side of the current step.
        private const int kMarkingHalfCount = 2;
        private static readonly Color kMarkingColor = new(1f, 1f, 1f, 0.3f);
        private static readonly Color kMarkingCenterColor = new(1f, 1f, 1f, 0.6f);

        // Lines of the plane of normal axis normalAxis (0: X, 1: Y, 2: Z) around pos, on the lattice of SnapPosition.
        public static void DrawPositionGrid(GizmoBatcher batcher, float3 origin, float3 pos, float3 step, bool local, quaternion localRot, int normalAxis)
        {
            int u = (normalAxis + 1) % 3;
            int v = (normalAxis + 2) % 3;
            float su = step[u];
            float sv = step[v];
            if (su <= 0f || sv <= 0f) return;

            float3x3 axes = local ? new float3x3(localRot) : float3x3.identity;
            float3 axisU = axes[u];
            float3 axisV = axes[v];
            float3 axisN = axes[normalAxis];

            // Global: X / Z on the world grid, Y relative to origin.
            float3 latticeOrigin = local ? origin : new float3(0f, origin.y, 0f);
            float3 rel = pos - latticeOrigin;
            float3 center = latticeOrigin
                + axisU * math.round(math.dot(rel, axisU) / su) * su
                + axisV * math.round(math.dot(rel, axisV) / sv) * sv
                + axisN * math.dot(rel, axisN);

            float3 halfU = axisU * su * kMarkingHalfCount;
            float3 halfV = axisV * sv * kMarkingHalfCount;
            for (int i = -kMarkingHalfCount; i <= kMarkingHalfCount; i++)
            {
                Color color = i == 0 ? kMarkingCenterColor : kMarkingColor;
                float3 lineU = center + axisU * su * i;
                float3 lineV = center + axisV * sv * i;
                batcher.DrawLine(lineU - halfV, lineU + halfV, color);
                batcher.DrawLine(lineV - halfU, lineV + halfU, color);
            }
        }

        // Ticks on the ring (center, axis, radius) at the angles SnapAngle allows around angle (radians from startDir).
        public static void DrawAngleTicks(GizmoBatcher batcher, float3 center, float3 axis, float3 startDir, float radius, float angle, float stepDegrees, bool absoluteYaw, quaternion refRot)
        {
            float step = math.radians(stepDegrees);
            if (step <= 0f) return;

            // Absolute yaw: ticks shifted by the start yaw.
            float yaw = absoluteYaw ? SnapMath.GetYaw(refRot) : 0f;
            float current = math.round((angle + yaw) / step);

            for (int i = -kMarkingHalfCount; i <= kMarkingHalfCount; i++)
            {
                float3 dir = math.rotate(quaternion.AxisAngle(axis, (current + i) * step - yaw), startDir);
                float length = i == 0 ? 0.2f : 0.12f;
                batcher.DrawLine(center + dir * radius * (1f - length), center + dir * radius * (1f + length), i == 0 ? kMarkingCenterColor : kMarkingColor);
            }
        }
    }
}
