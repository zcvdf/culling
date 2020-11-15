using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct WorldBoundingRadius : IComponentData
{
    public float Value;
}
