using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct FireworkUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) => state.RequireForUpdate<FireworkParticle>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (particle, size, transform, entity) in
                 SystemAPI.Query<RefRW<FireworkParticle>, RefRW<ParticleSize>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            particle.ValueRW.Lifetime -= dt;
            if (particle.ValueRW.Lifetime <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            float t = 1f - (particle.ValueRW.Lifetime / particle.ValueRW.StartLifetime);
            float3 gravity = new float3(0, -9.81f * 0.2f, 0);

            // Decay velocity and apply gravity over time
            particle.ValueRW.Velocity = math.lerp(particle.ValueRW.Velocity, float3.zero, dt / particle.ValueRW.StartLifetime);
            particle.ValueRW.Velocity += gravity * t * dt;
            particle.ValueRW.Position += particle.ValueRW.Velocity * dt;

            // Update scale (shrink)
            float scale = math.max(0, (1f - t)) * size.ValueRW.StartSize;

            transform.ValueRW.Position = particle.ValueRW.Position;
            transform.ValueRW.Scale = scale;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
