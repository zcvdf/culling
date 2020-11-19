using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ClusterID : ISharedComponentData
{
    public UInt64 Value;
}
