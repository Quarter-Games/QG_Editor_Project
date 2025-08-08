using Unity.Entities;
using UnityEngine;

public class FireworkParticleAuthoring : MonoBehaviour
{
    public class Baker : Baker<FireworkParticleAuthoring>
    {
        public override void Bake(FireworkParticleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<FireworkParticle>(entity);
            AddComponent<ParticleSize>(entity);
        }
    }
}
