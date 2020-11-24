using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateVisibilityBuffers : SystemBase
{
    public static JobHandle LastScheduledJob;

    struct ActualJob : IJob
    {
        public VisibleSets VisibleSets;
        public AABB FrustrumAABB;
        public WorldFrustrumPlanes FrustrumPlanes;

        public void Execute()
        {
            this.VisibleSets.Clear();

            ProcessFrustrumClusters(this.VisibleSets, this.FrustrumAABB, this.FrustrumPlanes);
        }
    }

    protected override void OnDestroy()
    {
        var visibleSetsEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibleSetsEntity).Value;

        visibleSets.Dispose();
    }

    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        LastScheduledJob.Complete();

        var visibleSetsEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibleSetsEntity).Value;

        LastScheduledJob = new ActualJob()
        {
            VisibleSets = visibleSets,
            FrustrumAABB = frustrumAABB,
            FrustrumPlanes = frustrumPlanes,
        }
        .Schedule(this.Dependency);

        this.Dependency = JobHandle.CombineDependencies(LastScheduledJob, this.Dependency);
    }

    static void ProcessFrustrumClusters(VisibleSets visibleSets,
            AABB frustrumAABB,
            WorldFrustrumPlanes frustrumPlanes)
    {
        int4 min;
        int4 max;
        Octree.GetMinMaxClusterIDs(frustrumAABB, out min, out max);

        for (int x = min.x; x < max.x; ++x)
        {
            for (int y = min.y; y < max.y; ++y)
            {
                for (int z = min.z; z < max.z; ++z)
                {
                    var clusterID = new int4(x, y, z, Octree.ClusterLayer);

                    if (!Math.IsCubeCulled(Octree.ClusterIDToPoint(clusterID.xyz), Octree.ClusterExtent, frustrumPlanes))
                    {
                        var packedID = Octree.PackID(clusterID);
                        visibleSets.ClusterLayer.Add(packedID);

                        ProcessNodeChildrenRecursive(visibleSets, frustrumPlanes, clusterID, 1);
                    }
                }
            }
        }
    }

    static void ProcessNodeChildrenRecursive(VisibleSets visibleSets,
            WorldFrustrumPlanes frustrumPlanes,
            int4 nodeID,
            int depth)
    {
        int4 min;
        int4 max;
        Octree.GetMinMaxNodeChildrenID(nodeID, out min, out max);
        var subNodeExtent = Octree.NodeExtent(depth);

        for (int x = min.x; x < max.x; ++x)
        {
            for (int y = min.y; y < max.y; ++y)
            {
                for (int z = min.z; z < max.z; ++z)
                {
                    var subNodeID = new int4(x, y, z, depth);

                    if (!Math.IsCubeCulled(Octree.NodeIDToPoint(subNodeID), subNodeExtent, frustrumPlanes))
                    {
                        var packedID = Octree.PackID(subNodeID);
                        visibleSets[depth].Add(packedID);

                        if (depth < Octree.LeafLayer)
                        {
                            ProcessNodeChildrenRecursive(visibleSets, frustrumPlanes, subNodeID, depth + 1);
                        }
                    }
                }
            }
        }
    }
}
