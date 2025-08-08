
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 10f;
    public class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Projectile
            {
                Speed = authoring.speed,
                Lifetime = authoring.lifetime,
                Timer = authoring.lifetime,
                Direction = Vector2.right // Default direction, can be set later
            });
        }
    }
}
public struct Projectile : IComponentData
{
    public float Speed;
    public float Lifetime;
    public float Timer;
    public float2 Direction;
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
            // Update the projectile's timer  

            // Move the projectile based on its direction and speed  
            // Handle projectile expiration  
            if (projectile.ValueRW.Timer <= 0f)
            {
                projectile.ValueRW.Update(deltaTime);
                //EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                //entityCommandBuffer.DestroyEntity(entity);
            }
            else
            {

                var transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                transform.Position += new float3(projectile.ValueRW.Direction.x, projectile.ValueRW.Direction.y, 0) * projectile.ValueRW.Speed * deltaTime;
                state.EntityManager.SetComponentData(entity, transform);

            }
        }
    }
}