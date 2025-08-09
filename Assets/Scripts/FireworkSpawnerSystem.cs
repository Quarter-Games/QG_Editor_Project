using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct FireworkSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var random = Unity.Mathematics.Random.CreateFromIndex(
            (uint)(SystemAPI.Time.ElapsedTime * 1000)
        );

        foreach (var (spawner, entity) in SystemAPI.Query<FireworkSpawnerComponent>().WithEntityAccess())
        {
            var prefabData = state.EntityManager.GetComponentData<FireworkParticleData>(spawner.Prefab);

            var newFirework = ecb.Instantiate(spawner.Prefab);

            ecb.SetComponent(newFirework, new LocalTransform
            {
                Position = new float3(0, 0, 0),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            var direction = math.normalize(random.NextFloat3Direction());
            ecb.AddComponent(newFirework, new Velocity
            {
                Value = direction * prefabData.MaxSpeed
            });

            ecb.AddComponent(newFirework, new Lifetime
            {
                Value = prefabData.LifeTime
            });

            ecb.AddComponent(newFirework, new Gravity
            {
                Value = prefabData.GravityStrength
            });

            ecb.AddComponent(newFirework, new ParticleColor
            {
                Name = Shader.PropertyToID("_Color"),
                Value = prefabData.Color
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
