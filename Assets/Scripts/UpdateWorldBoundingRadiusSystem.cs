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
    protected override void OnUpdate()
    {
        this.Entities
        .WithChangeFilter<WorldRenderBounds>()
        .ForEach((ref WorldBoundingRadius radius, in WorldRenderBounds bounds) =>
        {
            var extents = bounds.Value.Extents;

            radius.Value = math.max(math.max(extents.x, extents.y), extents.z) * Const.SQRT3;
        })
        .ScheduleParallel();
    }
}
