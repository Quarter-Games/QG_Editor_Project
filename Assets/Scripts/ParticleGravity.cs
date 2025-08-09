using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct GravitySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Velocity>();
        state.RequireForUpdate<Gravity>();
        state.RequireForUpdate<Lifetime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (vel, grav, life, entity) in
                 SystemAPI.Query<RefRW<Velocity>, RefRO<Gravity>, RefRO<Lifetime>>()
                 .WithEntityAccess())
        {
            // Apply gravity (negative Y direction)
            vel.ValueRW.Value.y -= grav.ValueRO.Value * deltaTime;
        }
    }
}
