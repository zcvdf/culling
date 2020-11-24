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
    struct GlobalCullingInput
    {
        [ReadOnly] public WorldFrustrumPlanes FrustrumPlanes;
        [ReadOnly] public VisibleSets VisibleSets;
        [ReadOnly] public NativeArray<Translation> SphereOccluderTranslations;
        [ReadOnly] public NativeArray<WorldOccluderRadius> SphereOccluderRadiuses;
        [ReadOnly] public NativeArray<Translation> QuadOccluderTranslations;
        [ReadOnly] public NativeArray<WorldOccluderExtents> QuadOccluderExtents;
        [ReadOnly] public float3 Viewer;
        [ReadOnly] public Quad NearPlane;
    }

    struct PerEntityCullingInput
    {
        [ReadOnly] public WorldRenderBounds Bounds; 
        [ReadOnly] public WorldBoundingRadius Radius;
        [ReadOnly] public OctreeNode OtreeNode;
    }

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<VisibleSetsComponent>();
    }

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

        var quadOccluderTranslations = planeOccluderQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var quadOccluderExtents = planeOccluderQuery.ToComponentDataArray<WorldOccluderExtents>(Allocator.TempJob);

        UpdateVisibilityBuffers.LastScheduledJob.Complete();

        var visibilityBufferEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibilityBufferEntity).Value;

        var globalInputs = new GlobalCullingInput
        {
            FrustrumPlanes = frustrumPlanes,
            Viewer = viewer,
            SphereOccluderRadiuses = sphereOccluderRadiuses,
            SphereOccluderTranslations = sphereOccluderTranslations,
            QuadOccluderExtents = quadOccluderExtents,
            QuadOccluderTranslations = quadOccluderTranslations,
            VisibleSets = visibleSets,
            NearPlane = nearPlane,
        };

        var jobsDependency = this.Dependency;

        // This code is fine but triggers job safety checks if they are enabled
        foreach (var visibleCluster in visibleSets[0])
        {
            var clusterJobHandle = this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter(new OctreeCluster { Value = visibleCluster })
            .WithReadOnly(globalInputs)
            .ForEach((ref EntityCullingResult cullingResult, in WorldRenderBounds bounds, in WorldBoundingRadius radius, in OctreeNode octreeNode) =>
            {
                ProcessVisibleCluster(ref cullingResult, globalInputs, bounds, radius, octreeNode);
            })
            .ScheduleParallel(jobsDependency);

            this.Dependency = JobHandle.CombineDependencies(this.Dependency, clusterJobHandle);
        }

        // Process Octree root node
        var rootJobHandle = this.Entities
        .WithAll<EntityTag>()
        .WithSharedComponentFilter(new OctreeCluster { Value = Octree.PackedRoot })
        .WithReadOnly(globalInputs)
        .ForEach((ref EntityCullingResult cullingResult, in WorldRenderBounds bounds, in WorldBoundingRadius radius, in OctreeNode octreeNode) =>
        {
            ProcessOctreeRoot(ref cullingResult, globalInputs, bounds, radius, octreeNode);
        })
        .ScheduleParallel(jobsDependency);

        this.Dependency = JobHandle.CombineDependencies(this.Dependency, rootJobHandle);

        Main.VisibleOctreeNodes = visibleSets.RawIDs;

        quadOccluderExtents.Dispose(this.Dependency);
        quadOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    static void ProcessVisibleCluster(ref EntityCullingResult result, in GlobalCullingInput global, 
        in WorldRenderBounds bounds, in WorldBoundingRadius radius, in OctreeNode octreeNode) 
    {
        if (!IsNodeVisible(octreeNode, global.VisibleSets))
        {
            result.Value = CullingResult.CulledByOctreeNodes;
            return;
        }

        var entityInputs = new PerEntityCullingInput
        {
            Bounds = bounds,
            OtreeNode = octreeNode,
            Radius = radius
        };

        PostOctreeCulling(ref result, in entityInputs, in global);
    }

    static void ProcessOctreeRoot(ref EntityCullingResult result, in GlobalCullingInput global,
        in WorldRenderBounds bounds, in WorldBoundingRadius radius, in OctreeNode octreeNode)
    {
        var entityInputs = new PerEntityCullingInput
        {
            Bounds = bounds,
            OtreeNode = octreeNode,
            Radius = radius
        };

        PostOctreeCulling(ref result, in entityInputs, in global);
    }

    static void PostOctreeCulling(ref EntityCullingResult result, in PerEntityCullingInput entity, in GlobalCullingInput global)
    {
        var boudingCenter = entity.Bounds.Value.Center;
        var boundingRadius = entity.Radius.Value;

        if (!Math.IsInFrustrum(boudingCenter, boundingRadius, global.FrustrumPlanes))
        {
            result.Value = CullingResult.CulledByFrustrumPlanes;
            return;
        }

        if (Math.IsOccludedBySphere(boudingCenter, boundingRadius, global.Viewer, 
            global.SphereOccluderTranslations, global.SphereOccluderRadiuses, global.FrustrumPlanes))
        {
            result.Value = CullingResult.CulledBySphereOccluder;
            return;
        }

        if (Math.IsOccludedByPlane(boudingCenter, boundingRadius, global.Viewer, 
            global.NearPlane, global.QuadOccluderTranslations, global.QuadOccluderExtents))
        {
            result.Value = CullingResult.CulledByQuadOccluder;
            return;
        }

        result.Value = CullingResult.NotCulled;
    }

    public static bool IsNodeVisible(in OctreeNode node, in VisibleSets visibleSets)
    {
        var nodeLayer = Octree.UnpackLayer(node.Value);

        return visibleSets[nodeLayer].Contains(node.Value);
    }
}
