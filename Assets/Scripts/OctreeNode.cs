using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct OctreeNode : IComponentData
{
    public UInt64 Value;

    public static implicit operator VisibleOctreeNode(OctreeNode x) => new VisibleOctreeNode { Value = x.Value };
}
