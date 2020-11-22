using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
public class UpdateEntityOctreeCluster : SystemBase
{
    protected override void OnUpdate()
    {
        var cmd = new EntityCommandBuffer(Allocator.Temp);

        this.Entities
        .WithChangeFilter<WorldRenderBounds>()
        .WithAll<EntityTag>()
        .WithoutBurst()
        .ForEach((in WorldRenderBounds boundsComponent, in Entity entity, in OctreeCluster cluster) =>
        {
            var bounds = boundsComponent.Value;

            var newIDUnpacked = ComputeClusterID(bounds);

            var newID = Octree.PackID(newIDUnpacked);

            if (newID != cluster.Value)
            {
                cmd.SetSharedComponent(entity, new OctreeCluster { Value = newID });
            }
        })
        .Run();

        cmd.Playback(this.EntityManager);
    }

    private static int4 ComputeClusterID(in AABB bounds)
    {
        var clusterID = Octree.PointToClusterID(bounds.Center);

        var clusterAABB = new AABB();
        clusterAABB.Center = Octree.ClusterIDToPoint(clusterID.xyz);
        clusterAABB.Extents = new float3(Octree.ClusterExtent);

        if (clusterAABB.Contains(bounds))
        {
            return clusterID;
        }
        else
        {
            return Octree.Root;
        }
    }
}
