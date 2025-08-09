using Unity.Entities;
using Unity.Mathematics;

public struct Velocity : IComponentData
{
    public float3 Value;
}

public struct Lifetime : IComponentData
{
    public float Value;
}

public struct Gravity : IComponentData
{
    public float Value;
}

public struct ParticleColor : IComponentData
{
    public int Name; // Shader property ID
    public float4 Value;
}
