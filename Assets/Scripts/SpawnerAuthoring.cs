using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    [Header("Entities Generation")]
    [SerializeField] GameObject EntityPrefab;
    [SerializeField] int EntityCount;
    [SerializeField] float MinGenerationSpan = 200f;
    [SerializeField] float MaxGenerationSpan = 2000f;
    [SerializeField] float MinScale = 1f;
    [SerializeField] float MaxScale = 20f;
    [SerializeField] float MinSelfRotationSpeed = 1f;
    [SerializeField] float MaxSelfRotationSpeed = 10f;
    [SerializeField] float MinWorldRotationSpeed = 0.01f;
    [SerializeField] float MaxWorldRotationSpeed = 1f;
    [SerializeField] [Range(0, 100)] int StaticEntityPercentage = 80;

    [Header("Sphere Occluder Generation")]
    [SerializeField] GameObject SphereOccluderPrefab;
    [SerializeField] int SphereOccluderCount;
    [SerializeField] float SphereOccluderMinGenerationSpan = 1000f;
    [SerializeField] float SphereOccluderMaxGenerationSpan = 2000f;
    [SerializeField] float SphereOccluderMinScale = 10f;
    [SerializeField] float SphereOccluderMaxScale = 50f;

    [Header("Quad Occluder Generation")]
    [SerializeField] GameObject QuadOccluderPrefab;
    [SerializeField] int QuadOccluderCount;
    [SerializeField] float QuadOccluderMinGenerationSpan = 1000f;
    [SerializeField] float QuadOccluderMaxGenerationSpan = 2000f;
    [SerializeField] float QuadOccluderMinScale = 10f;
    [SerializeField] float QuadOccluderMaxScale = 50f;

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

            SphereOccluderMaxGenerationSpan = this.SphereOccluderMaxGenerationSpan,
            SphereOccluderMaxScale = this.SphereOccluderMaxScale,
            SphereOccluderMinGenerationSpan = this.SphereOccluderMinGenerationSpan,
            SphereOccluderMinScale = this.SphereOccluderMinScale,

            QuadOccluderMaxGenerationSpan = this.QuadOccluderMaxGenerationSpan,
            QuadOccluderMaxScale = this.QuadOccluderMaxScale,
            QuadOccluderMinGenerationSpan = this.QuadOccluderMinGenerationSpan,
            QuadOccluderMinScale = this.QuadOccluderMinScale,
        };

        dstManager.AddComponentData(entity, spawner);
        dstManager.AddComponent<SpawnerUnusedTag>(entity);
    }
}
