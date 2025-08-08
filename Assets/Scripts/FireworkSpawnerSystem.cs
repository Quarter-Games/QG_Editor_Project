using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct FireworkSpawnerSystem : ISystem
{
    private float timer;
    private Entity particlePrefab;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        timer = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        timer += dt;

        if (particlePrefab == Entity.Null)
        {
            // You must manually assign this prefab in the editor via a Baker
            var query = SystemAPI.QueryBuilder().WithAll<FireworkParticle>().Build();
            foreach (var entity in query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                particlePrefab = entity;
                break;
            }

            if (particlePrefab == Entity.Null)
                return;
        }

        if (timer >= 2f)
        {
            timer = 0f;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            int count = 100;
            float3 origin = new float3(0, 0, 0);

            for (int i = 0; i < count; i++)
            {
                var particle = ecb.Instantiate(particlePrefab);

                float3 dir = math.normalize(UnityEngine.Random.insideUnitSphere);
                float speed = UnityEngine.Random.Range(3f, 6f);

                ecb.SetComponent(particle, new FireworkParticle
                {
                    Position = origin,
                    Velocity = dir * speed,
                    Lifetime = 2f,
                    StartLifetime = 2f
                });

                ecb.SetComponent(particle, new ParticleSize
                {
                    StartSize = 0.1f
                });

                ecb.SetComponent(particle, new LocalTransform
                {
                    Position = origin,
                    Rotation = quaternion.identity,
                    Scale = 0.1f
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
