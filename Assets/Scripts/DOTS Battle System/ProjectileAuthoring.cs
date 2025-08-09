
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
                Direction = Vector2.right
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
    public void UpdateTimer(float deltaTime)
    {
        Timer -= deltaTime;
    }

}
[BurstCompile]
public partial struct ProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (projectile, entity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
        {
            projectile.ValueRW.Timer -= deltaTime;

            if (projectile.ValueRO.Timer <= 0f)
            {
                ecb.DestroyEntity(entity);
                Debug.Log("Projectile destroyed");
            }
            else
            {
                var transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                transform.Position += new float3(projectile.ValueRW.Direction.x, projectile.ValueRW.Direction.y, 0) * projectile.ValueRW.Speed * deltaTime;
                state.EntityManager.SetComponentData(entity, transform);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}