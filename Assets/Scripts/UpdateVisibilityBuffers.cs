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
        public DynamicBuffer<VisibleOctreeCluster> VisibleClusters;
        public DynamicBuffer<VisibleOctreeNode> VisibleOctreeNodes;
        public DynamicBuffer<VisibleNodeInClusterCount> VisibleNodeInClusterCounts;
        public AABB FrustrumAABB;
        public WorldFrustrumPlanes FrustrumPlanes;

        public void Execute()
        {
            this.VisibleClusters.Clear();
            this.VisibleOctreeNodes.Clear();
            this.VisibleNodeInClusterCounts.Clear();

            AddRoot(this.VisibleClusters, this.VisibleOctreeNodes, this.VisibleNodeInClusterCounts);
            ProcessClusters(this.VisibleClusters, this.VisibleOctreeNodes, this.VisibleNodeInClusterCounts, this.FrustrumAABB, this.FrustrumPlanes);

#if ENABLE_ASSERTS
            AssertNoDupplicate(this.VisibleClusters);
#endif
        }
    }

    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        LastScheduledJob.Complete();

        var visibilityBufferEntity = GetSingletonEntity<VisibleOctreeNode>();
        var visibleClusters = this.EntityManager.GetBuffer<VisibleOctreeCluster>(visibilityBufferEntity);
        var visibleOctreeNodes = this.EntityManager.GetBuffer<VisibleOctreeNode>(visibilityBufferEntity);
        var visibleNodeInClusterCounts = this.EntityManager.GetBuffer<VisibleNodeInClusterCount>(visibilityBufferEntity);

        LastScheduledJob = new ActualJob()
        {
            VisibleClusters = visibleClusters,
            VisibleOctreeNodes = visibleOctreeNodes,
            VisibleNodeInClusterCounts = visibleNodeInClusterCounts,
            FrustrumAABB = frustrumAABB,
            FrustrumPlanes = frustrumPlanes,
        }
        .Schedule(this.Dependency);

        this.Dependency = JobHandle.CombineDependencies(LastScheduledJob, this.Dependency);

        /*this.Entities.ForEach((DynamicBuffer<VisibleOctreeCluster> visibleClusters, 
            DynamicBuffer<VisibleOctreeNode> visibleOctreeNodes,
            DynamicBuffer<VisibleNodeInClusterCount> visibleNodeInClusterCounts) =>
        {
            visibleClusters.Clear();
            visibleOctreeNodes.Clear();
            visibleNodeInClusterCounts.Clear();
            
            AddRoot(visibleClusters, visibleOctreeNodes, visibleNodeInClusterCounts);
            ProcessClusters(visibleClusters, visibleOctreeNodes, visibleNodeInClusterCounts, frustrumAABB, frustrumPlanes);

            #if ENABLE_ASSERTS
                AssertNoDupplicate(visibleClusters);
            #endif
        })
        .ScheduleParallel();*/
    }

    static void AddRoot(DynamicBuffer<VisibleOctreeCluster> visibleClusters,
            DynamicBuffer<VisibleOctreeNode> visibleOctreeNodes,
            DynamicBuffer<VisibleNodeInClusterCount> visibleNodeInClusterCounts)
    {
        visibleClusters.Add(new VisibleOctreeCluster { Value = Octree.PackedRoot });
        visibleOctreeNodes.Add(new VisibleOctreeNode { Value = Octree.PackedRoot });
        visibleNodeInClusterCounts.Add(new VisibleNodeInClusterCount { Value = 1 });
    }

    static void ProcessClusters(DynamicBuffer<VisibleOctreeCluster> visibleClusters,
            DynamicBuffer<VisibleOctreeNode> visibleOctreeNodes,
            DynamicBuffer<VisibleNodeInClusterCount> visibleNodeInClusterCounts,
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

                    if (!Math.IsCubeCulled(Octree.ClusterIDToPoint(clusterID.xyz), Octree.ClusterExtent, frustrumPlanes, out var intersects))
                    {
                        var packedClusterID = Octree.PackID(clusterID);
                        visibleClusters.Add(new VisibleOctreeCluster { Value = packedClusterID });

                        int visibleNodeCount;
                        if (intersects)
                        {
                            visibleNodeCount = ProcessNodeRecursive(visibleOctreeNodes, frustrumPlanes, clusterID);
                        }
                        else
                        {
                            visibleOctreeNodes.Add(new VisibleOctreeNode { Value = packedClusterID });
                            visibleNodeCount = 1;
                        }
                        
                        visibleNodeInClusterCounts.Add(new VisibleNodeInClusterCount { Value = visibleNodeCount });
                    }
                }
            }
        }
    }

    static int ProcessNodeRecursive(DynamicBuffer<VisibleOctreeNode> visibleOctreeNodes,
            WorldFrustrumPlanes frustrumPlanes,
            int4 nodeID,
            int depth = 0)
    {
        int4 min;
        int4 max;
        Octree.GetMinMaxNodeChildrenID(nodeID, out min, out max);
        var subDepth = depth + 1;
        var subNodeExtent = Octree.NodeExtent(subDepth);

        int visibleNodeCount = 0;

        for (int x = min.x; x < max.x; ++x)
        {
            for (int y = min.y; y < max.y; ++y)
            {
                for (int z = min.z; z < max.z; ++z)
                {
                    var subNodeID = new int4(x, y, z, subDepth);

                    if (!Math.IsCubeCulled(Octree.NodeIDToPoint(subNodeID), subNodeExtent, frustrumPlanes, out var intersects))
                    {
                        if (subDepth < Octree.LeafLayer && intersects)
                        {
                            visibleNodeCount += ProcessNodeRecursive(visibleOctreeNodes, frustrumPlanes, subNodeID, subDepth);
                        }
                        else
                        {
                            var packedID = Octree.PackID(subNodeID);
                            visibleOctreeNodes.Add(new VisibleOctreeNode { Value = packedID });

                            ++visibleNodeCount;
                        }
                    }
                }
            }
        }

        return visibleNodeCount;
    }

    static void AssertNoDupplicate(DynamicBuffer<VisibleOctreeCluster> ids)
    {
        for (int i = 0; i < ids.Length; ++i)
        {
            var a = ids[i].Value;

            for (int j = 0; j < ids.Length; ++j)
            {
                if (i == j) continue;

                var b = ids[j].Value;

                Debug.Assert(a != b);
            }
        }
    }
}
