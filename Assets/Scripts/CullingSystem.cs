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
            var isOccluded = IsOccluded(center, radius, occluderTranslations, occluderRadiuses);

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

    static bool IsInFrustrum(float3 center, float radius, in NativeArray<Plane> planes)
    {
        for (int i = 0; i < 6; ++i)
        {
            float3 normal = planes[i].normal;
            float distance = planes[i].distance;
            float3 point = -normal * distance;
            var delta = center - point;

            if (math.dot(normal, delta) < -radius)
            {
                return false;
            }
        }

        return true;
    }

    static bool IsOccluded(float3 center, float radius, float3 viewer, float3 occluderDirection, float occluderDistance, float3 occluderCenter, float occluderRadius)
    {
        var viewerToObject = center - viewer;
        var objectProjectedDistance = math.dot(occluderDirection, viewerToObject);
        var objectProjection = viewer + occluderDirection * objectProjectedDistance;
        var ratio = objectProjectedDistance / occluderDistance;

        var maxDist = ratio * occluderRadius - radius;
        var maxDistSq = maxDist * maxDist;

        var projectionToObject = center - objectProjection;

        return math.lengthsq(projectionToObject) < maxDistSq;
    }

    static bool IsOccluded(float3 center, float radius, 
        in NativeArray<Translation> occluderTranslations, in NativeArray<WorldOccluderRadius> occluderRadiuses)
    {
        for (int i = 0; i < occluderTranslations.Length; ++i)
        {
            var occluderCenter = occluderTranslations[i].Value;
            var occluderRadius = occluderRadiuses[i].Value;
            var viewer = float3.zero;
            var viewerToOccluder = occluderCenter - viewer;
            var occluderDistance = math.length(viewerToOccluder);
            var occluderDirection = viewerToOccluder / occluderDistance;

            if (IsOccluded(center, radius, viewer, occluderDirection, occluderDistance, occluderCenter, occluderRadius))
            {
                return true;
            }
        }

        return false;
    }
}
