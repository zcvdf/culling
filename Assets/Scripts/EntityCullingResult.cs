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
public class EntityCullingResult : IComponentData
{
    public CullingResult Value;
}
