using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WorldOccluderExtents : IComponentData
{
    public float3 Right;
    public float3 Up;
}
