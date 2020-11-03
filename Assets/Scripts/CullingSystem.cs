using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class CullingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var worldToNDC = Main.WorldToNDC;
        var entityOutFrumstrumColor = Main.EntityOutFrumstrumColor;
        var entityInFrumstrumColor = Main.EntityInFrustrumColor;

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation) =>
        {
            var p = translation.Value;

            var v = math.mul(worldToNDC, new float4(p.x, p.y, p.z, 1));
            var w = math.abs(v.w);

            if (
                v.x < -w || v.x > w
            ||  v.y < -w || v.y > w
            ||  v.z < -w || v.z > w)
            {
                color.Value = entityOutFrumstrumColor;
            }
            else
            {
                color.Value = entityInFrumstrumColor;
            }
        })
        .ScheduleParallel();
    }
}
