using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(HybridRendererSystem))]
public class UpdateStats : SystemBase
{
    const int NbFrameSample = 10;

    bool wasLockedLastUpdate = false;
    double lastElapsedTime;
    int frame;

    protected override void OnStartRunning()
    {
        this.lastElapsedTime = this.Time.ElapsedTime;
    }

    protected override void OnUpdate()
    {
        UpdateFPSDatas();

        if (!Main.DisplayStats) return;
        if (Main.IsLocked && this.wasLockedLastUpdate) return;

        this.wasLockedLastUpdate = false;

        UpdateVisibilityBuffers.LastScheduledJob.Complete();

        var visibleOctreeEntity = GetSingletonEntity<VisibleOctreeNode>();
        var visibleClusterEntity = GetSingletonEntity<VisibleOctreeCluster>();

        var visibleNodes = GetBuffer<VisibleOctreeNode>(visibleOctreeEntity).AsNativeArray();
        var visibleClusters = GetBuffer<VisibleOctreeCluster>(visibleClusterEntity).AsNativeArray();

        var stats = new NativeArray<int>(7, Allocator.TempJob);

        foreach (var visibleCluster in visibleClusters)
        {
            this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter<OctreeCluster>(visibleCluster)
            .WithNativeDisableParallelForRestriction(stats)
            .ForEach((in EntityCullingResult result, in OctreeNode octreeNode) =>
            {
                var id = (int)result.Value;
                ++stats[id];

                if (octreeNode.Value == Octree.PackedRoot)
                {
                    ++stats[6];
                }    
            })
            .Run();
        }

        var notCulled = stats[0];

        // stats[1] is a placeholder. 
        // Would contain the number of entities culled by the octree clusters if they were not excluded from the query
        Debug.Assert(stats[1] == 0);

        var culledByOctreeNodes = stats[2];
        var culledByFrustrumPlanes = stats[3];
        var culledBySphereOccluder = stats[4];
        var culledByQuadOccluder = stats[5];
        var atRootOctreeLayer = stats[6];

        var culledByOctreeClusters = Stats.TotalEntityNumber - culledByOctreeNodes 
            - culledByFrustrumPlanes - culledByQuadOccluder - culledBySphereOccluder - notCulled;

        Stats.CulledByOctreeNodes = culledByOctreeNodes;
        Stats.CulledByFrustrumPlanes = culledByFrustrumPlanes;
        Stats.CulledByQuadOccluders = culledByQuadOccluder;
        Stats.CulledBySphereOccluders = culledBySphereOccluder;
        Stats.CulledByOctreeClusters = culledByOctreeClusters;
        Stats.VisibleOctreeNodes = visibleNodes.Length;
        Stats.VisibleOctreeClusters = visibleClusters.Length;
        Stats.AtRootOctreeLayer = atRootOctreeLayer;

        stats.Dispose();

        if (Main.IsLocked)
        {
            this.wasLockedLastUpdate = true;
        }
    }

    void UpdateFPSDatas()
    {
        ++this.frame;

        if (this.frame >= NbFrameSample)
        {
            var t = this.Time.ElapsedTime - this.lastElapsedTime;

            var averageDT = t / this.frame;
            Stats.FPS = (int)math.round(1f / (averageDT));

            this.frame = 0;
            this.lastElapsedTime = this.Time.ElapsedTime;
        }
    }
}
