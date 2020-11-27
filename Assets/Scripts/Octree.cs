using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class Octree
{
    public const int MaxLayer = 1 << 4;
    public const int MaxPosition = 1 << 20;

    public const int ClusterLayer = 0;
    public const int LeafLayer = 3;
    public const int RootLayer = MaxLayer - 1;
    public static readonly int4 Root = new int4(0, 0, 0, RootLayer);
    public static readonly UInt64 PackedRoot = PackID(Root);

    public const int ClusterAdditionalDivision = 0;

    public const float ClusterExtent = 2500;
    public const float ClusterSize = ClusterExtent * 2f;
    public const int ClusterSubdivisions = (1 << (LeafLayer + ClusterAdditionalDivision));

    public const float LeafExtent = ClusterExtent / ClusterSubdivisions;
    public const float LeafSize = LeafExtent * 2f;

    const Int64 PositionPackOffset = MaxPosition >> 1;
    const UInt64 LayerPackMask = MaxLayer - 1;
    const UInt64 PositionPackMask = MaxPosition - 1;

    const UInt64 XPackMask = (1 << 20) - 1;
    const UInt64 YPackMask = ((1 << 40) - 1) ^ XPackMask;
    const UInt64 ZPackMask = ((1 << 60) - 1) ^ (XPackMask | YPackMask);

    public static UInt64 PackID(int4 id)
    {
        var l64 = id.w;
        var ul = (UInt64)l64;
        AssertValidPackedLayer(ul);

        var packedXYZ = PackXYZ(id.xyz);
        var packed = packedXYZ | (ul << 60);

        return packed;
    }

    public static UInt64 PackXYZ(int3 id)
    {
        var x64 = id.x + PositionPackOffset;
        var y64 = id.y + PositionPackOffset;
        var z64 = id.z + PositionPackOffset;

        var ux = (UInt64)x64;
        var uy = (UInt64)y64;
        var uz = (UInt64)z64;

        AssertValidPackedField(ux, uy, uz, 0);

        var packed = ux | (uy << 20) | (uz << 40);

        return packed;
    }

    public static int4 UnpackID(UInt64 id)
    {
        var x = UnpackX(id);
        var y = UnpackY(id);
        var z = UnpackZ(id);
        var l = UnpackLayer(id);

        return new int4(x, y, z, l);
    }

    public static int3 UnpackXYZ(UInt64 id)
    {
        var x = UnpackX(id);
        var y = UnpackY(id);
        var z = UnpackZ(id);

        return new int3(x, y, z);
    }

    public static int UnpackX(UInt64 id)
    {
        var ux = (UInt64)(id & PositionPackMask);
        AssertValidPackedPosition(ux);

        var x64 = (Int64)ux - PositionPackOffset;
        return (int)x64;
    }

    public static int UnpackY(UInt64 id)
    {
        var uy = (UInt64)((id >> 20) & PositionPackMask);
        AssertValidPackedPosition(uy);

        var y64 = (Int64)uy - PositionPackOffset;
        return (int)y64;
    }

    public static int UnpackZ(UInt64 id)
    {
        var uz = (UInt64)((id >> 40) & PositionPackMask);
        AssertValidPackedPosition(uz);

        var z64 = (Int64)uz - PositionPackOffset;
        return (int)z64;
    }

    public static int UnpackLayer(UInt64 id)
    {
        var ul = (UInt64)((id >> 60) & LayerPackMask);
        AssertValidPackedLayer(ul);

        return (int)ul;
    }

    public static float NodeExtent(int layer)
    {
        if (layer == ClusterLayer) return ClusterExtent;

        return ClusterExtent / (1 << (layer + ClusterAdditionalDivision));
    }

    public static float NodeSize(int layer)
    {
        return NodeExtent(layer) * 2f;
    }

    public static int4 PointToClusterID(float3 point)
    {
        var posID = new int3(math.floor(point / ClusterSize));
        return new int4(posID, ClusterLayer);
    }

    public static float3 ClusterIDToPoint(int3 clusterID)
    {
        return new float3(clusterID) * new float3(ClusterSize) + new float3(ClusterExtent);
    }

    public static void GetMinMaxClusterIDs(in AABB aabb, out int4 minClusterID, out int4 maxClusterID)
    {
        minClusterID = PointToClusterID(aabb.Min);
        maxClusterID = PointToClusterID(aabb.Max) + new int4(1, 1, 1, 0);
    }

    public static int4 PointToILeafID(float3 point)
    {
        var posID = new int3(math.floor(point / LeafSize));
        return new int4(posID, LeafLayer);
    }

    public static float3 LeafIDToPoint(int3 leafID)
    {
        return new float3(leafID) * LeafSize + LeafExtent;
    }

    public static float3 NodeIDToPoint(int4 nodeID)
    {
        var nodeExtent = NodeExtent(nodeID.w);
        var nodeSize = nodeExtent * 2f;

        return new float3(nodeID.xyz) * nodeSize + nodeExtent;
    }

    public static void GetMinMaxNodeChildrenID(int4 nodeID, out int4 minChildrenID, out int4 maxChildrenID)
    {
        var additionalDivision = nodeID.w == ClusterLayer ? ClusterAdditionalDivision : 0;

        minChildrenID = new int4(nodeID.xyz << 1 + additionalDivision, nodeID.w + 1);
        maxChildrenID = minChildrenID + new int4(2, 2, 2, 0) * (1 << additionalDivision);
    }

    public static int4 GetParentNodeID(int4 nodeID, int parentLayer)
    {
        if (parentLayer == nodeID.w) return nodeID;

        AssertParentLayer(nodeID.w, parentLayer);

        var additionalShift = parentLayer == ClusterLayer ? ClusterAdditionalDivision : 0;

        var rshift = nodeID.w - parentLayer + additionalShift;

        return new int4(nodeID.xyz >> rshift, parentLayer);
    }

    public static int4 GetLeafParentNodeID(int3 leafID, int parentLayer)
    {
        if (parentLayer == LeafLayer) return new int4(leafID, LeafLayer);

        var additionalShift = parentLayer == ClusterLayer ? ClusterAdditionalDivision : 0;

        var rshift = LeafLayer - parentLayer + additionalShift;
        return new int4(leafID >> rshift, parentLayer);
    }

    private static void AssertValidPackedField(UInt64 x, UInt64 y, UInt64 z, UInt64 l)
    {
#if ENABLE_ASSERTS
        AssertValidPackedPosition(x);
        AssertValidPackedPosition(y);
        AssertValidPackedPosition(z);
        AssertValidPackedLayer(l);
#endif
    }

    private static void AssertValidPackedPosition(UInt64 p)
    {
#if ENABLE_ASSERTS
        Debug.Assert(p < MaxPosition);
#endif
    }

    private static void AssertValidPackedLayer(UInt64 l)
    {
#if ENABLE_ASSERTS
        Debug.Assert(l < MaxLayer);
#endif
    }

    private static void AssertParentLayer(int layer, int parentLayer)
    {
#if ENABLE_ASSERTS
        Debug.Assert(parentLayer < layer);
#endif
    }
}
