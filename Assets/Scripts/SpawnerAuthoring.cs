using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    [SerializeField] GameObject EntityPrefab;
    [SerializeField] int EntityCount;
    [SerializeField] GameObject SphereOccluderPrefab;
    [SerializeField] int SphereOccluderCount;
    [SerializeField] GameObject QuadOccluderPrefab;
    [SerializeField] int QuadOccluderCount;
    [SerializeField] float MinGenerationSpan = 200f;
    [SerializeField] float MaxGenerationSpan = 2000f;
    [SerializeField] float MinScale = 1f;
    [SerializeField] float MaxScale = 20f;
    [SerializeField] float MinSelfRotationSpeed = 1f;
    [SerializeField] float MaxSelfRotationSpeed = 10f;
    [SerializeField] float MinWorldRotationSpeed = 0.01f;
    [SerializeField] float MaxWorldRotationSpeed = 1f;
    [SerializeField] [Range(0, 100)]  int StaticEntityPercentage = 80;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(this.EntityPrefab);
        referencedPrefabs.Add(this.SphereOccluderPrefab);
        referencedPrefabs.Add(this.QuadOccluderPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawner = new Spawner
        {
            Origin = this.transform.position,
            EntityPrefab = conversionSystem.GetPrimaryEntity(this.EntityPrefab),
            EntityCount = this.EntityCount,
            SphereOccluderPrefab = conversionSystem.GetPrimaryEntity(this.SphereOccluderPrefab),
            SphereOccluderCount = this.SphereOccluderCount,
            QuadOccluderPrefab = conversionSystem.GetPrimaryEntity(this.QuadOccluderPrefab),
            QuadOccluderCount = this.QuadOccluderCount,
            MinGenerationSpan = this.MinGenerationSpan,
            MaxGenerationSpan = this.MaxGenerationSpan,
            MinScale = this.MinScale,
            MaxScale = this.MaxScale,
            MinSelfRotationSpeed = this.MinSelfRotationSpeed,
            MaxSelfRotationSpeed = this.MaxSelfRotationSpeed,
            MinWorldRotationSpeed = this.MinWorldRotationSpeed,
            MaxWorldRotationSpeed = this.MaxWorldRotationSpeed,
            StaticEntityPercentage = this.StaticEntityPercentage,
        };

        dstManager.AddComponentData(entity, spawner);
        dstManager.AddComponent<SpawnerUnusedTag>(entity);
    }
}
