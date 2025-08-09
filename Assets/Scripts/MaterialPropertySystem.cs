using Unity.Entities;
using UnityEngine.Rendering;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
public partial struct MaterialPropertySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (color, materialProperty, entity) in
                 SystemAPI.Query<RefRO<ParticleColor>, RefRW<ParticleColor>>().WithEntityAccess())
        {
            //materialProperty.ValueRW.Value("_Color", new UnityEngine.Vector4(
            //    color.ValueRO.Value.x,
            //    color.ValueRO.Value.y,
            //    color.ValueRO.Value.z,
            //    color.ValueRO.Value.w));
        }
    }
}
