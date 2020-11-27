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
        Main.EntityQuery = this.EntityManager.CreateEntityQuery(typeof(EntityTag), typeof(WorldRenderBounds));
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

            var offset = rand.NextFloat(spawner.MinGenerationSpan, spawner.MaxGenerationSpan) * rand.NextFloat3Direction();
            var position = new float3(spawner.Origin) + offset;
            var scale = rand.NextFloat3(new float3(spawner.MinScale), new float3(spawner.MaxScale));
            var rotation = rand.NextQuaternionRotation();

            var selfRotationAxis = rand.NextFloat3Direction();
            var selfRotationSpeed = rand.NextFloat(spawner.MinSelfRotationSpeed, spawner.MaxSelfRotationSpeed);

            var worldRotationAxis = rand.NextFloat3Direction();
            var worldRotationSpeed = rand.NextFloat(spawner.MinWorldRotationSpeed, spawner.MaxWorldRotationSpeed);

            var isStatic = rand.NextInt(0, 100) < spawner.StaticEntityPercentage;

            if (isStatic)
            {
                var trs = float4x4.TRS(position, rotation, scale);
                this.EntityManager.SetComponentData(entity, new LocalToWorld { Value = trs });

                this.EntityManager.RemoveComponent<Translation>(entity);
                this.EntityManager.RemoveComponent<Rotation>(entity);
                this.EntityManager.AddComponent<StaticOptimizeEntity>(entity);
            }
            else
            {
                this.EntityManager.AddComponentData(entity, new NonUniformScale { Value = scale });
                this.EntityManager.SetComponentData(entity, new Translation { Value = position });
                this.EntityManager.SetComponentData(entity, new Rotation { Value = rotation });

                this.EntityManager.AddComponentData(entity, new SelfRotationAxis { Value = selfRotationAxis });
                this.EntityManager.AddComponentData(entity, new SelfRotationSpeed { Value = selfRotationSpeed });

                this.EntityManager.AddComponentData(entity, new WorldRotationAxis { Value = worldRotationAxis });
                this.EntityManager.AddComponentData(entity, new WorldRotationSpeed { Value = worldRotationSpeed });
            }
            
            this.EntityManager.AddSharedComponentData(entity, new OctreeCluster());
        }

        this.EntityManager.RemoveComponent<SpawnerUnusedTag>(spawnerEntity);

        Stats.TotalEntityNumber = spawner.Count;
    }
}
