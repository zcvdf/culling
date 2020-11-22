using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct Spawner : IComponentData
{
    public Vector3 Origin;
    public Entity Prefab;
    public int Count;
    public float GenerationSpan;
    public float MinScale;
    public float MaxScale;
    public float MinRotationSpeed;
    public float MaxRotationSpeed;
}

public struct VisibleOctreeCluster : IBufferElementData
{
    public UInt64 Value;

    public static implicit operator OctreeCluster(VisibleOctreeCluster x) => new OctreeCluster { Value = x.Value };
}

public struct VisibleOctreeNode : IBufferElementData
{
    public UInt64 Value;

    public static implicit operator OctreeLeaf(VisibleOctreeNode x) => new OctreeLeaf { Value = x.Value };
}

public struct VisibleNodeInClusterCount : IBufferElementData
{
    public int Value;
}

public struct WorldOccluderExtents : IComponentData
{
    public float3 LocalRight;
    public float LocalRightLength;
    public float3 LocalUp;
    public float LocalUpLength;
}

public struct WorldOccluderRadius : IComponentData
{
    public float Value;
}