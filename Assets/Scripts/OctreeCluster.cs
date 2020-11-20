using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct OctreeCluster : ISharedComponentData
{
    public UInt64 Value;

    public static implicit operator VisibleOctreeCluster(OctreeCluster x) => new OctreeCluster { Value = x.Value };
}
