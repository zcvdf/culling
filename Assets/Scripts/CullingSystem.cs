using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
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

[UpdateAfter(typeof(UpdateWorldBoundingRadiusSystem))]
public class CullingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var viewer = Main.Viewer;
        var worldToNDC = Main.WorldToNDC;
        var entityOutFrumstrumColor = Main.EntityOutFrumstrumColor;
        var entityInFrumstrumColor = Main.EntityInFrustrumColor;
        var entityOccludedColor = Main.EntityOccludedColor;

        var sphereOccluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));
        var planeOccluderQuery = GetEntityQuery(typeof(WorldOccluderExtents), typeof(Translation));

        var frustrumPlanes = new NativeArray<Plane>(Main.FrustrumPlanes, Allocator.TempJob);
        var sphereOccluderTranslations = sphereOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var sphereOccluderRadiuses = sphereOccluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        var planeOccluderTranslations = planeOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var planeOccluderExtents = planeOccluderQuery.ToComponentDataArray<WorldOccluderExtents>(Allocator.TempJob);

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(frustrumPlanes)
        .WithReadOnly(sphereOccluderTranslations)
        .WithReadOnly(sphereOccluderRadiuses)
        .WithReadOnly(planeOccluderTranslations)
        .WithReadOnly(planeOccluderExtents)
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radiusComponent) =>
        {
            var center = translation.Value;
            var radius = radiusComponent.Value;

            var isInFrustrum = IsInFrustrum(center, radius, frustrumPlanes);
            if (!isInFrustrum)
            {
                color.Value = entityOutFrumstrumColor;
            }
            else
            {
                var isSphereOccluded = 
                IsOccludedBySphere(center, radius, viewer, sphereOccluderTranslations, sphereOccluderRadiuses, frustrumPlanes)
                || IsOccludedByPlane(center, radius, viewer, planeOccluderTranslations, planeOccluderExtents, frustrumPlanes);

                color.Value = isSphereOccluded ? entityOccludedColor : entityInFrumstrumColor;
            }
        })
        .WithDisposeOnCompletion(planeOccluderExtents)
        .WithDisposeOnCompletion(planeOccluderTranslations)
        .WithDisposeOnCompletion(sphereOccluderRadiuses)
        .WithDisposeOnCompletion(sphereOccluderTranslations)
        .WithDisposeOnCompletion(frustrumPlanes)
        .ScheduleParallel();
    }

    static float SignedDistanceToPlane(float3 point, Plane plane)
    {
        float3 normal = plane.normal;
        float distance = plane.distance;
        float3 planePoint = -normal * distance;
        var delta = point - planePoint;

        return math.dot(normal, delta);
    }

    static bool IsClipped(float3 center, float radius, Plane plane)
    {
        return SignedDistanceToPlane(center, plane) < -radius;
    }

    static bool IsInFrustrum(float3 center, float radius, in NativeArray<Plane> planes)
    {
        for (int i = 0; i < 6; ++i)
        {
            if (IsClipped(center, radius, planes[i]))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsSphereOccluderInFrustrum(float3 center, float radius, in NativeArray<Plane> planes, out bool hasNearIntersection)
    {
        // Special handling of the near clipping plane for occluders (planes[4])
        // We want the occluder to be discarded if its center is behind the near plane
        // Otherwise the objects made visible by the clipping of the near plane get culled out

        var nearPlane = planes[4];
        hasNearIntersection = false;

        if 
        (
            IsClipped(center, radius, planes[0])
            || IsClipped(center, radius, planes[1])
            || IsClipped(center, radius, planes[2])
            || IsClipped(center, radius, planes[3])
            || IsClipped(center, radius, planes[5])
        )
        {
            return false;
        }

        var nearDist = SignedDistanceToPlane(center, nearPlane);
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

    static bool IsSphereOccluded(float3 viewerToObject, float objectRadius, float3 viewerToOccluder, float3 occluderDirection, 
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

    static bool IsOccludedBySphere(float3 testedCenter, float testedRadius, float3 viewer,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderRadius> occluderRadiuses, in NativeArray<Plane> frustrumPlanes)
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

    static float3 GetOccluderlaneNormal(float3 localRight, float3 localUp)
    {
        return math.cross(localUp, localRight);
    }

    static bool OccluderPlaneHasContribution(float3 planeNormal, float3 viewerToCenter)
    {
        return math.dot(planeNormal, viewerToCenter) < 0f;
    }

    static OccluderPlanes GetOccluderPlanes(float3 viewer, float3 center, float3 occluderNormal, float3 localRight, float localRightLength, float3 localUp, float localUpLength)
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

    static bool IsOccludedByPlane(float3 testedCenter, float testedRadius, in OccluderPlanes planes)
    {
        return
            IsClipped(testedCenter, testedRadius, planes.Left)
            && IsClipped(testedCenter, testedRadius, planes.Right)
            && IsClipped(testedCenter, testedRadius, planes.Up)
            && IsClipped(testedCenter, testedRadius, planes.Down)
            && IsClipped(testedCenter, testedRadius, planes.Near);
    }

    static bool IsOccludedByPlane(float3 testedCenter, float testedRadius, float3 viewer,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderExtents> occluderExtents, in NativeArray<Plane> frustrumPlanes)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var center = occluderTranslations[i].Value;
            var localRight = occluderExtents[i].LocalRight;
            var localRightLength = occluderExtents[i].LocalRightLength;
            var localUp = occluderExtents[i].LocalUp;
            var localUpLength = occluderExtents[i].LocalUpLength;

            var viewerToCenter = math.normalize(center - viewer);
            var occluderNormal = GetOccluderlaneNormal(localRight, localUp);

            if (!OccluderPlaneHasContribution(occluderNormal, viewerToCenter)) continue;

            var occlusionPlanes = GetOccluderPlanes(viewer, center, occluderNormal, localRight, localRightLength, localUp, localUpLength);

            if (IsOccludedByPlane(testedCenter, testedRadius, occlusionPlanes))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Intersect(in Plane plane0, in Plane plane1, out float3 point)
    {
        // Unoptimized version looks like this. 
        // Basically trying to find the intersection point between 3 planes by solving a system with 3 member using an inverse matrix.

        // var plane2 = new Plane(math.cross(plane0.normal, plane1.normal), 0f);
        // var mat = math.transpose(new float3x3 { c0 = plane0.normal, c1 = plane1.normal, c2 = plane2.normal });
        // var invMat = math.inverse(mat);
        // 
        // return math.mul(invMat, new float3(-plane0.distance, -plane1.distance, -plane2.distance));

        // The optimized version is doing the same thing but much faster.
        // The trick comes from the fact that one of the vector involved in the system is the cross product of the two other.

        // Matrix3x3 determinant is equal to dot(a, cross(b, c)) with a, b, c the row vectors of the matrix.
        // Here, a = cross(b, c), so the determinant becomes dot(a, a) which becomes lengthsq(a)

        // We also assume that the 3rd plane distance is always 0. This is possible since the plane has a normal orthogonal to the 2 other planes.
        // Which means that, assuming the 2 planes are not parallel : 
        // The third plane is always going to cut the intersection line of the two planes no matter its position.

        var cross = math.cross(plane0.normal, plane1.normal);

        float det = math.lengthsq(cross);

        if (det == 0f)
        {
            point = float3.zero;
            return false;
        }

        point = (math.cross(cross, plane1.normal) * plane0.distance + math.cross(plane0.normal, cross) * plane1.distance) / det;
        return true;
    }

    public static bool Intersect(in Plane plane, in Quad quad)
    {
        float3 point;
        if (!Intersect(plane, new Plane(quad.Normal, quad.Center), out point)) return false;

        var quadToPoint = point - quad.Center;

        var x = math.abs(math.dot(quad.LocalRight, quadToPoint));
        var maxX = math.lengthsq(quad.LocalRight);

        if (x > maxX) return false;

        var y = math.abs(math.dot(quad.LocalUp, quadToPoint));
        var maxY = math.lengthsq(quad.LocalUp);

        if (y > maxY) return false;

        return true;
    }
}
