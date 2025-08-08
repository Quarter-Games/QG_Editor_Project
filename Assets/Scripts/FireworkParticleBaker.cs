using Unity.Entities;
using UnityEngine;

public class FireworkParticleBaker : Baker<FireworkParticleAuthoring>
{
    public override void Bake(FireworkParticleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FireworkParticle
        {
            MaxSpeed = authoring.MaxSpeed,
            LifeTime = authoring.LifeTime,
            GravityStrength = authoring.GravityStrength,
        });
    }
}
