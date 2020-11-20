using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Octree
{
    public const int Depth = 1;

    public const float ClusterExtent = 200f;
    public const float ClusterSize = ClusterExtent * 2f;

    public const float LeafExtent = ClusterExtent / (Depth + 1);
    public const float LeafSize = LeafExtent * 2f;

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

    public static int3 PointToClusterID(float3 point)
    {
        return new int3(math.floor(point / ClusterSize));
    }

    public static float3 ClusterIDToPoint(int3 clusterID)
    {
        return new float3(clusterID) * new float3(ClusterSize) + new float3(ClusterExtent);
    }

    public static void GetMinMaxClusterIDs(in AABB aabb, out int3 minClusterID, out int3 maxClusterID)
    {
        minClusterID = PointToClusterID(aabb.Min);
        maxClusterID = PointToClusterID(aabb.Max);
    }

    public static int3 PointToILeafID(float3 point)
    {
        return new int3(math.floor(point / LeafSize));
    }

    public static float3 LeafIDToPoint(int3 leafID)
    {
        return new float3(leafID) * new float3(LeafSize) + new float3(LeafExtent);
    }

    // Subdivide node in 8 children
    public static void GetMinMaxNodeChildrenID(int3 nodeID, out int3 minChildrenID, out int3 maxChildrenID)
    {
        minChildrenID = nodeID << 1;
        maxChildrenID = minChildrenID + new int3(2);
    }

    private static void AssertValidPackedField(UInt64 x, UInt64 y, UInt64 z)
    {
        Debug.Assert(x < MaxPackedField);
        Debug.Assert(y < MaxPackedField);
        Debug.Assert(z < MaxPackedField);
    }
}
