using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using Rand = Unity.Mathematics.Random;

public class SpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        Main.World = this.World;
        Main.EntityManager = this.EntityManager;
        Main.EntityQuery = this.EntityManager.CreateEntityQuery(typeof(EntityTag), typeof(Translation), typeof(WorldBoundingRadius));
    }

    [BurstCompatible]
    protected override void OnUpdate()
    {
        var spawnerEntity = GetSingletonEntity<Spawner>();
        if (!HasComponent<SpawnerUnusedTag>(spawnerEntity)) return;

        var spawner = GetSingleton<Spawner>();
        var entities = this.EntityManager.Instantiate(spawner.Prefab, spawner.Count, Allocator.Temp);

        var rand = new Rand(10);

        for (int i = 0; i < entities.Length; ++i)
        {
            var entity = entities[i];

            var offset = 1000f * (rand.NextFloat3(new float3(2f)) - new float3(1f));
            var position = new float3(spawner.Origin) + offset;

            this.EntityManager.SetComponentData(entity, new Translation { Value = position });
            this.EntityManager.AddSharedComponentData(entity, new OctreeCluster());
        }

        this.EntityManager.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
    }
}
