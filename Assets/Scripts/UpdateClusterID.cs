using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
public class UpdateClusterID : SystemBase
{
    protected override void OnUpdate()
    {
        var cmd = new EntityCommandBuffer(Allocator.Temp);

        this.Entities
        .WithChangeFilter<Translation>()
        .WithAll<EntityTag>()
        .WithoutBurst()
        .ForEach((in Translation translation, in Entity entity, in ClusterID currentID) =>
        {
            var newID = Octree.PackID(Octree.PointToIDLayer0(translation.Value));

            if (newID != currentID.Value)
            {
                cmd.SetSharedComponent(entity, new ClusterID { Value = newID });
            }
        })
        .Run();

        cmd.Playback(this.EntityManager);
    }
}
