using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

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

        var occluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));

        var frustrumPlanes = new NativeArray<Plane>(Main.FrustrumPlanes, Allocator.TempJob);
        var occluderTranslations = occluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var occluderRadiuses = occluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(frustrumPlanes)
        .WithReadOnly(occluderTranslations)
        .WithReadOnly(occluderRadiuses)
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radiusComponent) =>
        {
            var center = translation.Value;
            var radius = radiusComponent.Value;

            var isInFrustrum = IsInFrustrum(center, radius, frustrumPlanes);
            var isOccluded = IsOccluded(center, radius, viewer, occluderTranslations, occluderRadiuses, frustrumPlanes);

            if (!isInFrustrum)
            {
                color.Value = entityOutFrumstrumColor;
            }
            else
            {
                color.Value = isOccluded ? entityOccludedColor : entityInFrumstrumColor;
            }
        })
        .WithDisposeOnCompletion(occluderRadiuses)
        .WithDisposeOnCompletion(occluderTranslations)
        .WithDisposeOnCompletion(frustrumPlanes)
        .ScheduleParallel();
    }

    static bool IsClipped(float3 center, float radius, Plane plane)
    {
        float3 normal = plane.normal;
        float distance = plane.distance;
        float3 point = -normal * distance;
        var delta = center - point;

        return -math.dot(normal, delta) > radius;
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

    static bool IsOccluderInFrustrum(float3 center, float radius, in NativeArray<Plane> planes)
    {
        // Special handling of the near clipping plane for occluders (planes[4])
        // We want the occluder to be discarded if its center is behind the near plane
        // Otherwise the objects made visible by the clipping of the near plane get culled out
        if 
        (
            IsClipped(center, radius, planes[0])
            || IsClipped(center, radius, planes[1])
            || IsClipped(center, radius, planes[2])
            || IsClipped(center, radius, planes[3])
            || IsClipped(center, 0f, planes[4])
            || IsClipped(center, radius, planes[5])
        )
        {
            return false;
        }

        return true;
    }

    static bool IsOccluded(float3 viewerToObject, float objectRadius, float3 viewerToOccluder, float3 occluderDirection, float occluderDistance, float occluderRadius)
    {
        var objectProjectedDistance = math.dot(occluderDirection, viewerToObject);
        var objectProjectedNear = objectProjectedDistance - objectRadius;

        // Not occluded if behind the near slice of the occlusion cone
        if (objectProjectedNear < occluderDistance) return false;

        var occluderToObject = viewerToObject - viewerToOccluder;

        var minDistToOccluderSq = occluderRadius + objectRadius;
        minDistToOccluderSq *= minDistToOccluderSq;

        // Not occluded if in the occluder sphere
        if (math.lengthsq(occluderToObject) < minDistToOccluderSq) return false;

        var objectProjection = occluderDirection * objectProjectedDistance;
        var ratio = objectProjectedDistance / occluderDistance;

        var maxDist = ratio * occluderRadius - objectRadius;
        var maxDistSq = maxDist * maxDist;

        var projectionToObject = viewerToObject - objectProjection;

        // If the boudning sphere fits in the occlusion cone, cull it out
        return math.lengthsq(projectionToObject) < maxDistSq;
    }

    static bool IsOccluded(float3 testedCenter, float testedRadius, float3 viewer,
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderRadius> occluderRadiuses, in NativeArray<Plane> frustrumPlanes)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var occluderCenter = occluderTranslations[i].Value;
            var occluderRadius = occluderRadiuses[i].Value;

            if (!IsOccluderInFrustrum(occluderCenter, occluderRadius, frustrumPlanes)) continue;

            var viewerToTested = testedCenter - viewer;
            var viewerToOccluder = occluderCenter - viewer;
            var occluderDistance = math.length(viewerToOccluder);
            var occluderDirection = viewerToOccluder / occluderDistance;

            if (IsOccluded(viewerToTested, testedRadius, viewerToOccluder, occluderDirection, occluderDistance, occluderRadius))
            {
                return true;
            }
        }

        return false;
    }
}
