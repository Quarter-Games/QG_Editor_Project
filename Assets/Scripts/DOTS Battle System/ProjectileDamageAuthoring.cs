using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public class ProjectileDamageAuthoring : MonoBehaviour
{
    public float Damage = 10f;
    class Baker : Baker<ProjectileDamageAuthoring>
    {
        public override void Bake(ProjectileDamageAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ProjectileDamage
            {
                Damage = authoring.Damage
            });
        }
    }
}
public struct ProjectileDamage : IComponentData
{
    public float Damage;
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct ProjectileDamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var _ecbSystem = state.World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        var job = new CollisionJob
        {
            ProjectileLookup = SystemAPI.GetComponentLookup<ProjectileDamage>(true),
            EnemyLookup = SystemAPI.GetComponentLookup<EnemyHealth>(true),
            ECB = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
        };


        var sim = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = job.Schedule(sim, state.Dependency);
        _ecbSystem.AddJobHandleForProducer(state.Dependency);
    }

    [BurstCompile]
    public struct CollisionJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<ProjectileDamage> ProjectileLookup;
        [ReadOnly] public ComponentLookup<EnemyHealth> EnemyLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        public void Execute(TriggerEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;
            bool aIsProjectile = ProjectileLookup.HasComponent(entityA);
            bool bIsProjectile = ProjectileLookup.HasComponent(entityB);
            bool aIsEnemy = EnemyLookup.HasComponent(entityA);
            bool bIsEnemy = EnemyLookup.HasComponent(entityB);


            if ((aIsProjectile && bIsEnemy) || (bIsProjectile && aIsEnemy))
            {
                Entity projectileEntity = aIsProjectile ? entityA : entityB;
                Entity enemyEntity = aIsEnemy ? entityA : entityB;

                var damage = (int)ProjectileLookup[projectileEntity].Damage;

                Entity eventEntity = ECB.CreateEntity(0);
                ECB.AddComponent(0, eventEntity, new DamageEvent
                {
                    Target = enemyEntity,
                    Amount = damage
                });
                ECB.DestroyEntity(0, projectileEntity);
            }
        }
    }
}
public struct DamageEvent : IComponentData
{
    public Entity Target;
    public int Amount;
}
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ApplyDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (damageEvent, entity) in SystemAPI.Query<DamageEvent>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<EnemyHealth>(damageEvent.Target))
            {
                var health = SystemAPI.GetComponent<EnemyHealth>(damageEvent.Target);
                health.CurrentHealth -= damageEvent.Amount;
                SystemAPI.SetComponent(damageEvent.Target, health);
                if (health.CurrentHealth <= 0)
                {
                    ecb.DestroyEntity(damageEvent.Target);
                }
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}