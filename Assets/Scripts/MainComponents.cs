using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct OctreeID : IComponentData
{
    public int3 ID0;
    public int3 ID1;
}

struct Spawner : IComponentData
{
    public Vector3 Origin;
    public Entity Prefab;
    public int Count;
}

public struct VisibleOctreeIDs : IBufferElementData
{
    public OctreeID Value;
}

public struct WorldBoundingRadius : IComponentData
{
    public float Value;
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