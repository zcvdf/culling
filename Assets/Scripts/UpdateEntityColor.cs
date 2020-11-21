using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref URPMaterialPropertyBaseColor color, in EntityCullingResult cullingResult) =>
        {
            switch (cullingResult.Value)
            {
                case CullingResult.CulledByOctree:          color.Value = outFrumstrumColor;    break;
                case CullingResult.CulledByFrustrumPlanes:  color.Value = outFrumstrumColor;    break;
                case CullingResult.CulledByQuadOccluder:    color.Value = occludedColor;        break;
                case CullingResult.CulledBySphereOccluder:  color.Value = occludedColor;        break;
                case CullingResult.NotCulled:               color.Value = inFrumstrumColor;     break;
            }
        })
        .ScheduleParallel();
    }
}
