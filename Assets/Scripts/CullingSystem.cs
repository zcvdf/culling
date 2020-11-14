using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Jobs;
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
        var nearPlane = Main.NearPlane;
        var nearPlaneCenter = Main.NearPlaneCenter;
        var worldToNDC = Main.WorldToNDC;
        var entityInFrumstrumColor = Main.EntityInFrustrumColor;
        var entityOccludedColor = Main.EntityOccludedColor;
        var frustrumAABB = Main.FrustrumAABB;

        var sphereOccluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));
        var planeOccluderQuery = GetEntityQuery(typeof(WorldOccluderExtents), typeof(Translation));

        var frustrumPlanes = new NativeArray<Plane>(Main.FrustrumPlanes, Allocator.TempJob);
        var sphereOccluderTranslations = sphereOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var sphereOccluderRadiuses = sphereOccluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        var planeOccluderTranslations = planeOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var planeOccluderExtents = planeOccluderQuery.ToComponentDataArray<WorldOccluderExtents>(Allocator.TempJob);

        var visibleOctreeNodes = GetVisibleOctreeNodes(frustrumPlanes, frustrumAABB);

        // For the moment, each jobs wait for the previous one. 
        // Wait for an answer from the DOTS team to know how to fix this without triggering safety checks.
        foreach (OctreeID octreeID in visibleOctreeNodes)
        {
            this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter(octreeID)
            .WithReadOnly(frustrumPlanes)
            .WithReadOnly(sphereOccluderTranslations)
            .WithReadOnly(sphereOccluderRadiuses)
            .WithReadOnly(planeOccluderTranslations)
            .WithReadOnly(planeOccluderExtents)
            .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radiusComponent) =>
            {
                var center = translation.Value;
                var radius = radiusComponent.Value;

                if (!IsInFrustrum(center, radius, frustrumPlanes)) return;

                var isSphereOccluded =
                    IsOccludedBySphere(center, radius, viewer, sphereOccluderTranslations, sphereOccluderRadiuses, frustrumPlanes)
                    || IsOccludedByPlane(center, radius, viewer, nearPlane, planeOccluderTranslations, planeOccluderExtents, frustrumPlanes);

                color.Value = isSphereOccluded ? entityOccludedColor : entityInFrumstrumColor;
            })
            .ScheduleParallel();
        }

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
        frustrumPlanes.Dispose(this.Dependency);
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

    static bool OccluderPlaneHasContribution(in Quad occluder, in Quad nearPlane)
    {
        var occluderToNearPlane = nearPlane.Center - occluder.Center;
        var signedDist = math.dot(occluderToNearPlane, occluder.Normal);

        if (signedDist < 0f) return false;

        var distSq = signedDist * signedDist;

        // Add small epsilon to avoid having to deal with too tiny float values
        var nearBoundingRadiusSq = math.lengthsq(nearPlane.LocalRight + nearPlane.LocalUp) + 0.1f; 

        return distSq > nearBoundingRadiusSq;
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

    static bool IsOccludedByPlane(float3 testedCenter, float testedRadius, float3 viewer, in Quad nearPlane,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderExtents> occluderExtents, in NativeArray<Plane> frustrumPlanes)
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

    static List<OctreeID> GetVisibleOctreeNodes(NativeArray<Plane> planes, AABB frustrumAABB)
    {
        var visible = new List<OctreeID>();

        Octree.ForEachBoundingNode0(frustrumAABB, (int3 id0) =>
        {
            var center0 = Octree.IDLayer0ToPoint(id0);
            var radius0 = Octree.Node0BoundingRadius;

            if (IsInFrustrum(center0, radius0, planes))
            {
                Octree.ForEachNode0Childs(id0, (int3 id1) =>
                {
                    var center1 = Octree.IDLayer1ToPoint(id1);
                    var radius1 = Octree.Node1BoundingRadius;

                    if (IsInFrustrum(center1, radius1, planes))
                    {
                        var id = new OctreeID
                        {
                            ID0 = id0,
                            ID1 = id1,
                        };

                        visible.Add(id);
                    }
                });
            }
        });

#if ENABLE_ASSERTS
        AssertNoDupplicate(visible);
#endif

        return visible;
    }

    static void AssertNoDupplicate(List<OctreeID> ids)
    {
        for (int i = 0; i < ids.Count; ++i)
        {
            var a0 = ids[i].ID0;
            var a1 = ids[i].ID1;

            for (int j = 0; j < ids.Count; ++j)
            {
                if (i == j) continue;

                var b0 = ids[j].ID0;
                var b1 = ids[j].ID1;

                Debug.Assert(math.any(a0 != b0) || math.any(a1 != b1));
            }
        }
    }
}
