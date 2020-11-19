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
}

public struct VisibleClusterID : IBufferElementData
{
    public ClusterID Value;
}

public struct VisibleOctreeID : IBufferElementData
{
    public OctreeID Value;
}

public struct VisibleLeafInClusterCount : IBufferElementData
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