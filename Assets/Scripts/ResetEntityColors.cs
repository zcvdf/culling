using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ResetEntityColors : SystemBase
{
    protected override void OnUpdate()
    {
        var entityOutFrumstrumColor = Main.EntityOutFrumstrumColor;

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref URPMaterialPropertyBaseColor color) =>
        {
            color.Value = entityOutFrumstrumColor;
        })
        .ScheduleParallel();
    }
}
