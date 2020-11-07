﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityRand = UnityEngine.Random;

public class SpawnerSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        this.cmdBufferSystem = this.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        Main.World = this.World;
        Main.EntityManager = this.EntityManager;
        Main.EntityQuery = GetEntityQuery(typeof(EntityTag), typeof(Translation), typeof(WorldBoundingRadius));
    }

    protected override void OnUpdate()
    {
        var cmd = this.cmdBufferSystem.CreateCommandBuffer();
        
        this.Entities
        .WithAll<SpawnerUnusedTag>()
        .ForEach((ref Entity spawnerEntity, in Spawner spawner) =>
        {
            for (int i = 0; i < spawner.Count; ++i)
            {
                var entity = cmd.Instantiate(spawner.Prefab);
                var rand = UnityRand.insideUnitSphere;
                float3 position = spawner.Origin + rand.normalized * 20f + rand * 10f;

                cmd.SetComponent(entity, new Translation { Value = position });
                cmd.AddComponent<EntityTag>(entity);
                cmd.AddComponent<URPMaterialPropertyBaseColor>(entity);
                cmd.AddComponent<WorldBoundingRadius>(entity);
            }

            cmd.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
        })
        .Run();
    }
}
