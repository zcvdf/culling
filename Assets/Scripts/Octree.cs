using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class Octree
{
    public const int Depth = 2;

    public const float ClusterExtent = 200f;
    public const float ClusterSize = ClusterExtent * 2f;

    public const int ClusterSubdivisions = (1 << Depth);
    public const float LeafExtent = ClusterExtent / ClusterSubdivisions;
    public const float LeafSize = LeafExtent * 2f;
    public const int MaxLayer = 1 << 4;
    public const int MaxPosition = 1 << 20;

    const Int64 PositionPackOffset = MaxPosition >> 1;
    const UInt64 LayerPackMask = MaxLayer - 1;
    const UInt64 PositionPackMask = MaxPosition - 1;

    public static UInt64 PackID(int3 id, int layer = 0)
    {
        var x64 = id.x + PositionPackOffset;
        var y64 = id.y + PositionPackOffset;
        var z64 = id.z + PositionPackOffset;

        var ux = (UInt64)x64;
        var uy = (UInt64)y64;
        var uz = (UInt64)z64;
        var ul = (UInt64)layer;

        AssertValidPackedField(ux, uy, uz, ul);
        var packed = ux | (uy << 20) | (uz << 40) | (ul << 60);

        return packed;
    }

    public static int3 UnpackID(UInt64 id)
    {
        var ux = (UInt64)(id & PositionPackMask);
        var uy = (UInt64)((id >> 20) & PositionPackMask);
        var uz = (UInt64)((id >> 40) & PositionPackMask);
        var ul = (UInt64)((id >> 60) & LayerPackMask);

        AssertValidPackedField(ux, uy, uz, ul);

        var x64 = (Int64)ux - PositionPackOffset; 
        var y64 = (Int64)uy - PositionPackOffset; 
        var z64 = (Int64)uz - PositionPackOffset; 

        var unpacked = new int3((int)x64, (int)y64, (int)z64);

        return unpacked;
    }

    public static float NodeExtent(int depth)
    {
        return ClusterExtent / (1 << depth);
    }

    public static float NodeSize(int depth)
    {
        return NodeExtent(depth) * 2f;
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
        maxClusterID = PointToClusterID(aabb.Max) + new int3(1);
    }

    public static int3 PointToILeafID(float3 point)
    {
        return new int3(math.floor(point / LeafSize));
    }

    public static float3 LeafIDToPoint(int3 leafID)
    {
        return new float3(leafID) * new float3(LeafSize) + new float3(LeafExtent);
    }

    public static int3 PointToNode(float3 point, int depth)
    {
        return new int3(math.floor(point / NodeSize(depth)));
    }

    public static float3 NodeIDToPoint(int3 nodeID, int depth)
    {
        var nodeExtent = NodeExtent(depth);
        var nodeSize = nodeExtent * 2f;

        return new float3(nodeID) * nodeSize + nodeExtent;
    }

    // Subdivide node in 8 children
    public static void GetMinMaxNodeChildrenID(int3 nodeID, out int3 minChildrenID, out int3 maxChildrenID)
    {
        minChildrenID = nodeID << 1;
        maxChildrenID = minChildrenID + new int3(2);
    }

    // Subdivide cluster in leafs
    public static void GetMinMaxClusterNodeIDs(int3 clusterID, int depth, out int3 minNodeID, out int3 maxNodeID)
    {
        minNodeID = clusterID << depth;
        maxNodeID = minNodeID + new int3(1 << depth);
    }

    public static int3 GetLeafParentNodeID(int3 leafID, int parentDepth)
    {
        var rshift = Depth - parentDepth;
        return leafID >> rshift;
    }

    private static void AssertValidPackedField(UInt64 x, UInt64 y, UInt64 z, UInt64 l)
    {
#if ENABLE_ASSERTS
        Debug.Assert(x < MaxPosition);
        Debug.Assert(y < MaxPosition);
        Debug.Assert(z < MaxPosition);
        Debug.Assert(l < MaxLayer);
#endif
    }
}
