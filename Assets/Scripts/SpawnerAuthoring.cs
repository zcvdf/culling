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
    [SerializeField] float MinGenerationSpan = 200f;
    [SerializeField] float MaxGenerationSpan = 2000f;
    [SerializeField] float MinScale = 1f;
    [SerializeField] float MaxScale = 20f;
    [SerializeField] float MinSelfRotationSpeed = 1f;
    [SerializeField] float MaxSelfRotationSpeed = 10f;
    [SerializeField] float MinWorldRotationSpeed = 0.01f;
    [SerializeField] float MaxWorldRotationSpeed = 1f;

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
            Count = this.Count,
            MinGenerationSpan = this.MinGenerationSpan,
            MaxGenerationSpan = this.MaxGenerationSpan,
            MinScale = this.MinScale,
            MaxScale = this.MaxScale,
            MinSelfRotationSpeed = this.MinSelfRotationSpeed,
            MaxSelfRotationSpeed = this.MaxSelfRotationSpeed,
            MinWorldRotationSpeed = this.MinWorldRotationSpeed,
            MaxWorldRotationSpeed = this.MaxWorldRotationSpeed,
        };

        dstManager.AddComponentData(entity, spawner);
        dstManager.AddComponent<SpawnerUnusedTag>(entity);
    }
}
