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
        .ForEach((in Translation translation, in Entity entity, in OctreeCluster cluster) =>
        {
            var newCluster = Octree.PackID(Octree.PointToClusterID(translation.Value));

            if (newCluster != cluster.Value)
            {
                cmd.SetSharedComponent(entity, new OctreeCluster { Value = newCluster });
            }
        })
        .Run();

        cmd.Playback(this.EntityManager);
    }
}
