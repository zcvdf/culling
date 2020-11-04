using System.Collections;
using System.Collections.Generic;
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

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radius) =>
        {
            var p = translation.Value;

            var v = math.mul(worldToNDC, new float4(p.x, p.y, p.z, 1));
            var w = math.abs(v.w);

            var isInX = v.x > -w && v.x < w;
            var isInY = v.y > -w && v.y < w;
            var isInZ = v.z > -w && v.z < w;

            if (isInX && isInY && isInZ)
            {
                color.Value = entityInFrumstrumColor;
            }
            else
            {
                color.Value = entityOutFrumstrumColor;
            }
        })
        .ScheduleParallel();
    }
}
