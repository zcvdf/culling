using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

struct Spawner : IComponentData
{
    public Vector3 Origin;
    public Entity Prefab;
    public int Count;
}
