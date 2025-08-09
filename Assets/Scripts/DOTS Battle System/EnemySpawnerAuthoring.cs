using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject EnemyHealthAuthoringPrefab;
    public float MinSpawnRadius = 5f;
    public float MaxSpawnRadius = 10f;
    public float SpawnInterval = 2f;

    class Baker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            Entity prefabEntity = GetEntity(authoring.EnemyHealthAuthoringPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemySpawner
            {
                EnemyPrefab = prefabEntity,
                MinRadius = authoring.MinSpawnRadius,
                MaxRadius = authoring.MaxSpawnRadius,
                SpawnInterval = authoring.SpawnInterval,
                SpawnTimer = 0f
            });
        }
    }
}

public struct EnemySpawner : IComponentData
{
    public Entity EnemyPrefab;
    public float MinRadius;
    public float MaxRadius;
    public float SpawnInterval;
    public float SpawnTimer;
}

[BurstCompile]
public partial struct EnemySpawnerSystem : ISystem
{

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var entityManager = state.EntityManager;
        var random = new Unity.Mathematics.Random((uint)Environment.TickCount);

        foreach (var (spawner, entity) in SystemAPI.Query<RefRW<EnemySpawner>>().WithEntityAccess())
        {
            ProcessEntity(deltaTime, ref entityManager, ref random, spawner);
        }
    }

    private static void ProcessEntity(float deltaTime, ref EntityManager entityManager, ref Unity.Mathematics.Random random, RefRW<EnemySpawner> spawner)
    {
        spawner.ValueRW.SpawnTimer -= deltaTime;
        if (spawner.ValueRW.SpawnTimer <= 0f && spawner.ValueRO.EnemyPrefab != Entity.Null)
        {
            float3 spawnPos = CalculatePosition(ref random, spawner);

            Entity spawnedEnemy = entityManager.Instantiate(spawner.ValueRO.EnemyPrefab);

            entityManager.SetComponentData(spawnedEnemy, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            spawner.ValueRW.SpawnTimer = spawner.ValueRO.SpawnInterval;
        }
    }

    private static float3 CalculatePosition(ref Unity.Mathematics.Random random, RefRW<EnemySpawner> spawner)
    {
        float angle = random.NextFloat(0f, 2f * math.PI);
        float radius = random.NextFloat(spawner.ValueRO.MinRadius, spawner.ValueRO.MaxRadius);

        float3 spawnPos = new float3(
            math.cos(angle) * radius,
            math.sin(angle) * radius,
            0f
        );
        return spawnPos;
    }
}
