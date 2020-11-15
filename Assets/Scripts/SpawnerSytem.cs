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

    protected override void OnUpdate()
    {
        var cmd = new EntityCommandBuffer(Allocator.Temp);

        var rand = new Rand(10);

        this.Entities
        .WithAll<SpawnerUnusedTag>()
        .ForEach((in Entity spawnerEntity, in Spawner spawner) =>
        {
            for (int i = 0; i < spawner.Count; ++i)
            {
                var entity = cmd.Instantiate(spawner.Prefab);

                var offset = 500 * (rand.NextFloat3(new float3(1f)) - new float3(0.5f));
                var position = new float3(spawner.Origin) + offset;

                cmd.AddComponent(entity, new Translation { Value = position });
                cmd.AddComponent<EntityTag>(entity);
                cmd.AddComponent<URPMaterialPropertyBaseColor>(entity);
                cmd.AddComponent<WorldBoundingRadius>(entity);
                cmd.AddComponent(entity, Octree.RootID);
            }

            cmd.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);
        })
        .Run();

        cmd.Playback(this.EntityManager);
    }
}
