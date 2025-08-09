using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AutomatedTowerAuthoring : MonoBehaviour
{
    public float CoolDownSeconds = 1f;
    public ProjectileAuthoring ProjectileAuthoringPrefab;

    class Baker : Baker<AutomatedTowerAuthoring>
    {
        public override void Bake(AutomatedTowerAuthoring authoring)
        {
            var projectileEntity = GetEntity(authoring.ProjectileAuthoringPrefab, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AutomatedTower
            {
                CoolDownSeconds = authoring.CoolDownSeconds,
                CoolDownTimer = authoring.CoolDownSeconds,
                ProjectilePrefab = projectileEntity
            });
        }
    }
}

public struct AutomatedTower : IComponentData
{
    public float CoolDownSeconds;
    public float CoolDownTimer;
    public Entity ProjectilePrefab;

    public void Shoot(ref SystemState state, Entity thisEntity, float2 direction)
    {
        var em = state.EntityManager;

        // Instantiate projectile entity
        var projectileEntity = em.Instantiate(ProjectilePrefab);

        // Get tower position
        var towerTransform = em.GetComponentData<LocalTransform>(thisEntity);

        // Get projectile data from prefab as template
        var projData = em.GetComponentData<Projectile>(ProjectilePrefab);

        // Set projectile components
        em.SetComponentData(projectileEntity, new Projectile
        {
            Speed = projData.Speed,
            Lifetime = projData.Lifetime,
            Timer = projData.Lifetime,
            Direction = direction
        });

        em.SetComponentData(projectileEntity, new LocalTransform
        {
            Position = towerTransform.Position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        Debug.Log("Shoot!");
    }
}

[BurstCompile]
public partial struct AutomatedTowerSystem : ISystem
{
    private float2 GetClosestEnemy(ref SystemState state, float3 currentPosition)
    {
        float3 closestEnemyPos = new float3(0, 0, 0);
        float closestDistance = float.MaxValue;

        // Query enemies
        foreach (var (enemyHealth, enemyTransform) in SystemAPI.Query<RefRO<EnemyHealth>, RefRO<LocalTransform>>())
        {
            if (enemyHealth.ValueRO.CurrentHealth <= 0)
                continue;

            float dist = math.distance(enemyTransform.ValueRO.Position, currentPosition);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestEnemyPos = enemyTransform.ValueRO.Position;
            }
        }

        if (closestDistance == float.MaxValue)
            return new float2(1, 0); // No enemies found

        float2 direction = new float2(closestEnemyPos.x - currentPosition.x, closestEnemyPos.y - currentPosition.y);
        return math.normalize(direction);
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (tower, entity) in SystemAPI.Query<RefRW<AutomatedTower>>().WithEntityAccess())
        {
            tower.ValueRW.CoolDownTimer -= deltaTime;

            if (tower.ValueRW.CoolDownTimer <= 0f)
            {
                if (tower.ValueRO.ProjectilePrefab == Entity.Null)
                    continue;
                var towerPos = SystemAPI.GetComponent<LocalTransform>(entity).Position; 
                float2 shootDir = GetClosestEnemy(ref state, towerPos);

                tower.ValueRW.Shoot(ref state, entity, shootDir);

                tower.ValueRW.CoolDownTimer = tower.ValueRW.CoolDownSeconds; // Reset cooldown
            }
        }
    }
}
