using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct ColorFadeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ParticleColor>();
        state.RequireForUpdate<Lifetime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (color, life) in SystemAPI.Query<RefRW<ParticleColor>, RefRW<Lifetime>>())
        {
            // Fade alpha based on lifetime
            float lifeRatio = math.max(life.ValueRW.Value / 2f, 0f); // 2f is your default lifetime
            color.ValueRW.Value.w = lifeRatio; // update alpha
        }
    }
}
