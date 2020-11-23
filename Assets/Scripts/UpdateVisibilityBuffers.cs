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
        public NativeHashSet<ulong> Layer0;
        public NativeHashSet<ulong> Layer1;
        public AABB FrustrumAABB;
        public WorldFrustrumPlanes FrustrumPlanes;

        public void Execute()
        {
            this.Layer0.Clear();
            this.Layer1.Clear();

            AddRoot(this.Layer0);
            ProcessClusters(this.Layer0, this.Layer1, this.FrustrumAABB, this.FrustrumPlanes);
        }
    }

    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        LastScheduledJob.Complete();

        var visibilityBufferEntity = GetSingletonEntity<VisibilityBuffer>();
        var visibilityBuffer = this.EntityManager.GetComponentData<VisibilityBuffer>(visibilityBufferEntity);

        LastScheduledJob = new ActualJob()
        {
            Layer0 = visibilityBuffer.Layer0,
            Layer1 = visibilityBuffer.Layer1,
            FrustrumAABB = frustrumAABB,
            FrustrumPlanes = frustrumPlanes,
        }
        .Schedule(this.Dependency);

        this.Dependency = JobHandle.CombineDependencies(LastScheduledJob, this.Dependency);

        this.Entities.ForEach((DynamicBuffer<VisibleOctreeCluster> visibleClusters, 
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
        .ScheduleParallel();
    }

    static void AddRoot(NativeHashSet<ulong> setLayer0)
    {
        setLayer0.Add(Octree.PackedRoot);
    }

    static void ProcessClusters(NativeHashSet<ulong> setLayer0,
            NativeHashSet<ulong> setLayer1,
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
                        setLayer0.Add(packedClusterID);

                        if (intersects)
                        {
                            ProcessLayer1(setLayer1, frustrumPlanes, clusterID);
                        }
                    }
                }
            }
        }
    }

    static void ProcessLayer1(NativeHashSet<ulong> setLayer1,
            WorldFrustrumPlanes frustrumPlanes,
            int4 nodeID,
            int depth = 0)
    {
        int4 min;
        int4 max;
        Octree.GetMinMaxNodeChildrenID(nodeID, out min, out max);
        var subDepth = depth + 1;
        var subNodeExtent = Octree.NodeExtent(subDepth);

        for (int x = min.x; x < max.x; ++x)
        {
            for (int y = min.y; y < max.y; ++y)
            {
                for (int z = min.z; z < max.z; ++z)
                {
                    var subNodeID = new int4(x, y, z, subDepth);

                    if (!Math.IsCubeCulled(Octree.NodeIDToPoint(subNodeID), subNodeExtent, frustrumPlanes, out var intersects))
                    {
                        var packedID = Octree.PackID(subNodeID);
                        setLayer1.Add(packedID);
                    }
                }
            }
        }
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
