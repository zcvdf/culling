using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ResetEntityCullingResult : SystemBase
{
    protected override void OnUpdate()
    {
        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref EntityCullingResult cullingResult) =>
        {
            cullingResult.Value = CullingResult.CulledByOctreeClusters;
        })
        .ScheduleParallel();
    }
}
