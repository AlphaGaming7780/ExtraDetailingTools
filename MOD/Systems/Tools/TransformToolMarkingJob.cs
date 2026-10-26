using Colossal;
using ExtraDetailingTools.Snapping;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool
    {
        // Markings of the active snaps (grid lines, rotation ticks). Drawn with the GizmoBatcher: nothing to raycast, so no entity.
        // The drawing itself lives with each snap (SnapGrid.DrawPositionGrid...), this job only picks what applies to the tool.
#if RELEASE
        [BurstCompile]
#endif
        private struct TransformToolMarkingJob : IJob
        {
            [ReadOnly] public Mode m_Mode;
            [ReadOnly] public Handle m_SelectedHandle;
            [ReadOnly] public bool m_UseLocalAxis;
            [ReadOnly] public XZHandleMode m_XZHandleMode;
            [ReadOnly] public float3 m_Position;
            [ReadOnly] public quaternion m_Rotation;
            // Dragging: the gizmo written by TransformToolSnapJob is used instead of m_Position / m_Rotation.
            [ReadOnly] public bool m_UseDragGizmo;
            [ReadOnly] public NativeReference<DragGizmoResult> m_DragGizmo;

            [ReadOnly] public float3 m_CameraPosition;
            [ReadOnly] public float m_TanFOV;
            [ReadOnly] public float m_HandleScreenSize;

            // Grid. m_GridOrigin: where the incremental steps start (drag start, or the gizmo when idle).
            [ReadOnly] public bool m_GridEnabled;
            [ReadOnly] public float3 m_GridOrigin;
            [ReadOnly] public float3 m_PosStep;
            [ReadOnly] public float3 m_RotStep;
            [ReadOnly] public float3 m_DragAxis;
            [ReadOnly] public quaternion m_DragStartGizmoRot;
            [ReadOnly] public quaternion m_DragStartPrimaryRot;
            [ReadOnly] public float3 m_DragStartDir;

            [ReadOnly] public GizmoBatcher m_Batcher;

            public void Execute()
            {
                float3 pos = m_UseDragGizmo ? m_DragGizmo.Value.m_Position : m_Position;
                quaternion rot = m_UseDragGizmo ? m_DragGizmo.Value.m_Rotation : m_Rotation;

                if (m_GridEnabled)
                {
                    if (m_Mode == Mode.Move) DrawMoveGrid(pos, rot);
                    else if (m_Mode == Mode.Rotate && m_UseDragGizmo) DrawRotateTicks(pos, rot, GetHandleSize(pos, m_CameraPosition, m_TanFOV, m_HandleScreenSize));
                }
            }

            // Grid of the plane the drag moves in.
            private void DrawMoveGrid(float3 pos, quaternion rot)
            {
                if (m_UseDragGizmo && m_SelectedHandle == Handle.XZ && m_XZHandleMode == XZHandleMode.FollowSurface) return;

                int normal = 1;
                if (m_SelectedHandle == Handle.Y)
                {
                    // Vertical plane, the one facing the camera the most.
                    float3x3 axes = m_UseLocalAxis ? new float3x3(rot) : float3x3.identity;
                    float3 view = math.normalizesafe(pos - m_CameraPosition);
                    normal = math.abs(math.dot(axes.c0, view)) > math.abs(math.dot(axes.c2, view)) ? 0 : 2;
                }
                else if (m_SelectedHandle == Handle.XZ)
                {
                    normal = m_XZHandleMode == XZHandleMode.FixedX ? 0 : m_XZHandleMode == XZHandleMode.FixedZ ? 2 : 1;
                }

                SnapGrid.DrawPositionGrid(m_Batcher, m_GridOrigin, pos, m_PosStep, m_UseLocalAxis, rot, normal);
            }

            // Ticks on the dragged ring.
            private void DrawRotateTicks(float3 pos, quaternion rot, float radius)
            {
                if (m_SelectedHandle != Handle.X && m_SelectedHandle != Handle.Y && m_SelectedHandle != Handle.Z) return;

                float3 axis = m_DragAxis;
                float3 startDir = math.normalizesafe(m_DragStartDir - axis * math.dot(m_DragStartDir, axis));
                if (math.lengthsq(startDir) < 1e-6f) return;

                // Angle so far, from the gizmo rotation written by TransformToolSnapJob.
                float3 currentDir = math.rotate(math.mul(rot, math.inverse(m_DragStartGizmoRot)), startDir);
                float angle = math.atan2(math.dot(axis, math.cross(startDir, currentDir)), math.dot(startDir, currentDir));

                bool absoluteYaw = !m_UseLocalAxis && m_SelectedHandle == Handle.Y;
                SnapGrid.DrawAngleTicks(m_Batcher, pos, axis, startDir, radius, angle, GetRotStep(m_RotStep, m_SelectedHandle), absoluteYaw, m_DragStartPrimaryRot);
            }
        }

        // Markings of the gizmo (same position / rotation as UpdateGizmosJob).
        private JobHandle UpdateMarkings(JobHandle inputDeps, bool hasGizmo, float3 position, quaternion rotation, bool useDragGizmo, Camera cam, float tanFOV)
        {
            if (!hasGizmo || !m_GridEnabled || (m_Mode != Mode.Move && m_Mode != Mode.Rotate)) return inputDeps;

            JobHandle jobHandle = new TransformToolMarkingJob
            {
                m_Mode = m_Mode,
                m_SelectedHandle = m_SelectedHandle,
                m_UseLocalAxis = m_UseLocalAxis,
                m_XZHandleMode = m_XZHandleMode,
                m_Position = position,
                m_Rotation = rotation,
                m_UseDragGizmo = useDragGizmo,
                m_DragGizmo = m_DragGizmo,
                m_CameraPosition = cam.transform.position,
                m_TanFOV = tanFOV,
                m_HandleScreenSize = EDT.m_Settings.HandleScreenSize,
                m_GridEnabled = m_GridEnabled,
                m_GridOrigin = useDragGizmo ? m_DragStartGizmoPos : position,
                m_PosStep = m_PosOffset,
                m_RotStep = m_RotOffset,
                m_DragAxis = GetSelectedAxisDirection(m_SelectedHandle, m_DragStartGizmoRot),
                m_DragStartGizmoRot = m_DragStartGizmoRot,
                m_DragStartPrimaryRot = m_DragStartPrimaryRot,
                m_DragStartDir = m_DragStartMouseHitPos,
                m_Batcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle batcherDeps),
            }.Schedule(JobHandle.CombineDependencies(inputDeps, batcherDeps, m_DragGizmoDeps));
            m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
            m_DragGizmoDeps = JobHandle.CombineDependencies(m_DragGizmoDeps, jobHandle);
            return jobHandle;
        }
    }
}
