using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ParticleLifetimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (lifetime, transform, entity) in
                 SystemAPI.Query<RefRW<Lifetime>, RefRW<LocalTransform>>()
                           .WithEntityAccess())
        {
            // Countdown lifetime
            lifetime.ValueRW.Value -= deltaTime;

            // Shrink scale proportionally to remaining lifetime
            transform.ValueRW.Scale = math.max(0f, transform.ValueRW.Scale - deltaTime * 0.99f);

            // Destroy when expired
            if (lifetime.ValueRW.Value <= 0f)
                ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
