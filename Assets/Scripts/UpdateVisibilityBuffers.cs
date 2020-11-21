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
            DynamicBuffer<VisibleOctreeLeaf> visibleOctreeLeafs,
            DynamicBuffer<VisibleLeafInClusterCount> visibleLeafInClusterCounts) =>
        {
            visibleClusters.Clear();
            visibleOctreeLeafs.Clear();
            visibleLeafInClusterCounts.Clear();

            ProcessClusters(visibleClusters, visibleOctreeLeafs, visibleLeafInClusterCounts, frustrumAABB, frustrumPlanes);

            #if ENABLE_ASSERTS
                AssertNoDupplicate(visibleClusters);
            #endif
        })
        .ScheduleParallel();
    }

    static void ProcessClusters(DynamicBuffer<VisibleOctreeCluster> visibleClusters,
            DynamicBuffer<VisibleOctreeLeaf> visibleOctreeLeafs,
            DynamicBuffer<VisibleLeafInClusterCount> visibleLeafInClusterCounts,
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

                    if (Math.IsCubeInFrustrum(Octree.ClusterIDToPoint(clusterID), Octree.ClusterExtent, frustrumPlanes))
                    {
                        var packedClusterID = Octree.PackID(clusterID);

                        visibleClusters.Add(new VisibleOctreeCluster { Value = packedClusterID });

                        var visibleLeafCount = ProcessNodeRecursive(visibleOctreeLeafs, frustrumPlanes, clusterID);

                        visibleLeafInClusterCounts.Add(new VisibleLeafInClusterCount { Value = visibleLeafCount });
                    }
                }
            }
        }
    }

    static int ProcessNodeRecursive(DynamicBuffer<VisibleOctreeLeaf> visibleOctreeLeafs,
            WorldFrustrumPlanes frustrumPlanes,
            int4 nodeID,
            int depth = 0)
    {
        int4 min;
        int4 max;
        Octree.GetMinMaxNodeChildrenID(nodeID, out min, out max);
        var subDepth = depth + 1;
        var subNodeExtent = Octree.NodeExtent(subDepth);

        int visibleLeafCount = 0;

        for (int x = min.x; x < max.x; ++x)
        {
            for (int y = min.y; y < max.y; ++y)
            {
                for (int z = min.z; z < max.z; ++z)
                {
                    var subNodeID = new int4(x, y, z, 0);
                    
                    if (Math.IsCubeInFrustrum(Octree.NodeIDToPoint(subNodeID, subDepth), subNodeExtent, frustrumPlanes))
                    {
                        if (subDepth < Octree.LeafLayer)
                        {
                            visibleLeafCount += ProcessNodeRecursive(visibleOctreeLeafs, frustrumPlanes, subNodeID, subDepth);
                        }
                        else
                        {
                            var packedID = Octree.PackID(subNodeID);
                            visibleOctreeLeafs.Add(new VisibleOctreeLeaf { Value = packedID });

                            ++visibleLeafCount;
                        }
                    }
                }
            }
        }

        return visibleLeafCount;
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
