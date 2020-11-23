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

            AddRoot(this.VisibleSets[0]);
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

    static void AddRoot(NativeHashSet<ulong> setLayer0)
    {
        setLayer0.Add(Octree.PackedRoot);
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

                    ProcessNodeRecursive(visibleSets, frustrumPlanes, clusterID);
                }
            }
        }
    }

    static void ProcessNodeRecursive(VisibleSets visibleSets,
            WorldFrustrumPlanes frustrumPlanes,
            int4 nodeID,
            int depth = 0)
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

                    if (!Math.IsCubeCulled(Octree.NodeIDToPoint(subNodeID), subNodeExtent, frustrumPlanes, out var intersects))
                    {
                        var packedID = Octree.PackID(subNodeID);
                        visibleSets[depth].Add(packedID);

                        if (depth < Octree.LeafLayer && intersects)
                        {
                            ProcessNodeRecursive(visibleSets, frustrumPlanes, subNodeID, depth + 1);
                        }
                    }
                }
            }
        }
    }
}
