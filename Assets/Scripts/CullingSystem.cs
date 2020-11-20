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
[UpdateAfter(typeof(UpdateVisibleOctreeIDs))]
[UpdateAfter(typeof(TransformSystemGroup))]
public class CullingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var viewer = Main.Viewer;
        var nearPlane = Main.NearPlane;
        var nearPlaneCenter = Main.NearPlaneCenter;
        var worldToNDC = Main.WorldToNDC;
        var entityInFrumstrumColor = Main.EntityInFrustrumColor;
        var entityOccludedColor = Main.EntityOccludedColor;
        var frustrumAABB = Main.FrustrumAABB;

        var sphereOccluderQuery = GetEntityQuery(typeof(WorldOccluderRadius), typeof(Translation));
        var planeOccluderQuery = GetEntityQuery(typeof(WorldOccluderExtents), typeof(Translation));

        var frustrumPlanes = Main.FrustrumPlanes;
        var sphereOccluderTranslations = sphereOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var sphereOccluderRadiuses = sphereOccluderQuery.ToComponentDataArray<WorldOccluderRadius>(Allocator.TempJob);

        var planeOccluderTranslations = planeOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var planeOccluderExtents = planeOccluderQuery.ToComponentDataArray<WorldOccluderExtents>(Allocator.TempJob);

        var visibleOctreeEntity = GetSingletonEntity<VisibleOctreeID>();
        var visibleClusterEntity = GetSingletonEntity<VisibleClusterID>();
        var visibleLeafEntity = GetSingletonEntity<VisibleLeafInClusterCount>();

        var visibleOctreeIDs = GetBuffer<VisibleOctreeID>(visibleOctreeEntity).AsNativeArray();
        var visibleClusterIDs = GetBuffer<VisibleClusterID>(visibleClusterEntity).AsNativeArray();
        var visibleLeafCounts = GetBuffer<VisibleLeafInClusterCount>(visibleLeafEntity).AsNativeArray();

        Main.VisibleOctreeIDs = visibleOctreeIDs.ToArray();

        for (int i = 0, srcLeafCountIndex = 0; i < visibleClusterIDs.Length; ++i)
        {
            var visibleClusterID = visibleClusterIDs[i].Value;
            var visibleLeafCount = visibleLeafCounts[i].Value;
            var srcIndex = srcLeafCountIndex; // Avoid weird compiler behavior resetting 'srcLeafCountIndex' to 0 if it's taken directly in the lambda

            this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter(visibleClusterID)
            .WithReadOnly(visibleOctreeIDs)
            .WithReadOnly(sphereOccluderTranslations)
            .WithReadOnly(sphereOccluderRadiuses)
            .WithReadOnly(planeOccluderTranslations)
            .WithReadOnly(planeOccluderExtents)
            .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radiusComponent, in OctreeLeaf octreeLeaf) =>
            {
                if (!Contains(visibleOctreeIDs, octreeLeaf, srcIndex, visibleLeafCount)) return;

                var center = translation.Value;
                var radius = radiusComponent.Value;

                if (!Math.IsInFrustrum(center, radius, frustrumPlanes)) return;

                var isSphereOccluded =
                    Math.IsOccludedBySphere(center, radius, viewer, sphereOccluderTranslations, sphereOccluderRadiuses, frustrumPlanes)
                    || Math.IsOccludedByPlane(center, radius, viewer, nearPlane, planeOccluderTranslations, planeOccluderExtents);

                color.Value = isSphereOccluded ? entityOccludedColor : entityInFrumstrumColor;
            })
            .ScheduleParallel();

            srcLeafCountIndex += visibleLeafCount;
        }

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    public static bool Contains(NativeArray<VisibleOctreeID> visibleIDs, OctreeLeaf leaf, int src, int range)
    {
        for (int i = 0; i < range; ++i)
        {
            var a = visibleIDs[src + i].Value;
            var b = leaf;

            if (a.Value == b.Value) return true;
        }

        return false;
    }
}
