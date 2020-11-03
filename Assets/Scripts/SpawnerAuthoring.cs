using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    [SerializeField] GameObject Prefab;
    [SerializeField] int Count;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(this.Prefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawner = new Spawner
        {
            Origin = this.transform.position,
            Prefab = conversionSystem.GetPrimaryEntity(this.Prefab),
            Count = this.Count
        };

        dstManager.AddComponentData(entity, spawner);
        dstManager.AddComponent<SpawnerUnusedTag>(entity);
    }
}
