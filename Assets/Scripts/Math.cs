﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct Quad
{
    public float3 Center;
    public float3 Normal;
    public float3 LocalRight;
    public float3 LocalUp;
}

public struct OccluderPlanes
{
    public Plane Left;
    public Plane Right;
    public Plane Up;
    public Plane Down;
    public Plane Near;
}
public struct WorldFrustrumPlanes
{
    public Plane Left;
    public Plane Right;
    public Plane Down;
    public Plane Up;
    public Plane Near;
    public Plane Far;
}


public static class Math
{
    public const float Sqrt3 = 1.73205080f;

    public static float SignedDistanceToPlane(float3 point, Plane plane)
    {
        float3 normal = plane.normal;
        float distance = plane.distance;
        float3 planePoint = -normal * distance;
        var delta = point - planePoint;

        return math.dot(normal, delta);
    }

    public static bool IsClipped(float3 center, float radius, Plane plane)
    {
        return SignedDistanceToPlane(center, plane) < -radius;
    }

    public static bool IsClipped(float3 center, Plane plane)
    {
        return SignedDistanceToPlane(center, plane) < 0f;
    }

    public static bool IsInFrustrum(float3 center, float radius, in WorldFrustrumPlanes planes)
    {
        return !IsClipped(center, radius, planes.Left)
            && !IsClipped(center, radius, planes.Right)
            && !IsClipped(center, radius, planes.Down)
            && !IsClipped(center, radius, planes.Up)
            && !IsClipped(center, radius, planes.Near)
            && !IsClipped(center, radius, planes.Far);
    }

    public static bool IsInFrustrum(float3 point, in WorldFrustrumPlanes planes)
    {
        return !IsClipped(point, planes.Left)
            && !IsClipped(point, planes.Right)
            && !IsClipped(point, planes.Down)
            && !IsClipped(point, planes.Up)
            && !IsClipped(point, planes.Near)
            && !IsClipped(point, planes.Far);
    }

    // Really need to figure out of to make fixed size stack array in C# ...
    public static bool IsClipped(float3 a, float3 b, float3 c, float3 d, float3 e, float3 f, float3 g, float3 h, Plane plane)
    {
        return IsClipped(a, plane)
            && IsClipped(b, plane)
            && IsClipped(c, plane)
            && IsClipped(d, plane)
            && IsClipped(e, plane)
            && IsClipped(f, plane)
            && IsClipped(g, plane)
            && IsClipped(h, plane);
    }

    public static bool IsCubeInFrustrum(float3 center, float extent, in WorldFrustrumPlanes planes)
    {
        if (!IsInFrustrum(center, extent * Sqrt3, planes)) return false;

        if (IsInFrustrum(center, extent, planes)) return true;

        var x = new float3(extent, 0, 0);
        var y = new float3(0, extent, 0);
        var z = new float3(0, 0, extent);

        var a = center + x + y + z;
        var b = center + x + y - z;
        var c = center + x - y + z;
        var d = center + x - y - z;
        var e = center - x + y + z;
        var f = center - x + y - z;
        var g = center - x - y + z;
        var h = center - x - y - z;

        return !IsClipped(a, b, c, d, e, f, g, h, planes.Left)
            && !IsClipped(a, b, c, d, e, f, g, h, planes.Right)
            && !IsClipped(a, b, c, d, e, f, g, h, planes.Down)
            && !IsClipped(a, b, c, d, e, f, g, h, planes.Up)
            && !IsClipped(a, b, c, d, e, f, g, h, planes.Near)
            && !IsClipped(a, b, c, d, e, f, g, h, planes.Far);
    }

    public static bool IsSphereOccluderInFrustrum(float3 center, float radius, in WorldFrustrumPlanes planes, out bool hasNearIntersection)
    {
        // Special handling of the near clipping plane for occluders (planes[4])
        // We want the occluder to be discarded if its center is behind the near plane
        // Otherwise the objects made visible by the clipping of the near plane get culled out

        hasNearIntersection = false;

        if
        (
            IsClipped(center, radius, planes.Left)
            || IsClipped(center, radius, planes.Right)
            || IsClipped(center, radius, planes.Down)
            || IsClipped(center, radius, planes.Up)
            || IsClipped(center, radius, planes.Far)
        )
        {
            return false;
        }

        var nearDist = SignedDistanceToPlane(center, planes.Near);
        if (nearDist < 0f)
        {
            return false;
        }

        if (math.abs(nearDist) < radius)
        {
            hasNearIntersection = true;
        }

        return true;
    }

    public static bool IsSphereOccluded(float3 viewerToObject, float objectRadius, float3 viewerToOccluder, float3 occluderDirection,
        float occluderDistance, float occluderRadius, bool cullInside)
    {
        // Handling of the objects in occluder sphere handling
        var occluderToObject = viewerToObject - viewerToOccluder;

        // If it is requested to cull the inside of the sphere, we need to check if the bounding sphere of the object is completely submerged by the occluder.
        // But if we don't want to cull the inside, the bounding sphere is still visible when it is completely in the occluder AND when it intersects with it.
        var maxDistToOccluderSq = occluderRadius + (cullInside ? -objectRadius : objectRadius);
        maxDistToOccluderSq *= maxDistToOccluderSq;

        var isInMaxDistToOccluder = math.lengthsq(occluderToObject) < maxDistToOccluderSq;
        if (isInMaxDistToOccluder)
        {
            return cullInside;
        }

        // Handling of the objects behind the near slice of the occlusion cone
        var objectProjectedDistance = math.dot(occluderDirection, viewerToObject);
        var objectProjectedNear = objectProjectedDistance - objectRadius;

        var isBehindNearSlice = objectProjectedNear < occluderDistance;
        if (isBehindNearSlice) return false;

        // Occlusion cone culling
        var objectProjection = occluderDirection * objectProjectedDistance;
        var ratio = objectProjectedDistance / occluderDistance;

        var maxDist = ratio * occluderRadius - objectRadius;
        var maxDistSq = maxDist * maxDist;

        var projectionToObject = viewerToObject - objectProjection;

        // If the boudning sphere fits in the occlusion cone, cull it out
        return math.lengthsq(projectionToObject) < maxDistSq;
    }

    public static bool IsOccludedBySphere(float3 testedCenter, float testedRadius, float3 viewer,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderRadius> occluderRadiuses, in WorldFrustrumPlanes frustrumPlanes)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var occluderCenter = occluderTranslations[i].Value;
            var occluderRadius = occluderRadiuses[i].Value;

            bool hasNearIntersection;
            if (!IsSphereOccluderInFrustrum(occluderCenter, occluderRadius, frustrumPlanes, out hasNearIntersection)) continue;

            var viewerToTested = testedCenter - viewer;
            var viewerToOccluder = occluderCenter - viewer;
            var occluderDistance = math.length(viewerToOccluder);
            var occluderDirection = viewerToOccluder / occluderDistance;

            if (IsSphereOccluded(viewerToTested, testedRadius, viewerToOccluder, occluderDirection, occluderDistance, occluderRadius, !hasNearIntersection))
            {
                return true;
            }
        }

        return false;
    }

    public static float3 GetOccluderlaneNormal(float3 localRight, float3 localUp)
    {
        return math.cross(localUp, localRight);
    }

    public static bool OccluderPlaneHasContribution(in Quad occluder, in Quad nearPlane)
    {
        var occluderToNearPlane = nearPlane.Center - occluder.Center;
        var signedDist = math.dot(occluderToNearPlane, occluder.Normal);

        if (signedDist < 0f) return false;

        var distSq = signedDist * signedDist;

        // Add small epsilon to avoid having to deal with too tiny float values
        var nearBoundingRadiusSq = math.lengthsq(nearPlane.LocalRight + nearPlane.LocalUp) + 0.1f;

        return distSq > nearBoundingRadiusSq;
    }

    public static OccluderPlanes GetOccluderPlanes(float3 viewer, float3 center, float3 occluderNormal, float3 localRight, float localRightLength, float3 localUp, float localUpLength)
    {
        var right = center + localRight * localRightLength;
        var left = center - localRight * localRightLength;
        var up = center + localUp * localUpLength;
        var down = center - localUp * localUpLength;

        var viewerToLeft = math.normalize(left - viewer);
        var viewerToRight = math.normalize(right - viewer);
        var viewerToUp = math.normalize(up - viewer);
        var viewerToDown = math.normalize(down - viewer);

        var leftPlaneNormal = math.cross(viewerToLeft, localUp);
        var rightPlaneNormal = math.cross(localUp, viewerToRight);
        var downPlaneNormal = math.cross(localRight, viewerToDown);
        var upPlaneNormal = math.cross(viewerToUp, localRight);
        var nearPlaneNormal = occluderNormal;

        var planes = new OccluderPlanes();
        planes.Left = new Plane(leftPlaneNormal, left);
        planes.Right = new Plane(rightPlaneNormal, right);
        planes.Up = new Plane(upPlaneNormal, up);
        planes.Down = new Plane(downPlaneNormal, down);
        planes.Near = new Plane(nearPlaneNormal, center);

        return planes;
    }

    public static bool IsOccludedByPlane(float3 testedCenter, float testedRadius, in OccluderPlanes planes)
    {
        return IsClipped(testedCenter, testedRadius, planes.Left)
            && IsClipped(testedCenter, testedRadius, planes.Right)
            && IsClipped(testedCenter, testedRadius, planes.Up)
            && IsClipped(testedCenter, testedRadius, planes.Down)
            && IsClipped(testedCenter, testedRadius, planes.Near);
    }

    public static bool IsOccludedByPlane(float3 testedCenter, float testedRadius, float3 viewer, in Quad nearPlane,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderExtents> occluderExtents)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var center = occluderTranslations[i].Value;
            var localRight = occluderExtents[i].LocalRight;
            var localRightLength = occluderExtents[i].LocalRightLength;
            var localUp = occluderExtents[i].LocalUp;
            var localUpLength = occluderExtents[i].LocalUpLength;
            var occluderNormal = GetOccluderlaneNormal(localRight, localUp);

            var occluderQuad = new Quad();
            occluderQuad.Center = center;
            occluderQuad.LocalRight = localRight * localRightLength;
            occluderQuad.LocalUp = localUp * localUpLength;
            occluderQuad.Normal = occluderNormal;

            if (!OccluderPlaneHasContribution(occluderQuad, nearPlane)) continue;

            var occlusionPlanes = GetOccluderPlanes(viewer, center, occluderNormal, localRight, localRightLength, localUp, localUpLength);

            if (IsOccludedByPlane(testedCenter, testedRadius, occlusionPlanes))
            {
                return true;
            }
        }

        return false;
    }

    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }
}