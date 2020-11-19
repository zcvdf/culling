using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Octree
{
    public const float Node0Extent = 200f;
    public const float Node0Size = Node0Extent * 2f;
    public const float Node0BoundingRadius = Node0Extent * Math.Sqrt3;
    public const int Node0Subdivision = 4;

    public const float Node1Extent = Node0Extent / Node0Subdivision;
    public const float Node1Size = Node1Extent * 2f;
    public const float Node1BoundingRadius = Node1Extent * Math.Sqrt3;

    const Int64 BitPackOffset = 1 << 20;
    const UInt64 BitPackMask = (1 << 21) - 1;
    const UInt64 MaxPackedField = 1 << 21;

    public static UInt64 PackID(int3 id)
    {
        var x64 = id.x + BitPackOffset;
        var y64 = id.y + BitPackOffset;
        var z64 = id.z + BitPackOffset;

        var ux = (UInt64)x64;
        var uy = (UInt64)y64;
        var uz = (UInt64)z64;

#if ENABLE_ASSERTS
        AssertValidPackedField(ux, uy, uz);
#endif
        var packed = ux | (uy << 21) | (uz << 42);

        return packed;
    }

    public static int3 UnpackID(UInt64 id)
    {
        var ux = (UInt64)(id & BitPackMask);
        var uy = (UInt64)((id >> 21) & BitPackMask);
        var uz = (UInt64)((id >> 42) & BitPackMask);

#if ENABLE_ASSERTS
        AssertValidPackedField(ux, uy, uz);
#endif

        var x64 = (Int64)ux - BitPackOffset; 
        var y64 = (Int64)uy - BitPackOffset; 
        var z64 = (Int64)uz - BitPackOffset; 

        var unpacked = new int3((int)x64, (int)y64, (int)z64);

        return unpacked;
    }

    public static int3 PointToIDLayer0(float3 point)
    {
        return new int3(math.floor(point / Node0Size));
    }

    public static float3 IDLayer0ToPoint(int3 id)
    {
        return new float3(id) * new float3(Node0Size) + new float3(Node0Extent);
    }

    public static void ForEachNode0(float3 min, float3 max, Action<int3> func)
    {
        var minID = PointToIDLayer0(min);
        var maxID = PointToIDLayer0(max);

        ForEach3DMaxIncluded(minID, maxID, func);
    }

    public static void GetMixMaxIDLayer0(in AABB aabb, out int3 minID, out int3 maxID)
    {
        minID = PointToIDLayer0(aabb.Min);
        maxID = PointToIDLayer0(aabb.Max);
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

        ForEach3DMaxIncluded(minID, maxID, func);
    }

    public static void ForEachBoundingNode1(in AABB aabb, Action<int3> func)
    {
        ForEachNode1(aabb.Min, aabb.Max, func);
    }

    public static void GetMixMaxIDChild0(int3 id0, out int3 minID1, out int3 maxID1)
    {
        minID1 = id0 * Node0Subdivision;
        maxID1 = minID1 + new int3(Node0Subdivision);
    }

    public static void ForEachNode0Childs(int3 id0, Action<int3> func)
    {
        int3 minID1;
        int3 maxID1;
        GetMixMaxIDChild0(id0, out minID1, out maxID1);

        ForEach3DMaxExcluded(minID1, maxID1, func);
    }

    private static void ForEach3DMaxIncluded(int3 minID, int3 maxID, Action<int3> func)
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

    private static void ForEach3DMaxExcluded(int3 minID, int3 maxID, Action<int3> func)
    {
        for (int x = minID.x; x < maxID.x; ++x)
        {
            for (int y = minID.y; y < maxID.y; ++y)
            {
                for (int z = minID.z; z < maxID.z; ++z)
                {
                    var id = new int3(x, y, z);
                    func(id);
                }
            }
        }
    }

    private static void AssertValidPackedField(UInt64 x, UInt64 y, UInt64 z)
    {
        Debug.Assert(x < MaxPackedField);
        Debug.Assert(y < MaxPackedField);
        Debug.Assert(z < MaxPackedField);
    }
}
