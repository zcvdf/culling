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
[UpdateAfter(typeof(UpdateOctreeID))]
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
        var visibleOctreeIDs = GetBuffer<VisibleOctreeID>(visibleOctreeEntity).AsNativeArray();

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(visibleOctreeIDs)
        .WithReadOnly(sphereOccluderTranslations)
        .WithReadOnly(sphereOccluderRadiuses)
        .WithReadOnly(planeOccluderTranslations)
        .WithReadOnly(planeOccluderExtents)
        .ForEach((ref URPMaterialPropertyBaseColor color, in Translation translation, in WorldBoundingRadius radiusComponent, in OctreeID octreeID) =>
        {
            if (!Contains(visibleOctreeIDs, octreeID)) return;

            var center = translation.Value;
            var radius = radiusComponent.Value;

            if (!Math.IsInFrustrum(center, radius, frustrumPlanes)) return;

            var isSphereOccluded =
                Math.IsOccludedBySphere(center, radius, viewer, sphereOccluderTranslations, sphereOccluderRadiuses, frustrumPlanes)
                || Math.IsOccludedByPlane(center, radius, viewer, nearPlane, planeOccluderTranslations, planeOccluderExtents);

            color.Value = isSphereOccluded ? entityOccludedColor : entityInFrumstrumColor;
        })
        .ScheduleParallel();

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    public static bool Contains(NativeArray<VisibleOctreeID> visibleIDs, OctreeID id)
    {
        for (int i = 0; i < visibleIDs.Length; ++i)
        {
            var a = visibleIDs[i].Value;
            var b = id;

            if (math.all(a.Value == b.Value)) return true;
        }

        return false;
    }
}
