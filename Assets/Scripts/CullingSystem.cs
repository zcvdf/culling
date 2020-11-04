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

        var frustrumPlanes = new NativeArray<Plane>(Main.FrustrumPlanes, Allocator.TempJob);

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(frustrumPlanes)
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radius) =>
        {
            bool inFrustrum = true;

            for (int i = 0; i < 6; ++i)
            {
                float3 normal = frustrumPlanes[i].normal;
                float distance = frustrumPlanes[i].distance;
                float3 point = -normal * distance;
                var delta = translation.Value - point;

                if (math.dot(normal, delta) < -radius.Value)
                {
                    inFrustrum = false;
                    break;
                }
            }

            color.Value = inFrustrum ? entityInFrumstrumColor : entityOutFrumstrumColor;
        })
        .WithDisposeOnCompletion(frustrumPlanes)
        .ScheduleParallel();
    }
}
