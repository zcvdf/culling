using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityRand = UnityEngine.Random;

public class SpawnerSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        this.cmdBufferSystem = this.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
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
                float3 position = spawner.Origin + rand.normalized * 10f + rand * 20f;

                cmd.SetComponent(entity, new Translation { Value = position });
            }

            cmd.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
        })
        .Run();
    }
}
