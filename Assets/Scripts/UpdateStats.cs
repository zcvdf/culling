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
    double lastElapsedTime;
    int frame;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<VisibleSetsComponent>();
    }

    protected override void OnStartRunning()
    {
        this.lastElapsedTime = this.Time.ElapsedTime;
    }

    protected override void OnUpdate()
    {
        UpdateFPSDatas();

        if (Stats.Details == StatsDetails.None) return;

        UpdateVisibleSets.LastScheduledJob.Complete();

        var visibleSetsEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibleSetsEntity).Value;

        Stats.VisibleOctreeClusters = visibleSets.ClusterLayer.Count();
        Stats.VisibleOctreeLeafs = visibleSets.LeafLayer.Count();

        if (Stats.Details == StatsDetails.Normal) return;

        var stats = new NativeArray<int>(6, Allocator.TempJob);

        this.Entities
        .WithAll<EntityTag>()
        .WithNativeDisableParallelForRestriction(stats)
        .ForEach((in EntityCullingResult result, in OctreeNode octreeNode) =>
        {
            var id = (int)result.Value;
            ++stats[id];

            if (octreeNode.Value == Octree.PackedRoot)
            {
                ++stats[5];
            }
        })
        .Run();

        var notCulled = stats[0];
        var culledByOctreeNodes = stats[1];
        var culledByFrustrumPlanes = stats[2];
        var culledBySphereOccluder = stats[3];
        var culledByQuadOccluder = stats[4];
        var atRootOctreeLayer = stats[5];

        Stats.CulledByOctreeNodes = culledByOctreeNodes;
        Stats.CulledByFrustrumPlanes = culledByFrustrumPlanes;
        Stats.CulledByQuadOccluders = culledByQuadOccluder;
        Stats.CulledBySphereOccluders = culledBySphereOccluder;
        Stats.AtRootOctreeLayer = atRootOctreeLayer;

        stats.Dispose();
    }

    void UpdateFPSDatas()
    {
        const int NbFrameSample = 10;

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
