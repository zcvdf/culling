using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public enum CullingResult
{
    NotCulled,
    CulledByOctreeClusters,
    CulledByOctreeNodes,
    CulledByFrustrumPlanes,
    CulledBySphereOccluder,
    CulledByQuadOccluder,
}

[GenerateAuthoringComponent]
public struct EntityCullingResult : IComponentData
{
    public CullingResult Value;
}
