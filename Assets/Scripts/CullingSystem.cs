using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

[UpdateAfter(typeof(UpdateEntityOctreeNode))]
[UpdateAfter(typeof(UpdateVisibleSets))]
[UpdateAfter(typeof(TransformSystemGroup))]
public class CullingSystem : SystemBase
{
    struct GlobalCullingInput
    {
        [ReadOnly] public AABB FrustrumAABB;
        [ReadOnly] public WorldFrustrumPlanes FrustrumPlanes;
        [ReadOnly] public VisibleSets VisibleSets;
        [ReadOnly] public NativeArray<Translation> SphereOccluderTranslations;
        [ReadOnly] public NativeArray<WorldOccluderRadius> SphereOccluderRadiuses;
        [ReadOnly] public NativeArray<Translation> QuadOccluderTranslations;
        [ReadOnly] public NativeArray<WorldOccluderExtents> QuadOccluderExtents;
        [ReadOnly] public float3 Viewer;
        [ReadOnly] public Quad NearPlane;
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

        UpdateVisibleSets.LastScheduledJob.Complete();

        var visibilityBufferEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibilityBufferEntity).Value;

        var globalInputs = new GlobalCullingInput
        {
            FrustrumAABB = frustrumAABB,
            FrustrumPlanes = frustrumPlanes,
            Viewer = viewer,
            SphereOccluderRadiuses = sphereOccluderRadiuses,
            SphereOccluderTranslations = sphereOccluderTranslations,
            QuadOccluderExtents = quadOccluderExtents,
            QuadOccluderTranslations = quadOccluderTranslations,
            VisibleSets = visibleSets,
            NearPlane = nearPlane,
        };

        this.Entities
        .WithAll<EntityTag>()
        .WithReadOnly(globalInputs)
        .ForEach((ref EntityCullingResult cullingResult, in WorldRenderBounds bounds, in OctreeNode octreeNode) =>
        {
            cullingResult.Value = ProcessVisibleCluster(globalInputs, bounds, octreeNode);
        })
        .ScheduleParallel();

        Main.VisibleOctreeNodes = visibleSets.RawIDs;

        quadOccluderExtents.Dispose(this.Dependency);
        quadOccluderTranslations.Dispose(this.Dependency);
        sphereOccluderRadiuses.Dispose(this.Dependency);
        sphereOccluderTranslations.Dispose(this.Dependency);
    }

    static CullingResult ProcessVisibleCluster(in GlobalCullingInput global, 
        in WorldRenderBounds bounds, in OctreeNode octreeNode) 
    {
        if (!IsNodeVisible(octreeNode, global.VisibleSets))
        {
            return CullingResult.CulledByOctreeNodes;
        }

        if (!Math.IsInFrustrum(bounds.Value, global.FrustrumPlanes))
        {
            return CullingResult.CulledByFrustrumPlanes;
        }

        if (Math.IsOccludedBySphere(bounds.Value, global.Viewer,
            global.SphereOccluderTranslations, global.SphereOccluderRadiuses, global.FrustrumPlanes))
        {
            return CullingResult.CulledBySphereOccluder;
        }

        if (Math.IsOccludedByPlane(bounds.Value, global.Viewer,
            global.NearPlane, global.QuadOccluderTranslations, global.QuadOccluderExtents))
        {
            return CullingResult.CulledByQuadOccluder;
        }

        return CullingResult.NotCulled;
    }

    public static bool IsNodeVisible(in OctreeNode node, in VisibleSets visibleSets)
    {
        if (node.Value == Octree.PackedRoot) return true;

        var nodeLayer = Octree.UnpackLayer(node.Value);

        return visibleSets[nodeLayer].Contains(node.Value);
    }
}
