
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;
    public class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Projectile
            {
                Speed = authoring.speed,
                Lifetime = authoring.lifetime,
                Timer = authoring.lifetime
            });
        }
    }
}
public struct Projectile : IComponentData
{
    public float Speed;
    public float Lifetime;
    public float Timer;
    public void Update(float deltaTime)
    {
        Timer -= deltaTime;
        if (Timer <= 0f)
        {
            // Handle projectile expiration logic here
            Debug.Log("Projectile expired");
        }
    }
}
[BurstCompile]
public partial struct ProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (projectile, entity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
        {
            projectile.ValueRW.Update(deltaTime);
            if (projectile.ValueRW.Timer <= 0f)
            {
                EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                entityCommandBuffer.DestroyEntity(entity);
            }
        }
    }
}