using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using System;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateOctreeID : SystemBase
{
    protected override void OnUpdate()
    {
        this.Entities
        .WithChangeFilter<Translation>()
        .ForEach((ref OctreeID id, in Translation translation, in Entity entity) =>
        {
            var newID = Octree.PackID(Octree.PointToILeafID(translation.Value));

            id = new OctreeID
            {
                Value = newID
            };
        })
        .ScheduleParallel();
    }
}