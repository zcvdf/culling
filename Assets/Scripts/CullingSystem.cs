using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

[UpdateAfter(typeof(UpdateWorldBoundingRadiusSystem))]
[UpdateAfter(typeof(UpdateEntityOctreeCluster))]
[UpdateAfter(typeof(UpdateEntityOctreeLeaf))]
[UpdateAfter(typeof(UpdateVisibilityBuffers))]
[UpdateAfter(typeof(TransformSystemGroup))]
public class CullingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var viewer = Main.Viewer;
        var nearPlane = Main.NearPlane;
        var nearPlaneCenter = Main.NearPlaneCenter;
        var worldToNDC = Main.WorldToNDC;
        var frustrumAABB = Main.FrustrumAABB;

        var sphereOccluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));
        var planeOccluderQuery = GetEntityQuery(typeof(WorldOccluderExtents), typeof(Translation));

        var frustrumPlanes = Main.FrustrumPlanes;
        var sphereOccluderTranslations = sphereOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var sphereOccluderRadiuses = sphereOccluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        var planeOccluderTranslations = planeOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var planeOccluderExtents = planeOccluderQuery.ToComponentDataArray<WorldOccluderExtents>(Allocator.TempJob);

        var visibleOctreeEntity = GetSingletonEntity<VisibleOctreeLeaf>();
        var visibleClusterEntity = GetSingletonEntity<VisibleOctreeCluster>();
        var visibleLeafEntity = GetSingletonEntity<VisibleLeafInClusterCount>();

        var visibleLeafs = GetBuffer<VisibleOctreeLeaf>(visibleOctreeEntity).AsNativeArray();
        var visibleClusters = GetBuffer<VisibleOctreeCluster>(visibleClusterEntity).AsNativeArray();
        var visibleLeafCounts = GetBuffer<VisibleLeafInClusterCount>(visibleLeafEntity).AsNativeArray();

        Main.VisibleOctreeLeafs = visibleLeafs.ToArray();
        Main.VisibleOctreeClusters = visibleClusters.ToArray();

        for (int i = 0, srcLeafCountIndex = 0; i < visibleClusters.Length; ++i)
        {
            var visibleCluster = visibleClusters[i];
            var visibleLeafCount = visibleLeafCounts[i].Value;
            var srcIndex = srcLeafCountIndex; // Avoid weird compiler behavior resetting 'srcLeafCountIndex' to 0 if it's taken directly in the lambda

            this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter<OctreeCluster>(visibleCluster)
            .WithReadOnly(visibleLeafs)
            .WithReadOnly(sphereOccluderTranslations)
            .WithReadOnly(sphereOccluderRadiuses)
            .WithReadOnly(planeOccluderTranslations)
            .WithReadOnly(planeOccluderExtents)
            .ForEach((ref EntityCullingResult cullingResult, in Translation translation, in WorldBoundingRadius radiusComponent, in OctreeLeaf octreeLeaf) =>
            {
                if (!Contains(visibleLeafs, octreeLeaf, srcIndex, visibleLeafCount))
                {
                    cullingResult.Value = CullingResult.CulledByOctreeNodes;
                    return;
                }

                var boudingCenter = translation.Value;
                var boundingRadius = radiusComponent.Value;

                if (!Math.IsInFrustrum(boudingCenter, boundingRadius, frustrumPlanes))
                {
                    cullingResult.Value = CullingResult.CulledByFrustrumPlanes;
                    return;
                }

                if (Math.IsOccludedBySphere(boudingCenter, boundingRadius, viewer, sphereOccluderTranslations, sphereOccluderRadiuses, frustrumPlanes))
                {
                    cullingResult.Value = CullingResult.CulledBySphereOccluder;
                    return;
                }

                if (Math.IsOccludedByPlane(boudingCenter, boundingRadius, viewer, nearPlane, planeOccluderTranslations, planeOccluderExtents))
                {
                    cullingResult.Value = CullingResult.CulledByQuadOccluder;
                    return;
                }

                cullingResult.Value = CullingResult.NotCulled;
            })
            .ScheduleParallel();

            srcLeafCountIndex += visibleLeafCount;
        }

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    public static bool Contains(NativeArray<VisibleOctreeLeaf> visibleLeafs, OctreeLeaf leaf, int src, int range)
    {
        for (int i = 0; i < range; ++i)
        {
            var a = visibleLeafs[src + i].Value;
            var b = leaf.Value;

            if (a == b) return true;
        }

        return false;
    }
}
