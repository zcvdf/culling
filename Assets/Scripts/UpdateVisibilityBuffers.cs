using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateVisibilityBuffers : SystemBase
{
    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        this.Entities.ForEach((DynamicBuffer<VisibleOctreeCluster> visibleClusters, 
            DynamicBuffer<VisibleOctreeNode> visibleOctreeNodes,
            DynamicBuffer<VisibleNodeInClusterCount> visibleNodeInClusterCounts) =>
        {
            visibleClusters.Clear();
            visibleOctreeNodes.Clear();
            visibleNodeInClusterCounts.Clear();

            ProcessClusters(visibleClusters, visibleOctreeNodes, visibleNodeInClusterCounts, frustrumAABB, frustrumPlanes);

            #if ENABLE_ASSERTS
                AssertNoDupplicate(visibleClusters);
            #endif
        })
        .ScheduleParallel();
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
                    var clusterID = new int4(x, y, z, 0);

                    if (Math.IsCubeInFrustrum(Octree.ClusterIDToPoint(clusterID.xyz), Octree.ClusterExtent, frustrumPlanes, out var intersects))
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

                    if (Math.IsCubeInFrustrum(Octree.NodeIDToPoint(subNodeID), subNodeExtent, frustrumPlanes, out var intersects))
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
