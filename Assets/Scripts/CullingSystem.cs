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
[UpdateAfter(typeof(UpdateEntityOctreeNode))]
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

        UpdateVisibilityBuffers.LastScheduledJob.Complete();

        var visibilityBufferEntity = GetSingletonEntity<VisibilityBuffer>();
        var visibilityBuffer = this.EntityManager.GetComponentData<VisibilityBuffer>(visibilityBufferEntity);

        var visiblityLayer0 = visibilityBuffer.Layer0;
        var visiblityLayer1 = visibilityBuffer.Layer1;

        var jobsDependency = this.Dependency;

        // This code is fine but triggers job safety checks if they are enabled
        foreach (var visibleCluster in visiblityLayer0)
        {
            var jobHandle = this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter(new OctreeCluster { Value = visibleCluster })
            .WithReadOnly(visiblityLayer0)
            .WithReadOnly(visiblityLayer1)
            .WithReadOnly(sphereOccluderTranslations)
            .WithReadOnly(sphereOccluderRadiuses)
            .WithReadOnly(planeOccluderTranslations)
            .WithReadOnly(planeOccluderExtents)
            .ForEach((ref EntityCullingResult cullingResult, in WorldRenderBounds bounds, in WorldBoundingRadius radiusComponent, in OctreeNode octreeNode) =>
            {
                if (!IsNodeVisible(octreeNode, visiblityLayer0, visiblityLayer1))
                {
                    cullingResult.Value = CullingResult.CulledByOctreeNodes;
                    return;
                }

                var boudingCenter = bounds.Value.Center;
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
            .ScheduleParallel(jobsDependency);

            this.Dependency = JobHandle.CombineDependencies(this.Dependency, jobHandle);
        }

        Main.VisibleOctreeClusters = visibilityBuffer.Layer0.ToNativeArray(Allocator.Temp).ToArray();
        Main.VisibleOctreeNodes = visibilityBuffer.Layer1.ToNativeArray(Allocator.Temp).ToArray();

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    public static bool IsNodeVisible(in OctreeNode node, in NativeHashSet<ulong> layer0, in NativeHashSet<ulong> layer1)
    {
        if (node.Value == Octree.PackedRoot) return true;

        var nodeLayer = Octree.UnpackLayer(node.Value);

        if (nodeLayer == 0)
        {
            return layer0.Contains(node.Value);
        }
        else if (nodeLayer == 1)
        {
            return layer1.Contains(node.Value);
        }

        return false;
    }
}
