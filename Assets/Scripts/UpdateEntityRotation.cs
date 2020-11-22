using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class UpdateEntityRotation : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = this.Time.DeltaTime;

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref Rotation rotation, in SelfRotationAxis axis, in SelfRotationSpeed speed) =>
        {
            rotation.Value = math.mul(rotation.Value, quaternion.AxisAngle(axis.Value, speed.Value * dt));
        })
        .ScheduleParallel();
    }
}
