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

        var occluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));

        var frustrumPlanes = new NativeArray<Plane>(Main.FrustrumPlanes, Allocator.TempJob);
        //var occluderTranslations = occluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        //var occluderRadiuses = occluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(frustrumPlanes)
        /*.WithReadOnly(occluderTranslations)
        .WithReadOnly(occluderRadiuses)*/
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radius) =>
        {
            bool inFrustrum = IsInFrustrum(translation.Value, radius.Value, frustrumPlanes);

            color.Value = inFrustrum ? entityInFrumstrumColor : entityOutFrumstrumColor;
        })
        /*.WithDisposeOnCompletion(occluderRadiuses)
        .WithDisposeOnCompletion(occluderTranslations)*/
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
}
