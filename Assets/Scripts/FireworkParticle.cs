using Unity.Entities;
using Unity.Mathematics;

public struct FireworkParticle : IComponentData
{
    public float3 Velocity;
    public float3 Position;
    public float Lifetime;
    public float StartLifetime;
}

public struct ParticleSize : IComponentData
{
    public float StartSize;
}
