using System.Collections;
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

    public static bool Overlap(in this AABB b0, in AABB b1)
{
    if (b0.Min.x > b1.Max.x || b0.Min.y > b1.Max.y || b0.Min.z > b1.Max.z)
    {
        return false;
    }

    return b0.Max.x >= b1.Min.x && b0.Max.y >= b1.Min.y && b0.Max.z >= b1.Min.z;
}

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

    public static float3 FarestAABBCornerFromPoint(in AABB aabb, float3 point)
    {
        return aabb.Center + math.sign(aabb.Center - point) * aabb.Extents;
    }

    public static float3 FarestAABBCornerInDirection(in AABB aabb, float3 direction)
    {
        return aabb.Center + math.sign(direction) * aabb.Extents;
    }

    public static float3 NearestAABBCornerFromPoint(in AABB aabb, float3 point)
    {
        return aabb.Center - math.sign(aabb.Center - point) * aabb.Extents;
    }

    public static float3 NearestAABBCornerInDirection(in AABB aabb, float3 direction)
    {
        return aabb.Center - math.sign(direction) * aabb.Extents;
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

    public static bool IsCubeClipped(float3 center, float extent, Plane plane, out bool intersects)
    {
        var extentVector = new float3(extent);
        var localFarest = math.sign(plane.normal) * extentVector;

        var farest = center + localFarest;

        if (IsClipped(farest, plane))
        {
            intersects = false;
            return true;
        }
        else
        {
            var closest = center - localFarest;
            intersects = IsClipped(closest, plane);

            return false;
        }
    }

    public static bool IsCubeCulled(float3 center, float extent, in WorldFrustrumPlanes planes, out bool intersect)
    {
        var intersects0 = new bool4(false);
        var intersects1 = new bool2(false);

        var isClipped = IsCubeClipped(center, extent, planes.Left, out intersects0.x)
            || IsCubeClipped(center, extent, planes.Right, out intersects0.y)
            || IsCubeClipped(center, extent, planes.Down, out intersects0.z)
            || IsCubeClipped(center, extent, planes.Up, out intersects0.w)
            || IsCubeClipped(center, extent, planes.Near, out intersects1.x)
            || IsCubeClipped(center, extent, planes.Far, out intersects1.y);

        if (isClipped)
        {
            intersect = false;
            return true;
        }
        else
        {
            intersect = math.any(intersects0) || math.any(intersects1);
            return false;
        }
    }

    public static bool IsCubeClipped(float3 center, float extent, Plane plane)
    {
        var extentVector = new float3(extent);
        var localFarest = math.sign(plane.normal) * extentVector;

        var farest = center + localFarest;

        return IsClipped(farest, plane);
    }

    public static bool IsClipped(in AABB aabb, Plane plane)
    {
        var localFarest = math.sign(plane.normal) * aabb.Extents;

        var farest = aabb.Center + localFarest;

        return IsClipped(farest, plane);
    }

    public static bool IsCubeCulled(float3 center, float extent, in WorldFrustrumPlanes planes)
    {
        return IsCubeClipped(center, extent, planes.Left)
            || IsCubeClipped(center, extent, planes.Right)
            || IsCubeClipped(center, extent, planes.Down)
            || IsCubeClipped(center, extent, planes.Up)
            || IsCubeClipped(center, extent, planes.Near)
            || IsCubeClipped(center, extent, planes.Far);
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

    public static bool IsOccludedBySphere(float3 viewerToObject, float objectRadius, float3 viewerToOccluder, float3 occluderDirection,
        float occluderDistance, float occluderRadius, bool cullInside)
    {
        // Handling of the objects in occluder sphere. Skip it if the object can't fit in the occluder
        if (objectRadius < occluderRadius)
        {
            var occluderToObject = viewerToObject - viewerToOccluder;

            // If it is requested to cull the inside of the sphere, we need to check if the bounding sphere of the object is completely submerged by the occluder.
            // But if we don't want to cull the inside, the object must be visible when the bounding sphere is completely in the occluder OR when it intersects with it.
            var maxDistToOccluderSq = occluderRadius + (cullInside ? -objectRadius : objectRadius);
            maxDistToOccluderSq *= maxDistToOccluderSq;

            var isInMaxDistToOccluder = math.lengthsq(occluderToObject) < maxDistToOccluderSq;
            if (isInMaxDistToOccluder)
            {
                return cullInside;
            }
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

        // At this distance, the object is bigger than cone radius
        if (maxDist < 0f) return false;

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

            if (IsOccludedBySphere(viewerToTested, testedRadius, viewerToOccluder, occluderDirection, occluderDistance, occluderRadius, !hasNearIntersection))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsOccludedBySphere(in AABB aabb, float3 occluderPosition, float3 occluderDirection,
        float occluderDistance, float occluderRadius, bool cullInside)
    {
        // Handling of the objects in occluder sphere
        var farestFromOccluder = FarestAABBCornerFromPoint(aabb, occluderPosition);
        var nearestFromOccluder = NearestAABBCornerFromPoint(aabb, occluderPosition);

        // If it is requested to cull the inside of the sphere, we need to check if the fares corner of the AABB is in the occluder's sphere.
        // But if we don't want to cull the inside, the object is still visible when its AABB is completely in the occluder OR when it intersects with it.
        // So we compare with the nearest in this case
        var maxDistToOccluderSq = occluderRadius * occluderRadius;
        var cornerForCullingInside = cullInside ? farestFromOccluder : nearestFromOccluder;

        if (math.lengthsq(cornerForCullingInside - occluderPosition) < maxDistToOccluderSq)
        {
            return cullInside;
        }

        // Handling of the objects behind the near slice of the occlusion cone
        var objectProjectedNear = math.dot(occluderDirection, NearestAABBCornerInDirection(aabb, occluderDirection));

        var isBehindNearSlice = objectProjectedNear < occluderDistance;
        if (isBehindNearSlice) return false;

        // Occlusion cone culling
        var objectProjectedDistance = math.dot(occluderDirection, aabb.Center);
        var objectProjection = occluderDirection * objectProjectedDistance;

        var farestFromProjection = FarestAABBCornerFromPoint(aabb, objectProjection);

        var farestProjectedDistance = math.dot(occluderDirection, farestFromProjection);
        var farestProjection = occluderDirection * farestProjectedDistance;

        var thalesRatio = farestProjectedDistance / occluderDistance;
        var maxDist = thalesRatio * occluderRadius;
        var maxDistSq = maxDist * maxDist;

        var projectionToFarest = farestFromProjection - farestProjection;

        // If the farest corner is in the occlusion cone, cull it out
        return math.lengthsq(projectionToFarest) < maxDistSq;
    }

    public static bool IsOccludedBySphere(in AABB aabb, float3 viewer,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderRadius> occluderRadiuses, in WorldFrustrumPlanes frustrumPlanes)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var occluderCenter = occluderTranslations[i].Value;
            var occluderRadius = occluderRadiuses[i].Value;

            bool hasNearIntersection;
            if (!IsSphereOccluderInFrustrum(occluderCenter, occluderRadius, frustrumPlanes, out hasNearIntersection)) continue;

            var viewerToOccluder = occluderCenter - viewer;
            var occluderDistance = math.length(viewerToOccluder);
            var occluderDirection = viewerToOccluder / occluderDistance;

            var viewerAABB = new AABB();
            viewerAABB.Center = aabb.Center - viewer;
            viewerAABB.Extents = aabb.Extents;

            if (IsOccludedBySphere(viewerAABB, viewerToOccluder, occluderDirection, occluderDistance, occluderRadius, !hasNearIntersection))
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

    public static bool IsOccludedByPlane(in AABB aabb, in OccluderPlanes planes)
    {
        return IsClipped(aabb, planes.Left)
            && IsClipped(aabb, planes.Right)
            && IsClipped(aabb, planes.Up)
            && IsClipped(aabb, planes.Down)
            && IsClipped(aabb, planes.Near);
    }

    public static bool IsOccludedByPlane(in AABB aabb, float3 viewer, in Quad nearPlane,
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

            if (IsOccludedByPlane(aabb, occlusionPlanes))
            {
                return true;
            }
        }

        return false;
    }
}
