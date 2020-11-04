using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
public class UpdateWorldBoundingRadiusSystem : SystemBase
{
    public const float SQRT3 = 1.73205080f;

    protected override void OnUpdate()
    {
        this.Entities
        .ForEach((ref WorldBoundingRadius radius, in WorldRenderBounds bounds) =>
        {
            var extents = bounds.Value.Extents;

            radius.Value = math.max(math.max(extents.x, extents.y), extents.z) * SQRT3;
        })
        .ScheduleParallel();
    }
}
