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
using UnityRand = UnityEngine.Random;

public class SpawnerSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        this.cmdBufferSystem = this.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        Main.World = this.World;
        Main.EntityManager = this.EntityManager;
        Main.EntityQuery = this.EntityManager.CreateEntityQuery(typeof(EntityTag), typeof(Translation), typeof(WorldBoundingRadius));
    }

    protected override void OnUpdate()
    {
        var cmd = this.cmdBufferSystem.CreateCommandBuffer();
        
        this.Entities
        .WithAll<SpawnerUnusedTag>()
        .WithoutBurst()
        .ForEach((ref Entity spawnerEntity, in Spawner spawner) =>
        {
            for (int i = 0; i < spawner.Count; ++i)
            {
                var entity = cmd.Instantiate(spawner.Prefab);

                float3 offset = float3.zero;
                offset.x = (UnityRand.value - 0.5f);
                offset.y = (UnityRand.value - 0.5f);
                offset.z = (UnityRand.value - 0.5f);
                offset *= 100f;

                float3 position = new float3(spawner.Origin) + offset;

                cmd.SetComponent(entity, new Translation { Value = position });
                cmd.AddComponent<EntityTag>(entity);
                cmd.AddComponent<URPMaterialPropertyBaseColor>(entity);
                cmd.AddComponent<WorldBoundingRadius>(entity);
                cmd.AddSharedComponent(entity, Octree.RootID);
            }

            cmd.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
        })
        .Run();
    }
}
