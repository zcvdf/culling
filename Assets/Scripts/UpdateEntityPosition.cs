using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class UpdateEntityPosition : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = this.Time.DeltaTime;

        this.Entities
        .WithAll<EntityTag>()
        .ForEach((ref Translation position, in WorldRotationAxis axis, in WorldRotationSpeed speed) =>
        {
            var rotation = quaternion.AxisAngle(axis.Value, speed.Value * dt);

            position.Value = math.mul(rotation, position.Value);
        })
        .ScheduleParallel();
    }
}
