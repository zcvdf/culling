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

    protected override void OnDestroy()
    {
        var visibilityBufferEntity = GetSingletonEntity<VisibilityBuffer>();
        var visibilityBuffer = this.EntityManager.GetComponentData<VisibilityBuffer>(visibilityBufferEntity);

        visibilityBuffer.Layer0.Dispose();
        visibilityBuffer.Layer1.Dispose();
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
}
