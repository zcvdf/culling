using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MovemenSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = this.Time.DeltaTime;

        this.Entities
        .ForEach((ref Translation translation) =>
        {
            var up = new float3(0, 1, 0);
            translation.Value = math.rotate(quaternion.AxisAngle(up, dt), translation.Value);
        })
        .ScheduleParallel();
    }
}
