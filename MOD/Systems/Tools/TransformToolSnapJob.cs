using Colossal.Mathematics;
using ExtraDetailingTools.Snapping;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace ExtraDetailingTools.Systems.Tools
{
    public partial class TransformGizmoTool
    {
        // Gizmo position / rotation of the drag, written by TransformToolSnapJob (kept as is without hit).
        private struct DragGizmoResult
        {
            public float3 m_Position;
            public quaternion m_Rotation;
        }

        // Whole drag of a frame (plane, grid, FollowSurface, Object Side, delta per entity), snaps compete via ToolUtils.AddSnapPosition.
#if RELEASE
        [BurstCompile]
#endif
        private struct TransformToolSnapJob : IJob
        {
            [ReadOnly] public Mode m_Mode;
            [ReadOnly] public Handle m_Handle;
            [ReadOnly] public bool m_UseLocalAxis;
            [ReadOnly] public Entity m_SelectedEntity; // Original of the primary entity.

            // Camera ray (axis / plane drag).
            [ReadOnly] public float3 m_RayOrigin;
            [ReadOnly] public float3 m_RayDirection;
            [ReadOnly] public float3 m_CameraRight;
            [ReadOnly] public float3 m_CameraForward;
            [ReadOnly] public Line3.Segment m_RaycastLine; // Same line as the tool raycast (FollowSurface).

            // Drag start, see StartDragging.
            [ReadOnly] public float3 m_AxisDir;
            [ReadOnly] public float3 m_DragStartGizmoPos;
            [ReadOnly] public float3 m_DragStartSurfacePos; // Group pivot from the origins, put on the surface by FollowSurface.
            [ReadOnly] public quaternion m_DragStartGizmoRot;
            [ReadOnly] public quaternion m_DragStartPrimaryRot;
            [ReadOnly] public float3 m_DragStartMouseHitPos;

            // Sphere Mode. FollowSurface: m_ControlPoint is built from the raycast hit, with the drag start rotation.
            [ReadOnly] public XZHandleMode m_XZHandleMode;
            [ReadOnly] public bool m_HasHit;
            [ReadOnly] public ControlPoint m_ControlPoint;
            [ReadOnly] public bool m_Upright;
            [ReadOnly] public bool m_EditorMode;

            // Grid Snap. Individual: every entity snaps on its own.
            [ReadOnly] public bool m_GridEnabled;
            [ReadOnly] public bool m_SnapEach;
            [ReadOnly] public float m_PosOffset;
            [ReadOnly] public float m_RotOffset;

            // Object Side Snap, Move only. Not enabled by the tool yet.
            [ReadOnly] public bool m_ObjectSideSnapEnabled;
            [ReadOnly] public ObjectSideSnapMode m_ObjectSideSnapMode;

            public SnapContext m_Context;

            // One per drag entry.
            [ReadOnly] public NativeArray<Entity> m_Originals;
            [ReadOnly] public NativeArray<Transform> m_StartTransforms;
            [ReadOnly] public NativeArray<float3> m_Pivots;
            [ReadOnly] public int m_PrimaryIndex;

            public NativeArray<Transform> m_Results;
            public NativeReference<DragGizmoResult> m_Gizmo;

            private struct Delta
            {
                public float3 Position;
                public quaternion Rotation;
                // Raw (unsnapped) angle, used when each entity snaps on its own.
                public float Angle;
                public bool3 MovedAxes;
                // FollowSurface Individual: terrain under the primary, Null on an object / underground (the others just follow).
                public Entity SurfaceTerrain;
            }

            public void Execute()
            {
                Delta delta = new Delta
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity,
                };

                bool valid = IsFollowSurface()
                    ? FollowSurface(ref delta)
                    : DragPlane(ref delta);

                // No hit: default transforms (UpdateObjectJob keeps the current ones), the gizmo isn't written.
                if (!valid)
                {
                    for (int i = 0; i < m_Results.Length; i++) m_Results[i] = default;
                    return;
                }

                ApplyDelta(delta);
            }

            private bool IsFollowSurface()
            {
                return m_XZHandleMode == XZHandleMode.FollowSurface && m_Handle == Handle.XZ;
            }

            private bool FollowSurface(ref Delta delta)
            {
                if (!m_HasHit) return false;

                ControlPoint raw = m_ControlPoint;
                bool onWater = FixWaterHit(ref raw);

                // Same baseline as the vanilla SnapJob: the hit entity is kept only if the surface snap uses it.
                ControlPoint best = raw;
                best.m_OriginalEntity = Entity.Null;
                SnapObjectSurface.SnapSurface(ref m_Context, raw, ref best);

                // Tilt only for a single object or in Individual, a group just moves its pivot on the hit.
                if ((m_StartTransforms.Length != 1 && !m_SnapEach) || !m_Context.m_PrefabRefData.TryGetComponent(m_SelectedEntity, out PrefabRef prefabRef))
                {
                    delta.Position = best.m_Position - m_DragStartSurfacePos;
                    return true;
                }

                // Unlike ExtraSnap: no CalculateHeight (stays on the hit) and no 45 degrees alignment on the parent.
                quaternion parentRotation = quaternion.identity;
                SnapObjectSurface.AlignToSurface(ref m_Context, prefabRef.m_Prefab, true, m_Upright || onWater, m_EditorMode, raw, ref best, ref parentRotation, false);

                // The origin of the object goes on the snapped point, whatever the pivot:
                // best = pivot + posDelta + rotDelta * (start - pivot).
                delta.Rotation = math.normalize(math.mul(best.m_Rotation, math.inverse(m_DragStartPrimaryRot)));
                delta.Position = best.m_Position - m_Pivots[m_PrimaryIndex] - math.rotate(delta.Rotation, m_StartTransforms[m_PrimaryIndex].m_Position - m_Pivots[m_PrimaryIndex]);

                if (m_Context.m_TerrainEntityData.HasComponent(raw.m_OriginalEntity) && !m_Context.m_TransformData.HasComponent(best.m_OriginalEntity))
                {
                    delta.SurfaceTerrain = raw.m_OriginalEntity;
                }
                return true;
            }

            // FollowSurface Individual, non primary: same XZ move, then put on the surface and tilted like the primary.
            private Transform FollowSurfaceEach(int index, Delta delta)
            {
                Transform start = m_StartTransforms[index];
                float3 position = start.m_Position + delta.Position;

                // Primary on an object / underground: no surface to sample for the others, they keep their offset.
                if (delta.SurfaceTerrain == Entity.Null || !m_Context.m_PrefabRefData.TryGetComponent(m_Originals[index], out PrefabRef prefabRef))
                {
                    return new Transform(position, start.m_Rotation);
                }

                // Same water threshold as FixWaterHit.
                float waterHeight = WaterUtils.SampleHeight(ref m_Context.m_WaterSurfaceData, ref m_Context.m_TerrainHeightData, position, out float waterDepth);
                bool onWater = waterDepth >= 0.2f;
                position.y = onWater ? waterHeight : TerrainUtils.SampleHeight(ref m_Context.m_TerrainHeightData, position);

                ControlPoint raw = default;
                raw.m_OriginalEntity = delta.SurfaceTerrain;
                raw.m_Position = position;
                raw.m_HitPosition = position;
                raw.m_HitDirection = math.up();
                raw.m_Rotation = start.m_Rotation;

                ControlPoint best = raw;
                best.m_OriginalEntity = Entity.Null;
                quaternion parentRotation = quaternion.identity;
                SnapObjectSurface.AlignToSurface(ref m_Context, prefabRef.m_Prefab, true, m_Upright || onWater, m_EditorMode, raw, ref best, ref parentRotation, false);

                return new Transform(best.m_Position, best.m_Rotation);
            }

            // On a dry slope the coarse water grid hits above the terrain: keep the terrain hit unless on real water (returns true).
            private bool FixWaterHit(ref ControlPoint raw)
            {
                // Terrain hit (or water not raycasted): nothing to fix, no extra raycast.
                if (math.any(raw.m_HitDirection != 0f) || !m_Context.m_TerrainEntityData.HasComponent(raw.m_OriginalEntity)) return false;

                WaterUtils.SampleHeight(ref m_Context.m_WaterSurfaceData, ref m_Context.m_TerrainHeightData, raw.m_HitPosition, out float waterDepth);
                if (waterDepth >= 0.2f) return true;

                if (!TerrainUtils.Raycast(ref m_Context.m_TerrainHeightData, m_RaycastLine, false, out float t, out float3 normal, out _)) return false;

                raw.m_Position = MathUtils.Position(m_RaycastLine, t);
                raw.m_HitPosition = raw.m_Position;
                raw.m_HitDirection = normal;
                return false;
            }

            private bool DragPlane(ref Delta delta)
            {
                bool axisIsNormal = m_Mode == Mode.Rotate || m_Handle == Handle.XZ;
                if (!RaycastDragPlane(m_RayOrigin, m_RayDirection, m_AxisDir, m_DragStartGizmoPos, axisIsNormal, m_CameraRight, m_CameraForward, out float3 hitPos)) return false;

                delta.MovedAxes = GetMovedAxes(m_Handle, m_AxisDir);

                if (m_Mode == Mode.Move)
                {
                    float3 mouseDelta = hitPos - m_DragStartMouseHitPos;
                    float3 newPos = m_Handle == Handle.XZ
                        ? m_DragStartGizmoPos + mouseDelta
                        : m_DragStartGizmoPos + math.dot(mouseDelta, m_AxisDir) * m_AxisDir;

                    ControlPoint raw = default;
                    raw.m_Position = newPos;
                    raw.m_HitPosition = newPos;
                    raw.m_Rotation = m_DragStartPrimaryRot;
                    ControlPoint best = raw;

                    if (m_GridEnabled && !m_SnapEach)
                    {
                        ControlPoint gridCandidate = raw;
                        gridCandidate.m_Position = SnapPosition(m_DragStartGizmoPos, newPos, delta.MovedAxes);
                        gridCandidate.m_HitPosition = gridCandidate.m_Position;
                        // Same scoring as ObjectTool edge snaps, level 1 so it beats the raw point.
                        gridCandidate.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, gridCandidate.m_Position - newPos);
                        ToolUtils.AddSnapPosition(ref best, gridCandidate);
                    }

                    // Only the position is used, the rotation of the selection doesn't change in Move.
                    if (m_ObjectSideSnapEnabled && m_Context.m_PrefabRefData.TryGetComponent(m_SelectedEntity, out PrefabRef prefabRef)
                        && m_Context.m_ObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData placedGeometryData))
                    {
                        SnapObjectSide.Apply(ref m_Context, m_ObjectSideSnapMode, raw, ref best, raw.m_Rotation, placedGeometryData);
                    }

                    delta.Position = best.m_Position - m_DragStartGizmoPos;
                    return true;
                }

                if (m_Mode == Mode.Rotate)
                {
                    // m_DragStartMouseHitPos is the start direction in Rotate.
                    float3 currentDir = math.normalize(hitPos - m_DragStartGizmoPos);
                    float angle = math.atan2(
                        math.dot(m_AxisDir, math.cross(m_DragStartMouseHitPos, currentDir)),
                        math.dot(m_DragStartMouseHitPos, currentDir)
                    );

                    delta.Angle = angle;
                    if (m_GridEnabled && !m_SnapEach)
                    {
                        angle = SnapAngle(angle, m_DragStartPrimaryRot);
                    }
                    delta.Rotation = quaternion.AxisAngle(m_AxisDir, angle);
                    return true;
                }

                return false;
            }

            // Every entity keeps its offset to its pivot: pos = pivot + posDelta + rotDelta * (start - pivot).
            private void ApplyDelta(Delta delta)
            {
                float3 gizmoPosDelta = delta.Position;
                quaternion gizmoRotDelta = delta.Rotation;
                bool snapEach = m_GridEnabled && m_SnapEach && !IsFollowSurface();
                bool surfaceEach = m_SnapEach && IsFollowSurface();

                for (int i = 0; i < m_StartTransforms.Length; i++)
                {
                    // The primary entity uses the delta (it's the one on the hit), so does the gizmo.
                    if (surfaceEach && i != m_PrimaryIndex)
                    {
                        m_Results[i] = FollowSurfaceEach(i, delta);
                        continue;
                    }

                    Transform start = m_StartTransforms[i];
                    float3 pivot = m_Pivots[i];

                    float3 posDelta = delta.Position;
                    quaternion rotDelta = delta.Rotation;

                    if (snapEach)
                    {
                        if (m_Mode == Mode.Move)
                        {
                            posDelta = SnapPosition(start.m_Position, start.m_Position + delta.Position, delta.MovedAxes) - start.m_Position;
                        }
                        else if (m_Mode == Mode.Rotate)
                        {
                            rotDelta = quaternion.AxisAngle(m_AxisDir, SnapAngle(delta.Angle, start.m_Rotation));
                        }
                    }

                    m_Results[i] = new Transform(
                        pivot + posDelta + math.rotate(rotDelta, start.m_Position - pivot),
                        math.mul(rotDelta, start.m_Rotation));

                    // Individual: the gizmo is on the primary entity, follow its own snapped delta.
                    if (snapEach && i == m_PrimaryIndex)
                    {
                        gizmoPosDelta = posDelta;
                        gizmoRotDelta = rotDelta;
                    }
                }

                m_Gizmo.Value = new DragGizmoResult
                {
                    m_Position = m_DragStartGizmoPos + gizmoPosDelta,
                    m_Rotation = math.mul(gizmoRotDelta, m_DragStartGizmoRot),
                };
            }

            // Rules in SnapGrid.SnapPosition / SnapGrid.SnapAngle.
            private float3 SnapPosition(float3 origin, float3 pos, bool3 movedAxes)
            {
                return SnapGrid.SnapPosition(origin, pos, m_PosOffset, m_UseLocalAxis, m_DragStartGizmoRot, movedAxes);
            }

            private float SnapAngle(float angle, quaternion refRot)
            {
                return SnapGrid.SnapAngle(angle, m_RotOffset, !m_UseLocalAxis && m_Handle == Handle.Y, refRot);
            }
        }

        // Ray / drag plane: perpendicular to the axis if axisIsNormal, else containing it and facing the camera.
        private static bool RaycastDragPlane(float3 rayOrigin, float3 rayDirection, float3 axisDir, float3 center, bool axisIsNormal, float3 cameraRight, float3 cameraForward, out float3 hitPos)
        {
            hitPos = default;

            float3 planeNormal = axisDir;
            if (!axisIsNormal)
            {
                // Built with the camera right, camera forward if the axis is parallel to it.
                planeNormal = math.cross(axisDir, cameraRight);
                if (math.lengthsq(planeNormal) < 1e-6f)
                    planeNormal = math.cross(axisDir, cameraForward);
            }
            planeNormal = math.normalizesafe(planeNormal);

            // Same as UnityEngine.Plane.Raycast: no hit when parallel or behind the ray.
            float denominator = math.dot(rayDirection, planeNormal);
            if (math.abs(denominator) < 1e-6f) return false;

            float enter = math.dot(center - rayOrigin, planeNormal) / denominator;
            if (enter < 0f) return false;

            hitPos = rayOrigin + rayDirection * enter;
            return true;
        }

        // Axes (world) the handle can move along, used by the world grid so a single axis drag doesn't snap the other ones.
        private static bool3 GetMovedAxes(Handle handle, float3 axisDir)
        {
            float3 absAxis = math.abs(axisDir);
            return handle == Handle.XZ ? absAxis < 0.5f : absAxis > 0.5f;
        }
    }
}
