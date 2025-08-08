using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct FireworkSpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FireworkSpawnerComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (spawner, entity) in SystemAPI.Query<FireworkSpawnerComponent>().WithEntityAccess())
        {
            var newFirework = ecb.Instantiate(spawner.Prefab);

            ecb.SetComponent(newFirework, new LocalTransform
            {
                Position = new float3(0, 0, 0), // origin of explosion
                Rotation = quaternion.identity,
                Scale = 1
            });

            // You can add velocity and random direction components here
        }

        ecb.Playback(state.EntityManager);
    }
}
