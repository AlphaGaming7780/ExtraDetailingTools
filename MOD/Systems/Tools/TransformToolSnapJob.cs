using Colossal.Collections;
using ExtraDetailingTools.Snapping;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
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
        // Not wired into Update() yet. Uses the vanilla ControlPoint/ToolUtils.AddSnapPosition machinery
        // (same as ObjectTool's own snap checks) instead of a custom candidate type, specifically so the
        // existing Object Side snap functions (CheckSnapLineGeneric, FindNearbySideTarget, ...) can
        // eventually be called from here unmodified - they already take/return ControlPoint. It's also a
        // natural fit for the FollowSurface handle, whose raw input already IS a ControlPoint-shaped
        // (Entity, RaycastHit) pair from GetRaycastResult.
        //
        // m_ControlPoint is the raw candidate BEFORE any snapping (for the axis/plane-drag case, built by hand
        // with m_OriginalEntity = Entity.Null and m_SnapPriority = default, same idiom ObjectTool's own snap
        // functions use for their starting/baseline point; for FollowSurface, built from the raycast hit with
        // the drag start rotation). Execute() writes the snapped result in m_Result.
        //
        // Only scheduled for the FollowSurface handle for now (TransformGizmoTool.SnapFollowSurface).
#if RELEASE
        [BurstCompile]
#endif
        private struct TransformToolSnapJob : IJob
        {
            [ReadOnly] public Mode m_Mode;
            [ReadOnly] public Entity m_SelectedEntity;

            [ReadOnly] public Handle m_Handle;

            [ReadOnly] public ControlPoint m_ControlPoint;
            public NativeReference<ControlPoint> m_Result;

            [ReadOnly] public float3 m_DragStartGizmoPos;
            [ReadOnly] public quaternion m_DragStartGizmoRot;
            [ReadOnly] public float3 m_AxisDir;
            [ReadOnly] public bool m_UseLocalAxis;

            // Sphere Mode
            [ReadOnly] public XZHandleMode m_XZHandleMode;
            // FollowSurface: tilt the object to the surface (or keep it upright). Off for a group.
            [ReadOnly] public bool m_AlignToSurface;
            [ReadOnly] public bool m_Upright;
            [ReadOnly] public bool m_EditorMode;

            // Grid Snap
            [ReadOnly] public bool m_GridEnabled;
            [ReadOnly] public double m_PosOffset;
            [ReadOnly] public double m_RotOffset;

            // Object Side Snap - test wiring for SnapObjectSide (MOD/Snapping/SnapObjectSide.cs), Move mode
            // only (no "side" concept for a rotation delta). Competes against the grid through the same
            // best/ToolUtils.AddSnapPosition accumulator.
            [ReadOnly] public bool m_ObjectSideSnapEnabled;
            [ReadOnly] public ObjectSideSnapMode m_ObjectSideSnapMode;

            public SnapContext m_Context;


            public void Execute()
            {
                // Baseline candidate: no snap at all. m_RawControlPoint.m_SnapPriority is expected to be
                // default (0,0), so it only survives if nothing else beats it - same convention ObjectTool's
                // snap functions use (bestSnapPosition starts as the raw/unsnapped ControlPoint). Future
                // snap sources (object-side, etc.) just need to build their own ControlPoint and call
                // ToolUtils.AddSnapPosition - the grid below is only the first competitor, not a special case.
                ControlPoint best = m_ControlPoint;

                if (m_Handle == Handle.XZ)
                {
                    if(m_XZHandleMode == XZHandleMode.FollowSurface)
                    {
                        // Same baseline as the vanilla SnapJob: the hit entity is only kept if the surface snap uses it
                        // (AlignToSurface reads the terrain from the raw point).
                        best.m_OriginalEntity = Entity.Null;
                        SnapObjectSurface.SnapSurface(ref m_Context, m_ControlPoint, ref best);

                        if (m_AlignToSurface && m_Context.m_PrefabRefData.TryGetComponent(m_SelectedEntity, out PrefabRef prefabRef))
                        {
                            // No 45 degrees alignment on the parent: the gizmo keeps the rotation of the object.
                            quaternion parentRotation = quaternion.identity;
                            SnapObjectSurface.AlignToSurface(ref m_Context, prefabRef.m_Prefab, true, m_Upright, m_EditorMode, m_ControlPoint, ref best, ref parentRotation, false);
                        }
                    }
                }
                else
                {
                    if (m_GridEnabled)
                    {
                        if (m_Mode == Mode.Move)
                        {
                            quaternion gridRot = quaternion.identity;
                            if (m_UseLocalAxis && m_Context.m_TransformData.TryGetComponent(m_SelectedEntity, out Transform transform))
                            {
                                gridRot = transform.m_Rotation;
                            }
                            float3 snappedPos = SnapGrid.SnapPositionLocal(m_DragStartGizmoPos, m_ControlPoint.m_Position, gridRot, (float)m_PosOffset);
                            ControlPoint gridCandidate = m_ControlPoint;
                            gridCandidate.m_Position = snappedPos;
                            gridCandidate.m_HitPosition = snappedPos;
                            // Position-offset-based scoring (same formula ObjectTool's edge/corner snaps use)
                            // so a future closer object-snap candidate can naturally outrank the grid.
                            gridCandidate.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, snappedPos - m_ControlPoint.m_Position);
                            ToolUtils.AddSnapPosition(ref best, gridCandidate);
                        }
                        else if (m_Mode == Mode.Rotate)
                        {
                            // m_AxisDir is known, so the delta is guaranteed to be a pure rotation around it -
                            // recovering the signed angle from (currentRot * inverse(startRot)) via atan2 is
                            // exact here, not a generic/fragile quaternion decomposition.
                            quaternion deltaRot = math.mul(m_ControlPoint.m_Rotation, math.inverse(m_DragStartGizmoRot));
                            float rawAngle = 2f * math.atan2(math.dot(deltaRot.value.xyz, m_AxisDir), deltaRot.value.w);

                            float snappedAngle = SnapGrid.SnapAngleIncremental(rawAngle, (float)m_RotOffset);
                            ControlPoint gridCandidate = m_ControlPoint;
                            gridCandidate.m_Rotation = math.mul(quaternion.AxisAngle(m_AxisDir, snappedAngle), m_DragStartGizmoRot);
                            // No position offset to score a rotation snap against - wins outright over the
                            // unsnapped baseline (priority (0,0)) whenever the grid is enabled.
                            gridCandidate.m_SnapPriority = new float2(1f, 0f);
                            ToolUtils.AddSnapPosition(ref best, gridCandidate);
                        }
                    }
                }



                // Object Side Snap - integration test for SnapObjectSide. m_ControlPoint is the raw
                // candidate (never overwritten), so it's a valid "true raw reference"
                // for Apply's own internal edge scoring, same requirement as the grid competing above.
                if (m_ObjectSideSnapEnabled && m_Mode == Mode.Move && m_Context.m_PrefabRefData.HasComponent(m_SelectedEntity))
                {
                    Entity selectedPrefab = m_Context.m_PrefabRefData[m_SelectedEntity].m_Prefab;
                    if (m_Context.m_ObjectGeometryData.HasComponent(selectedPrefab))
                    {
                        ObjectGeometryData placedGeometryData = m_Context.m_ObjectGeometryData[selectedPrefab];
                        SnapObjectSide.Apply(ref m_Context, m_ObjectSideSnapMode, m_ControlPoint, ref best, m_ControlPoint.m_Rotation, placedGeometryData);
                    }
                }

                m_Result.Value = best;
            }
        }
    }
}
