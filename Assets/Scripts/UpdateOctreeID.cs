using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using System;

public struct OctreeID : ISharedComponentData
{
    public int3 ID0;
}

public class Octree
{
    public static readonly OctreeID RootID = new OctreeID 
    {
        ID0 = new int3(int.MaxValue),
    };

    public const float Node0Extent = 200f;
    public const float Node0Size = Node0Extent * 2f;
    public const float Node0BoundingRadius = Node0Extent * Const.SQRT3;

    public const float Node1Extent = 100f;
    public const float Node1Size = Node1Extent * 2f;
    public const float Node1BoundingRadius = Node1Extent * Const.SQRT3;

    public static int3 PointToIDLayer0(float3 point)
    {
        return new int3(math.floor(point / Node0Size));
    }

    public static float3 IDLayer0ToPoint(int3 id)
    {
        return new float3(id) * new float3(Node0Size) + new float3(Node0Extent);
    }

    private static void ForEach3DGeneric(int3 minID, int3 maxID, Action<int3> func)
    {
        for (int x = minID.x; x <= maxID.x; ++x)
        {
            for (int y = minID.y; y <= maxID.y; ++y)
            {
                for (int z = minID.z; z <= maxID.z; ++z)
                {
                    var id = new int3(x, y, z);
                    func(id);
                }
            }
        }
    }

    public static void ForEachNode0(float3 min, float3 max, Action<int3> func)
    {
        var minID = PointToIDLayer0(min);
        var maxID = PointToIDLayer0(max);

        ForEach3DGeneric(minID, maxID, func);
    }

    public static void ForEachBoundingNode0(in AABB aabb, Action<int3> func)
    {
        ForEachNode0(aabb.Min, aabb.Max, func);
    }



    public static int3 PointToIDLayer1(float3 point)
    {
        return new int3(math.floor(point / Node1Size));
    }

    public static float3 IDLayer1ToPoint(int3 id)
    {
        return new float3(id) * new float3(Node1Size) + new float3(Node1Extent);
    }

    public static void ForEachNode1(float3 min, float3 max, Action<int3> func)
    {
        var minID = PointToIDLayer1(min);
        var maxID = PointToIDLayer1(max);

        ForEach3DGeneric(minID, maxID, func);
    }

    public static void ForEachBoundingNode1(in AABB aabb, Action<int3> func)
    {
        ForEachNode1(aabb.Min, aabb.Max, func);
    }
}

[UpdateBefore(typeof(CullingSystem))]
public class UpdateOctreeID : SystemBase
{
    protected override void OnUpdate()
    {
        var cmd = new EntityCommandBuffer(Allocator.Temp);

        this.Entities
        .WithChangeFilter<Translation>()
        .WithoutBurst()
        .ForEach((in OctreeID id, in Translation translation, in Entity entity) =>
        {
            var id0 = Octree.PointToIDLayer0(translation.Value);
            var id1 = Octree.PointToIDLayer1(translation.Value);

            var newID = new OctreeID 
            { 
                ID0 = id0,
            };

            cmd.SetSharedComponent(entity, newID);
        })
        .Run();

        cmd.Playback(this.EntityManager);
    }
}
