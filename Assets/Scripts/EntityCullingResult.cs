using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public enum CullingResult
{
    NotCulled,
    CulledByOctree,
    CulledByFrustrumPlanes,
    CulledBySphereOccluder,
    CulledByQuadOccluder,
}

[GenerateAuthoringComponent]
public struct EntityCullingResult : IComponentData
{
    public CullingResult Value;
}
