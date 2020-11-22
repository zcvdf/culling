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

        var visibleNodeEntity = GetSingletonEntity<VisibleOctreeNode>();
        var visibleClusterEntity = GetSingletonEntity<VisibleOctreeCluster>();
        var visibleNodeCountEntity = GetSingletonEntity<VisibleNodeInClusterCount>();

        var visibleNodes = GetBuffer<VisibleOctreeNode>(visibleNodeEntity).AsNativeArray();
        var visibleClusters = GetBuffer<VisibleOctreeCluster>(visibleClusterEntity).AsNativeArray();
        var visibleNodeCounts = GetBuffer<VisibleNodeInClusterCount>(visibleNodeCountEntity).AsNativeArray();

        var jobsDependency = this.Dependency;

        // This code is fine but triggers job safety checks if they are enabled
        for (int i = 0, srcNodeCountIndex = 0; i < visibleClusters.Length; ++i)
        {
            var visibleCluster = visibleClusters[i];
            var visibleNodeCount = visibleNodeCounts[i].Value;
            var srcIndex = srcNodeCountIndex; // Avoid weird compiler behavior resetting 'srcLeafCountIndex' to 0 if it's taken directly in the lambda

            var jobHandle = this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter<OctreeCluster>(visibleCluster)
            .WithReadOnly(visibleNodes)
            .WithReadOnly(sphereOccluderTranslations)
            .WithReadOnly(sphereOccluderRadiuses)
            .WithReadOnly(planeOccluderTranslations)
            .WithReadOnly(planeOccluderExtents)
            .ForEach((ref EntityCullingResult cullingResult, in Translation translation, in WorldBoundingRadius radiusComponent, in OctreeNode octreeNode) =>
            {
                if (!IsNodeVisible(visibleNodes, octreeNode, srcIndex, visibleNodeCount))
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
            .ScheduleParallel(jobsDependency);

            this.Dependency = JobHandle.CombineDependencies(this.Dependency, jobHandle);

            srcNodeCountIndex += visibleNodeCount;
        }

        Main.VisibleOctreeNodes = visibleNodes.ToArray();
        Main.VisibleOctreeClusters = visibleClusters.ToArray();

        planeOccluderExtents.Dispose(this.Dependency);
        planeOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    public static bool IsNodeVisible(NativeArray<VisibleOctreeNode> visibleNodes, OctreeNode node, int src, int range)
    {
        if (node.Value == Octree.PackedRoot) return true;

        var nodeLayer = Octree.UnpackLayer(node.Value);

        for (int i = src; i < src + range; ++i)
        {
            var visibleNode = visibleNodes[i].Value;
            var visibleNodeLayer = Octree.UnpackLayer(visibleNode);

            if (nodeLayer < visibleNodeLayer)
            {
                return true;
            }    
            else if (nodeLayer == visibleNodeLayer)
            {
                if (visibleNode == node.Value) return true;
            }
            else
            {
                var parentNode = Octree.GetParentNodePackedID(node.Value, visibleNodeLayer);

                if (visibleNode == parentNode) return true;
            }
        }

        return false;
    }
}
