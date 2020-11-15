using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct VisibleOctreeIDs : IBufferElementData
{
    public OctreeID Value;
}