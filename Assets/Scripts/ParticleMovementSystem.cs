using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ParticleMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float gravity = -9.81f; // tweak this for weaker gravity during explosion

        foreach (var (vel, trans) in SystemAPI.Query<RefRW<Velocity>, RefRW<LocalTransform>>())
        {
            vel.ValueRW.Value.y += gravity * deltaTime;
            trans.ValueRW.Position += vel.ValueRW.Value * deltaTime;
        }
    }
}

public struct FireworkParticleData : IComponentData
{
    public float MaxSpeed;
    public float LifeTime;
    public float GravityStrength;
    public float4 Color;
}
