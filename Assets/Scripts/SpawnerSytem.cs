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
        base.OnCreate();

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
                var ant = cmd.Instantiate(spawner.Prefab);
                float3 position = spawner.Origin + UnityRand.insideUnitSphere * 2f;

                cmd.SetComponent(ant, new Translation { Value = position });
            }

            cmd.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
        })
        .Run();
    }
}
