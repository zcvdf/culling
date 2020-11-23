using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[UpdateAfter(typeof(CullingSystem))]
public class UpdateEntityColor : SystemBase
{
    protected override void OnUpdate()
    {
        var outFrumstrumColor = Main.EntityOutFrumstrumColor;
        var inFrumstrumColor = Main.EntityInFrustrumColor;
        var occludedColor = Main.EntityOccludedColor;

        var colors = new NativeArray<float4>(6, Allocator.TempJob);
        colors[0] = inFrumstrumColor;
        colors[1] = outFrumstrumColor;
        colors[2] = outFrumstrumColor;
        colors[3] = outFrumstrumColor;
        colors[4] = occludedColor;
        colors[5] = occludedColor;

        var rootLayerColor = Main.EntityAtRootLayerColor;
        var showRootLayer = Main.ShowRootLayerEntities;

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(colors)
        .ForEach((ref URPMaterialPropertyBaseColor color, in EntityCullingResult cullingResult, in OctreeNode octreeNode) =>
        {
            if (showRootLayer && octreeNode.Value == Octree.PackedRoot)
            {
                color.Value = rootLayerColor;
            }
            else
            {
                var id = (int)cullingResult.Value;
                color.Value = colors[id];
            }
        })
        .WithDisposeOnCompletion(colors)
        .ScheduleParallel();
    }
}
