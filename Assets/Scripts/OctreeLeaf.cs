using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct OctreeLeaf : IComponentData
{
    public UInt64 Value;

    public static implicit operator VisibleOctreeNode(OctreeLeaf x) => new VisibleOctreeNode { Value = x.Value };
}
