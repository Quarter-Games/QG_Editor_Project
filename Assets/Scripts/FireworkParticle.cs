using Unity.Entities;
using Unity.Mathematics;

public struct FireworkParticle : IComponentData
{
    public float3 Velocity;
    public float3 Position;
    public float Lifetime;
    public float StartLifetime;
    public float MaxSpeed;
    public float CurrentLifetime;
    internal float LifeTime;
    internal float GravityStrength;
}

public struct ParticleSize : IComponentData
{
    public float StartSize;
}
