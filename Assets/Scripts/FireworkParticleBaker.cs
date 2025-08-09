using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FireworkParticleBaker : Baker<FireworkParticleAuthoring>
{
    public override void Bake(FireworkParticleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FireworkParticleData
        {
            MaxSpeed = authoring.MaxSpeed,
            LifeTime = authoring.LifeTime,
            GravityStrength = authoring.GravityStrength,
            Color = new float4(authoring.Color.r, authoring.Color.g, authoring.Color.b, authoring.Color.a)
        });
    }
}
