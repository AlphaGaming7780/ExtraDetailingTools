using Colossal;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;
using RaycastHit = Game.Common.RaycastHit;

namespace ExtraDetailingTools.Gizmos
{
    internal partial class GizmosRaycastSystem
    {
        [BurstCompile]
        private struct RaycastResultJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeAccumulator<RaycastResult> m_Accumulator;

            [NativeDisableParallelForRestriction]
            public NativeList<RaycastResult> m_Result;

            public void Execute(int index)
            {
                m_Result[index] = m_Accumulator.GetResult(index);
            }
        }

#if RELEASE
        [BurstCompile]
#endif
        private struct GizmoRaycastJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityHandle;
            [ReadOnly] public ComponentTypeHandle<GizmosData> GizmoHandle;

            [ReadOnly] public NativeArray<GizmosRaycastInput> Inputs;

            public NativeAccumulator<RaycastResult>.ParallelWriter Accumulator;

            [ReadOnly] public GizmoBatcher Batcher;

            private bool m_Debug;

            public void Execute(
                in ArchetypeChunk chunk,
                int chunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var gizmos = chunk.GetNativeArray(ref GizmoHandle);

                for (int i = 0; i < gizmos.Length; i++)
                {
                    var entity = entities[i];
                    var gizmo = gizmos[i];

                    for (int inputIndex = 0; inputIndex < Inputs.Length; inputIndex++)
                    {
                        var input = Inputs[inputIndex];
                        m_Debug = input.m_Debug;

                        if (!MatchType(gizmo.Type, input.m_Type))
                        {
                            continue;
                        }

                        if (Intersect(gizmo, input.m_Line, input.m_Tolerance, out RaycastHit hit))
                        {
                            hit.m_HitEntity = entity;

                            Accumulator.Accumulate(inputIndex, new RaycastResult
                            {
                                m_Hit = hit,
                                m_Owner = entity
                            });
                        }
                    }
                }
            }

            private readonly bool MatchType(GizmoType type, GizmosRaycastType mask)
            {
                return ((GizmosRaycastType)(1u << (int)type) & mask) != 0;
            }

            private bool Intersect(GizmosData g, Line3.Segment line, float tolerance, out RaycastHit hit)
            {
                switch (g.Type)
                {
                    case GizmoType.Line:
                        return IntersectCapsule(g.A, g.B, tolerance, line, out hit);

                    case GizmoType.Bezier:
                        return IntersectBezier(g, line, tolerance, out hit);

                    case GizmoType.Arrow:
                        return IntersectArrow(g.A, g.B, g.Params0.x, g.Params0.y, line, tolerance, out hit);

                    case GizmoType.ArrowHead:
                        return IntersectArrowHead(g.A, g.B, g.Params0.x, g.Params0.y, line, tolerance, out hit);

                    case GizmoType.Sphere:
                        return IntersectSphere(g.A, g.Params0.x + tolerance, line, out hit);

                    case GizmoType.Cube:
                        return IntersectAABB(g, line, tolerance, out hit);

                    case GizmoType.WireArc:
                        return IntersectWireArc(g, line, tolerance, out hit);

                    case GizmoType.Cylinder:
                        return IntersectCylinder(g, line, tolerance, out hit);

                    case GizmoType.Cone:
                        return IntersectTaperedCylinder(
                            math.transform(g.TRS, g.A), g.Params0.x,
                            math.transform(g.TRS, g.B), g.Params0.y,
                            line, tolerance, roundedCaps: false, out hit);

                    case GizmoType.Capsule:
                        return IntersectCapsuleShape(g, line, tolerance, out hit);

                    case GizmoType.CapsuleConic:
                        return IntersectTaperedCylinder(
                            math.transform(g.TRS, g.A), g.Params0.x,
                            math.transform(g.TRS, g.B), g.Params0.y,
                            line, tolerance, roundedCaps: true, out hit);

                    case GizmoType.Frustum:
                        return IntersectFrustum(g, line, tolerance, out hit);

                    default:
                        hit = default;
                        return false;
                }
            }

            // ── Debug drawing ──
            // Every Intersect* method below draws the exact tolerance-inflated collision volume it tests
            // against, when m_Debug is set (input.m_Debug), so the raycast bounds can be visualized in-game.

            private void DebugDrawSphere(float3 center, float radius)
            {
                if (!m_Debug) return;
                Batcher.DrawWireSphere(center, radius, Color.green);
            }

            private void DebugDrawCapsule(float3 a, float3 b, float radius)
            {
                if (!m_Debug) return;

                float4x4 trs = BuildAxisTRS(a, b, out float axisLength);
                Batcher.DrawWireCapsule(trs, float3.zero, radius, axisLength + radius * 2f, Color.green);
            }

            private void DebugDrawCylinder(float3 a, float3 b, float radius)
            {
                if (!m_Debug) return;

                float4x4 trs = BuildAxisTRS(a, b, out float axisLength);
                Batcher.DrawWireCylinder(trs, float3.zero, radius, axisLength, Color.green);
            }

            // Used for Cone/CapsuleConic debug (radiusA != radiusB); `a`/`b` are already in world space.
            private void DebugDrawTaperedShape(float3 a, float radiusA, float3 b, float radiusB, bool roundedCaps)
            {
                if (!m_Debug) return;

                if (roundedCaps)
                    Batcher.DrawWireCapsuleConic(float4x4.identity, a, radiusA, b, radiusB, Color.green);
                else
                    Batcher.DrawWireCone(float4x4.identity, a, radiusA, b, radiusB, Color.green);
            }

            private void DebugDrawCube(float4x4 trs, float3 center, float3 size)
            {
                if (!m_Debug) return;
                Batcher.DrawWireCube(trs, center, size, Color.green);
            }

            // Builds a TRS placing the local Y axis along (b - a), centered at the midpoint of a/b.
            private static float4x4 BuildAxisTRS(float3 a, float3 b, out float axisLength)
            {
                float3 dir = b - a;
                axisLength = math.length(dir);
                float3 center = (a + b) * 0.5f;

                if (axisLength <= 1e-5f)
                {
                    return float4x4.TRS(center, quaternion.identity, new float3(1, 1, 1));
                }

                float3 up = dir / axisLength;
                float3 arbitrary = math.abs(up.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0);
                float3 forward = math.normalize(math.cross(arbitrary, up));
                float3 right = math.normalize(math.cross(up, forward));

                float4x4 rotM = new float4x4(
                    new float4(right, 0),
                    new float4(up, 0),
                    new float4(forward, 0),
                    new float4(0, 0, 0, 1)
                );

                return float4x4.TRS(center, new quaternion(rotM), new float3(1, 1, 1));
            }

            private bool IntersectSphere(float3 center, float radius, Line3.Segment ray, out RaycastHit hit)
            {
                DebugDrawSphere(center, radius);

                hit = default;

                float3 dir = ray.b - ray.a;
                float rayLength = math.length(dir);

                if (rayLength <= 1e-6f)
                    return false;

                dir /= rayLength;

                if (!MathUtils.Intersect(new Sphere3(radius, center), ray, out float2 t))
                    return false;

                float tRay = t.x;

                float3 pRay = ray.a + dir * tRay;

                float3 normal = pRay - center;
                float normalLen = math.length(normal);

                if (normalLen > 1e-5f)
                    normal /= normalLen;
                else
                    normal = dir;

                hit = new RaycastHit
                {
                    m_Position = center,
                    m_HitPosition = pRay,
                    m_HitDirection = normal,
                    m_NormalizedDistance = tRay / rayLength,
                    m_CurvePosition = 0f
                };

                return true;
            }

            // Transforms the ray into the shape's local space (via g.TRS) before testing against the
            // axis-aligned box, so rotated/scaled Cube gizmos are picked correctly.
            private bool IntersectAABB(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                float4x4 invTrs = math.inverse(g.TRS);

                float3 localRayA = math.transform(invTrs, ray.a);
                float3 localRayB = math.transform(invTrs, ray.b);

                Line3.Segment localRay = new Line3.Segment(localRayA, localRayB);

                float3 size = g.Params0.xyz + tolerance * 2f;

                DebugDrawCube(g.TRS, g.A, size);

                float3 min = g.A - size * 0.5f;
                float3 max = g.A + size * 0.5f;

                Bounds3 bounds = new Bounds3(min, max);

                if (!MathUtils.Intersect(bounds, localRay, out float2 t))
                {
                    hit = default;
                    return false;
                }

                if (t.x < 0f)
                {
                    hit = default;
                    return false;
                }

                float3 localHitPos = MathUtils.Position(localRay, t.x);

                hit = new RaycastHit
                {
                    m_Position = math.transform(g.TRS, g.A),
                    m_HitPosition = math.transform(g.TRS, localHitPos),
                    m_NormalizedDistance = t.x,
                };

                return true;
            }

            private bool IntersectArrow(float3 a, float3 b, float headLength, float headAngleDeg, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                // Compute ray direction and length
                float3 rayDir = ray.b - ray.a;
                float rayLength = math.length(rayDir);

                // Degenerate ray → no hit
                if (rayLength <= 0f)
                {
                    hit = default;
                    return false;
                }

                float angleRad = math.radians(headAngleDeg);
                float coneRadius = math.tan(angleRad) * headLength;
                float radius = coneRadius + tolerance;

                float3 ab = b - a;
                float abLen = math.length(ab);

                if (abLen <= 1e-6f)
                {
                    hit = default;
                    return false;
                }

                float3 abDir = ab / abLen;

                // Inner capsule segment:
                //
                // Without tolerance:
                //   - The capsule (cylinder + hemispheres) must start exactly at A and end exactly at B.
                //   - That means the internal segment of the capsule is [A + coneRadius * dir, B - coneRadius * dir].
                //
                // With tolerance:
                //   - We only grow the radius (coneRadius + tolerance),
                //   - So the shape can extend beyond A and B, but the "logical" arrow is still [A, B].
                float3 innerA = a + abDir * coneRadius;
                float3 innerB = b - abDir * coneRadius;

                // If the arrow is too short to contain the inner segment
                if (math.dot(innerB - innerA, abDir) <= 0f)
                {
                    hit = default;
                    return false;
                }

                // Perform the capsule intersection test on the inner segment
                if (IntersectCapsule(innerA, innerB, radius, ray, out hit))
                {
                    return true;
                }

                hit = default;
                return false;
            }

            // The arrow head is a cone: apex at `pos`, base offset backwards along `-dir` by `headLength`.
            private bool IntersectArrowHead(float3 pos, float3 dir, float headLength, float headAngleDeg, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                float3 dirLen = dir;
                float len = math.length(dirLen);

                if (len <= 1e-6f)
                {
                    hit = default;
                    return false;
                }

                float3 dirN = dirLen / len;

                float angleRad = math.radians(headAngleDeg);
                float baseRadius = math.tan(angleRad) * headLength;
                float3 basePos = pos - dirN * headLength;

                return IntersectTaperedCylinder(basePos, baseRadius, pos, 0f, ray, tolerance, roundedCaps: false, out hit);
            }

            private bool IntersectWireArc(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                hit = default;

                float3 center = g.A;
                float3 normal = g.B;
                float3 from = g.C;
                float angle = g.Params0.x;
                float radius = g.Params0.y;
                int segments = g.Segments;

                float angleRad = math.radians(angle);

                float3 n = math.normalize(normal);
                float3 f = math.normalize(from);

                f = math.normalize(f - n * math.dot(f, n));

                float step = angleRad / segments;

                float3 prev = center + f * radius;

                float bestDist = float.MaxValue;
                bool found = false;

                for (int i = 1; i <= segments; i++)
                {
                    float a = step * i;

                    quaternion rot = quaternion.AxisAngle(n, a);
                    float3 dir = math.rotate(rot, f);

                    float3 curr = center + dir * radius;

                    float capsuleRadius = tolerance;

                    if (IntersectCapsule(prev, curr, capsuleRadius, ray, out RaycastHit h))
                    {
                        if (h.m_NormalizedDistance < bestDist)
                        {
                            bestDist = h.m_NormalizedDistance;
                            hit = h;
                            found = true;
                        }
                    }

                    prev = curr;
                }

                return found;
            }

            // Approximates the curve as a chain of thin capsules between sampled points, same approach as IntersectWireArc.
            private bool IntersectBezier(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                hit = default;

                Bezier4x3 bezier = new Bezier4x3(g.A, g.B, g.C, g.D);

                int segments = math.max(g.Segments, 1);
                float step = 1f / segments;

                float3 prev = g.A;

                float bestDist = float.MaxValue;
                bool found = false;

                for (int i = 1; i <= segments; i++)
                {
                    float3 curr = MathUtils.Position(bezier, i * step);

                    if (IntersectCapsule(prev, curr, tolerance, ray, out RaycastHit h))
                    {
                        if (h.m_NormalizedDistance < bestDist)
                        {
                            bestDist = h.m_NormalizedDistance;
                            hit = h;
                            found = true;
                        }
                    }

                    prev = curr;
                }

                return found;
            }

            // Cylinder is stored as center + radius + height (local Y axis), oriented/positioned by TRS.
            private bool IntersectCylinder(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                float radius = g.Params0.x;
                float height = g.Params0.y;

                float3 localA = g.A + new float3(0f, height * 0.5f, 0f);
                float3 localB = g.A - new float3(0f, height * 0.5f, 0f);

                float3 worldA = math.transform(g.TRS, localA);
                float3 worldB = math.transform(g.TRS, localB);

                return IntersectTaperedCylinder(worldA, radius, worldB, radius, ray, tolerance, roundedCaps: false, out hit);
            }

            // Capsule is stored as center + radius + total height (local Y axis), oriented/positioned by TRS.
            private bool IntersectCapsuleShape(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                float radius = g.Params0.x;
                float height = g.Params0.y;
                float half = math.max(height * 0.5f - radius, 0f);

                float3 localA = g.A + new float3(0f, half, 0f);
                float3 localB = g.A - new float3(0f, half, 0f);

                float3 worldA = math.transform(g.TRS, localA);
                float3 worldB = math.transform(g.TRS, localB);

                return IntersectTaperedCylinder(worldA, radius, worldB, radius, ray, tolerance, roundedCaps: true, out hit);
            }

            // Approximate: uses the frustum's local bounding box rather than the exact 4 pyramid side faces.
            private bool IntersectFrustum(GizmosData g, Line3.Segment ray, float tolerance, out RaycastHit hit)
            {
                float fov = g.Params0.x;
                float minRange = g.Params0.y;
                float maxRange = g.Params0.z;
                float aspect = g.Params0.w;

                float tanHalf = math.tan(math.radians(fov * 0.5f));

                float halfHeightNear = tanHalf * minRange;
                float halfWidthNear = halfHeightNear * aspect;
                float halfHeightFar = tanHalf * maxRange;
                float halfWidthFar = halfHeightFar * aspect;

                float halfWidth = math.max(halfWidthNear, halfWidthFar) + tolerance;
                float halfHeight = math.max(halfHeightNear, halfHeightFar) + tolerance;

                float3 min = new float3(-halfWidth, -halfHeight, minRange - tolerance);
                float3 max = new float3(halfWidth, halfHeight, maxRange + tolerance);

                DebugDrawCube(g.TRS, (min + max) * 0.5f, max - min);

                Bounds3 bounds = new Bounds3(min, max);

                float4x4 invTrs = math.inverse(g.TRS);
                float3 localRayA = math.transform(invTrs, ray.a);
                float3 localRayB = math.transform(invTrs, ray.b);

                Line3.Segment localRay = new Line3.Segment(localRayA, localRayB);

                if (!MathUtils.Intersect(bounds, localRay, out float2 t) || t.x < 0f)
                {
                    hit = default;
                    return false;
                }

                float3 localHitPos = MathUtils.Position(localRay, t.x);

                hit = new RaycastHit
                {
                    m_Position = math.transform(g.TRS, float3.zero),
                    m_HitPosition = math.transform(g.TRS, localHitPos),
                    m_NormalizedDistance = t.x,
                };

                return true;
            }

            // Unified lateral-surface + caps intersection, shared by Cylinder, Cone, Capsule and CapsuleConic
            // (all are a straight or tapered tube between `a` and `b`, only the caps differ):
            //   - roundedCaps = true  -> hemispherical caps of radiusA/radiusB (Capsule, CapsuleConic).
            //   - roundedCaps = false -> flat disk caps of radiusA/radiusB (Cylinder, Cone, ArrowHead).
            // When radiusA == radiusB this reduces to a plain cylinder/capsule test.
            private bool IntersectTaperedCylinder(float3 a, float radiusA, float3 b, float radiusB, Line3.Segment ray, float tolerance, bool roundedCaps, out RaycastHit hit)
            {
                hit = default;

                float3 axis = b - a;
                float axisLen = math.length(axis);

                if (axisLen <= 1e-6f)
                {
                    return IntersectSphere(a, math.max(radiusA, radiusB) + tolerance, ray, out hit);
                }

                float rA = radiusA + tolerance;
                float rB = radiusB + tolerance;

                if (rA == rB)
                {
                    if (roundedCaps)
                        DebugDrawCapsule(a, b, rA);
                    else
                        DebugDrawCylinder(a, b, rA);
                }
                else
                {
                    DebugDrawTaperedShape(a, rA, b, rB, roundedCaps);
                }

                float3 d = axis / axisLen;

                float3 rayDir = ray.b - ray.a;
                float rayLength = math.length(rayDir);

                if (rayLength <= 1e-6f)
                    return false;

                rayDir /= rayLength;

                float slope = (rB - rA) / axisLen;

                float3 Q = ray.a - a;
                float s0 = math.dot(Q, d);
                float sd = math.dot(rayDir, d);

                float3 U = Q - s0 * d;
                float3 V = rayDir - sd * d;

                float r0 = rA + slope * s0;
                float rSlope = slope * sd;

                float A2 = math.dot(V, V) - rSlope * rSlope;
                float B2 = 2f * (math.dot(U, V) - r0 * rSlope);
                float C2 = math.dot(U, U) - r0 * r0;

                float bestT = float.MaxValue;
                float3 bestPos = default;
                float3 bestNormal = default;
                float bestS = 0f;
                bool found = false;

                if (math.abs(A2) > 1e-10f)
                {
                    float discriminant = B2 * B2 - 4f * A2 * C2;

                    if (discriminant >= 0f)
                    {
                        float sqrtDisc = math.sqrt(discriminant);
                        float inv2A = 1f / (2f * A2);

                        for (int i = 0; i < 2; i++)
                        {
                            float tHit = (i == 0) ? (-B2 - sqrtDisc) * inv2A : (-B2 + sqrtDisc) * inv2A;

                            if (tHit < 0f || tHit > rayLength)
                                continue;

                            float s = s0 + tHit * sd;

                            if (s < 0f || s > axisLen)
                                continue;

                            float3 p = ray.a + rayDir * tHit;
                            float3 axisPoint = a + d * s;
                            float3 n = math.normalize(p - axisPoint);

                            if (tHit < bestT)
                            {
                                bestT = tHit;
                                bestPos = p;
                                bestNormal = n;
                                bestS = s / axisLen;
                                found = true;
                            }
                        }
                    }
                }

                if (roundedCaps)
                {
                    if (IntersectSphere(a, rA, ray, out RaycastHit hitA))
                    {
                        float tA = hitA.m_NormalizedDistance * rayLength;
                        float sA = math.dot(hitA.m_HitPosition - a, d);

                        if (sA <= 0f && tA < bestT)
                        {
                            bestT = tA;
                            bestPos = hitA.m_HitPosition;
                            bestNormal = hitA.m_HitDirection;
                            bestS = 0f;
                            found = true;
                        }
                    }

                    if (IntersectSphere(b, rB, ray, out RaycastHit hitB))
                    {
                        float tB = hitB.m_NormalizedDistance * rayLength;
                        float sB = math.dot(hitB.m_HitPosition - b, d);

                        if (sB >= 0f && tB < bestT)
                        {
                            bestT = tB;
                            bestPos = hitB.m_HitPosition;
                            bestNormal = hitB.m_HitDirection;
                            bestS = 1f;
                            found = true;
                        }
                    }
                }
                else
                {
                    if (TryIntersectDisk(a, -d, rA, ray.a, rayDir, rayLength, out float3 posA, out float tDiskA) && tDiskA < bestT)
                    {
                        bestT = tDiskA;
                        bestPos = posA;
                        bestNormal = -d;
                        bestS = 0f;
                        found = true;
                    }

                    if (TryIntersectDisk(b, d, rB, ray.a, rayDir, rayLength, out float3 posB, out float tDiskB) && tDiskB < bestT)
                    {
                        bestT = tDiskB;
                        bestPos = posB;
                        bestNormal = d;
                        bestS = 1f;
                        found = true;
                    }
                }

                if (!found)
                {
                    hit = default;
                    return false;
                }

                hit = new RaycastHit
                {
                    m_Position = a,
                    m_HitPosition = bestPos,
                    m_HitDirection = bestNormal,
                    m_NormalizedDistance = bestT / rayLength,
                    m_CurvePosition = bestS
                };

                return true;
            }

            private static bool TryIntersectDisk(float3 center, float3 normal, float radius, float3 rayOrigin, float3 rayDirNorm, float rayLength, out float3 hitPos, out float t)
            {
                hitPos = default;
                t = 0f;

                float denom = math.dot(rayDirNorm, normal);

                if (math.abs(denom) < 1e-8f)
                    return false;

                t = math.dot(center - rayOrigin, normal) / denom;

                if (t < 0f || t > rayLength)
                    return false;

                float3 p = rayOrigin + rayDirNorm * t;

                if (math.lengthsq(p - center) > radius * radius)
                    return false;

                hitPos = p;
                return true;
            }

            private bool IntersectCapsule(float3 a, float3 b, float radius, Line3.Segment ray, out RaycastHit hit)
            {
                DebugDrawCapsule(a, b, radius);

                float3 rayDir = ray.b - ray.a;
                float rayLength = math.length(rayDir);

                if (rayLength <= 0f)
                {
                    hit = default;
                    return false;
                }

                rayDir /= rayLength;

                float3 ab = b - a;
                float abLen = math.length(ab);

                if (abLen <= 1e-6f)
                    return IntersectSphere(a, radius, ray, out hit);

                float3 abDir = ab / abLen;

                // Ray-infinite-cylinder intersection
                // Project ray onto the plane perpendicular to the capsule axis
                float3 dp = ray.a - a;
                float3 rayDirPerp = rayDir - abDir * math.dot(rayDir, abDir);
                float3 dpPerp = dp - abDir * math.dot(dp, abDir);

                float A2 = math.dot(rayDirPerp, rayDirPerp);
                float B2 = 2f * math.dot(rayDirPerp, dpPerp);
                float C2 = math.dot(dpPerp, dpPerp) - radius * radius;

                float bestT = float.MaxValue;
                float3 bestPos = default;
                float3 bestNormal = default;
                float bestTSeg = 0f;
                bool found = false;

                float discriminant = B2 * B2 - 4f * A2 * C2;

                if (A2 > 1e-10f && discriminant >= 0f)
                {
                    float sqrtDisc = math.sqrt(discriminant);
                    float inv2A = 1f / (2f * A2);

                    for (int i = 0; i < 2; i++)
                    {
                        float tHit = (i == 0) ? (-B2 - sqrtDisc) * inv2A : (-B2 + sqrtDisc) * inv2A;

                        if (tHit < 0f || tHit > rayLength) continue;

                        float3 p = ray.a + rayDir * tHit;
                        float s = math.dot(p - a, abDir);

                        if (s >= 0f && s <= abLen)
                        {
                            float3 axisPoint = a + abDir * s;
                            float3 n = math.normalize(p - axisPoint);

                            if (tHit < bestT)
                            {
                                bestT = tHit;
                                bestPos = p;
                                bestNormal = n;
                                bestTSeg = s / abLen;
                                found = true;
                            }
                            break;
                        }
                    }
                }

                // Hemisphere at endpoint A
                if (IntersectSphere(a, radius, ray, out RaycastHit hitA))
                {
                    float3 pA = hitA.m_HitPosition;
                    float sA = math.dot(pA - a, abDir);
                    float tA = hitA.m_NormalizedDistance * rayLength;
                    if (sA <= 0f && tA < bestT)
                    {
                        bestT = tA;
                        bestPos = pA;
                        bestNormal = hitA.m_HitDirection;
                        bestTSeg = 0f;
                        found = true;
                    }
                }

                // Hemisphere at endpoint B
                if (IntersectSphere(b, radius, ray, out RaycastHit hitB))
                {
                    float3 pB = hitB.m_HitPosition;
                    float sB = math.dot(pB - b, abDir);
                    float tB = hitB.m_NormalizedDistance * rayLength;
                    if (sB >= 0f && tB < bestT)
                    {
                        bestT = tB;
                        bestPos = pB;
                        bestNormal = hitB.m_HitDirection;
                        bestTSeg = 1f;
                        found = true;
                    }
                }

                if (!found)
                {
                    hit = default;
                    return false;
                }

                hit = new RaycastHit
                {
                    m_Position = a,
                    m_HitPosition = bestPos,
                    m_HitDirection = bestNormal,
                    m_NormalizedDistance = bestT / rayLength,
                    m_CurvePosition = bestTSeg
                };

                return true;
            }

        }
    }
}
