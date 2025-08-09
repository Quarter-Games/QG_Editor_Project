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

        // Logic to instantiate and shoot the projectile
        var temp = state.EntityManager.Instantiate(ProjectilePrefab);
        var towerPosition = state.EntityManager.GetComponentData<LocalTransform>(thisEntity).Position;
        var projData = state.EntityManager.GetComponentData<Projectile>(ProjectilePrefab);
        state.EntityManager.SetComponentData(temp, new Projectile()
        {
            Speed = projData.Speed, // Set the speed of the projectile
            Lifetime = projData.Lifetime, // Set the lifetime of the projectile
            Timer = projData.Lifetime, // Initialize the timer to the lifetime
            Direction = direction,
        });
        state.EntityManager.SetComponentData(temp, new LocalTransform
        {
            Position = towerPosition,
            Rotation = Quaternion.identity,
            Scale = 1f
        });
        Debug.Log("Shoot!");
    }

}
[BurstCompile]
public partial struct AutomatedTowerSystem : ISystem
{

    private readonly float2 GetClosestEnemy(ref SystemState state, float3 CurrentPosition)
    {
        float3 closestEnemyPosition = new float3(0, 0, 0);
        float closestDistance = float.MaxValue;



        foreach (var (enemyHealth, enemyTransform, enemyEntity) in SystemAPI.Query<RefRO<EnemyHealth>, LocalTransform>().WithEntityAccess())
        {
            if (enemyHealth.ValueRO.CurrentHealth <= 0)
            {
                continue; // Skip dead enemies
            }
            float distance = math.distance(enemyTransform.Position, CurrentPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemyPosition = enemyTransform.Position;
            }
        }
        if (closestDistance == float.MaxValue)
        {
            // No enemies found, return a default direction
            return new Vector2(1, 0);
        }
        // Calculate the direction to the closest enemy
        float2 direction = new float2(closestEnemyPosition.x - CurrentPosition.x, closestEnemyPosition.y - CurrentPosition.y);
        return math.normalize(direction); // Normalize the direction vector
    }
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (tower, entity) in SystemAPI.Query<RefRW<AutomatedTower>>().WithEntityAccess())
        {
            tower.ValueRW.CoolDownTimer -= deltaTime;
            if (tower.ValueRW.CoolDownTimer <= 0f)
            {
                var towerPos = state.EntityManager.GetComponentData<LocalTransform>(entity).Position;
                tower.ValueRW.Shoot(ref state, entity, GetClosestEnemy(ref state, towerPos));
                tower.ValueRW.CoolDownTimer = tower.ValueRW.CoolDownSeconds; // Reset cooldown
            }
        }
    }
}