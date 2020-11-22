using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using System;
using Unity.Rendering;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateEntityOctreeLeaf : SystemBase
{
    protected override void OnUpdate()
    {
        this.Entities
        .WithChangeFilter<WorldRenderBounds>()
        .ForEach((ref OctreeLeaf octreeLeaf, in WorldRenderBounds boundsComponent) =>
        {
            var bounds = boundsComponent.Value;

            var newID = ComputeOctreeNodeID(bounds);

            octreeLeaf = new OctreeLeaf
            {
                Value = Octree.PackID(newID)
            };
        })
        .ScheduleParallel();
    }

    public static int4 ComputeOctreeNodeID(in AABB bounds)
    {
        var leafID = Octree.PointToILeafID(bounds.Center);

        var leafAABB = new AABB();
        leafAABB.Center = Octree.LeafIDToPoint(leafID.xyz);
        leafAABB.Extents = new float3(Octree.LeafExtent);

        if (leafAABB.Contains(bounds))
        {
            return leafID;
        }
        else
        {
            var layer = Octree.LeafLayer - 1;
            while (layer >= 0)
            {
                var parentID = Octree.GetLeafParentNodeID(leafID.xyz, layer);

                var parentAABB = new AABB();
                parentAABB.Center = Octree.NodeIDToPoint(parentID);
                parentAABB.Extents = new float3(Octree.NodeExtent(layer));

                if (parentAABB.Contains(bounds))
                {
                    return parentID;
                }

                --layer;
            }

            return Octree.Root;
        }
    }
}